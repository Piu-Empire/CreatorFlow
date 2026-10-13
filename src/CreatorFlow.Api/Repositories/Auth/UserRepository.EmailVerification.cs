using CreatorFlow.Api.Models.Auth;
using Npgsql;
using NpgsqlTypes;
using System.Security.Cryptography;

namespace CreatorFlow.Api.Repositories.Auth;

public sealed partial class UserRepository
{
    private const string VerificationSelect = """
        SELECT r.request_id, r.email_normalized, r.user_id, r.verifier_version, r.purpose, r.otp_verifier,
               r.created_at, r.expires_at, r.failed_attempts, r.delivered_at, r.consumed_at,
               r.invalidated_at, clock_timestamp() AS database_now, u.email AS user_email,
               u.password_hash, COALESCE(u.account_status = 'ACTIVE' AND LOWER(u.email) = r.email_normalized, FALSE) AS is_active_account
        FROM email_verification_requests r LEFT JOIN users u ON u.user_id = r.user_id
        """;

    public async Task<bool> IsEmailVerificationSchemaReadyAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT to_regclass('email_verification_requests') IS NOT NULL
                AND to_regclass('uq_email_verification_requests_pending_email') IS NOT NULL
                AND to_regclass('ix_email_verification_requests_email_created') IS NOT NULL;
            """;
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        if (!(bool)(await command.ExecuteScalarAsync(cancellationToken))!) return false;
        // Resolve the expected columns without reading account data or changing schema.
        await using var columns = new NpgsqlCommand(VerificationSelect + " WHERE FALSE;", connection);
        await using NpgsqlDataReader reader = await columns.ExecuteReaderAsync(cancellationToken);
        return true;
    }

    public async Task<string> NormalizeVerificationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT LOWER(@email);", connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<EmailVerificationRequest?> FindLatestEmailVerificationAsync(string emailNormalized,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(VerificationSelect +
            " WHERE r.email_normalized = @email ORDER BY r.created_at DESC, r.request_id DESC LIMIT 1;", connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, emailNormalized);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapVerification(reader) : null;
    }

    public async Task<EmailVerificationRequest?> FindEmailVerificationAsync(Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = VerificationCommand(VerificationSelect + " WHERE r.request_id = @request_id;", connection, null, requestId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapVerification(reader) : null;
    }

    public async Task<bool> TryCreateEmailVerificationAsync(Guid requestId, string emailNormalized, byte[] verifier,
        Guid? expectedPreviousId, long? expectedUserId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        await LockVerificationEmailAsync(connection, transaction, emailNormalized, cancellationToken);
        DateTimeOffset now;
        await using (var clock = new NpgsqlCommand("SELECT clock_timestamp();", connection, transaction))
            now = new DateTimeOffset((DateTime)(await clock.ExecuteScalarAsync(cancellationToken))!);
        Guid? previousId = null;
        DateTimeOffset? previousCreatedAt = null;
        await using (var latest = new NpgsqlCommand("""
            SELECT request_id, created_at FROM email_verification_requests
            WHERE email_normalized = @email ORDER BY created_at DESC, request_id DESC LIMIT 1;
            """, connection, transaction))
        {
            latest.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, emailNormalized);
            await using NpgsqlDataReader reader = await latest.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                previousId = reader.GetGuid(0);
                previousCreatedAt = new DateTimeOffset(reader.GetDateTime(1));
            }
        }
        if (previousId != expectedPreviousId || previousCreatedAt > now.AddSeconds(-60)) return false;
        await using (var quota = new NpgsqlCommand("""
            SELECT COUNT(*) FROM email_verification_requests
            WHERE email_normalized = @email AND created_at > @window_start;
            """, connection, transaction))
        {
            quota.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, emailNormalized);
            quota.Parameters.AddWithValue("window_start", NpgsqlDbType.TimestampTz, now.AddHours(-1));
            if ((long)(await quota.ExecuteScalarAsync(cancellationToken))! >= 5) return false;
        }
        const string sql = """
            UPDATE email_verification_requests SET invalidated_at = @now
            WHERE email_normalized = @email AND consumed_at IS NULL AND invalidated_at IS NULL;
            INSERT INTO email_verification_requests
                (request_id, email_normalized, user_id, otp_verifier, created_at, expires_at)
            VALUES (@request_id, @email,
                (SELECT user_id FROM users WHERE LOWER(email) = @email AND user_id = @expected_user AND account_status = 'ACTIVE' AND email_verified_at IS NULL),
                @verifier, @now, @expires_at);
            """;
        await using var insert = VerificationCommand(sql, connection, transaction, requestId);
        insert.Parameters.AddWithValue("expected_user", NpgsqlDbType.Bigint, (object?)expectedUserId ?? DBNull.Value);
        insert.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, emailNormalized);
        insert.Parameters.AddWithValue("verifier", NpgsqlDbType.Bytea, verifier);
        insert.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, now);
        insert.Parameters.AddWithValue("expires_at", NpgsqlDbType.TimestampTz, now.AddMinutes(10));
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<bool> MarkEmailVerificationDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        WithLockedVerificationAsync(requestId, async (connection, transaction, request) =>
        {
            if (!IsPendingVerification(request) || !request.IsActiveAccount || request.UserId is null) return VerificationMutation.Rollback;
            await using var command = VerificationCommand("""
                UPDATE email_verification_requests SET delivered_at = clock_timestamp()
                WHERE request_id = @request_id AND expires_at > clock_timestamp()
                    AND delivered_at IS NULL;
                """, connection, transaction, requestId);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1 ? VerificationMutation.Success : VerificationMutation.Rollback;
        }, cancellationToken);

    public async Task InvalidateEmailVerificationAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        _ = await WithLockedVerificationAsync(requestId, async (connection, transaction, request) =>
        {
            if (request.ConsumedAt is not null || request.InvalidatedAt is not null) return VerificationMutation.Rollback;
            await using var command = VerificationCommand("""
                UPDATE email_verification_requests SET invalidated_at = clock_timestamp()
                WHERE request_id = @request_id;
                """, connection, transaction, requestId);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1 ? VerificationMutation.Success : VerificationMutation.Rollback;
        }, cancellationToken);

    public async Task RecordEmailVerificationFailureAsync(Guid requestId, byte[] expectedVerifier,
        CancellationToken cancellationToken = default) =>
        _ = await WithLockedVerificationAsync(requestId, async (connection, transaction, request) =>
        {
            if (!IsUsableVerification(request) || !SameVerifier(request, expectedVerifier)) return VerificationMutation.Rollback;
            await using var command = VerificationCommand("""
                UPDATE email_verification_requests
                SET failed_attempts = failed_attempts + 1,
                    invalidated_at = CASE WHEN failed_attempts + 1 >= 5 THEN clock_timestamp() ELSE NULL END
                WHERE request_id = @request_id AND expires_at > clock_timestamp();
                """, connection, transaction, requestId);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1 ? VerificationMutation.Success : VerificationMutation.Rollback;
        }, cancellationToken);

    public Task<bool> CompleteEmailVerificationAsync(Guid requestId, byte[] expectedVerifier,
        CancellationToken cancellationToken = default) =>
        WithLockedVerificationAsync(requestId, async (connection, transaction, request) =>
        {
            if (!IsUsableVerification(request) || !SameVerifier(request, expectedVerifier)) return VerificationMutation.Rollback;
            await using var user = VerificationCommand("""
                UPDATE users SET email_verified_at = clock_timestamp()
                WHERE user_id = @user_id AND LOWER(email) = @email AND account_status = 'ACTIVE'
                    AND email_verified_at IS NULL;
                """, connection, transaction, requestId);
            user.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, request.UserId!.Value);
            user.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, request.EmailNormalized);
            if (await user.ExecuteNonQueryAsync(cancellationToken) != 1) return VerificationMutation.Rollback;
            await using var consume = VerificationCommand("""
                UPDATE email_verification_requests SET consumed_at = clock_timestamp()
                WHERE request_id = @request_id AND expires_at > clock_timestamp()
                    AND delivered_at IS NOT NULL AND invalidated_at IS NULL AND consumed_at IS NULL
                    AND failed_attempts < 5;
                """, connection, transaction, requestId);
            return await consume.ExecuteNonQueryAsync(cancellationToken) == 1 ? VerificationMutation.Success : VerificationMutation.Rollback;
        }, cancellationToken);

    private async Task<bool> WithLockedVerificationAsync(Guid requestId,
        Func<NpgsqlConnection, NpgsqlTransaction, EmailVerificationRequest, Task<VerificationMutation>> operation,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        string? email;
        await using (var find = VerificationCommand("SELECT email_normalized FROM email_verification_requests WHERE request_id = @request_id;",
            connection, transaction, requestId))
            email = (string?)await find.ExecuteScalarAsync(cancellationToken);
        if (email is null) return false;
        await LockVerificationEmailAsync(connection, transaction, email, cancellationToken);
        EmailVerificationRequest request;
        await using (var read = VerificationCommand(VerificationSelect + " WHERE r.request_id = @request_id FOR UPDATE OF r;",
            connection, transaction, requestId))
        {
            await using NpgsqlDataReader reader = await read.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return false;
            request = MapVerification(reader);
        }
        VerificationMutation result = await operation(connection, transaction, request);
        if (result == VerificationMutation.Rollback) await transaction.RollbackAsync(CancellationToken.None);
        else await transaction.CommitAsync(cancellationToken);
        return result == VerificationMutation.Success;
    }

    private enum VerificationMutation { Rollback, Success, InvalidateOnly }

    private static bool IsPendingVerification(EmailVerificationRequest request) => request.ConsumedAt is null &&
        request.InvalidatedAt is null && request.FailedAttempts < 5 && request.ExpiresAt > request.DatabaseNow;

    private static bool IsUsableVerification(EmailVerificationRequest request) => IsPendingVerification(request) &&
        request.VerifierVersion == 2 && request.Purpose == "EMAIL_VERIFICATION" &&
        request.DeliveredAt is not null && request.UserId is not null && request.IsActiveAccount;

    private static bool SameVerifier(EmailVerificationRequest request, byte[] expected) => expected.Length == 32 &&
        CryptographicOperations.FixedTimeEquals(request.OtpVerifier, expected);

    private static NpgsqlCommand VerificationCommand(string sql, NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid requestId)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("request_id", NpgsqlDbType.Uuid, requestId);
        return command;
    }

    private static async Task LockVerificationEmailAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string email, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('EMAIL_VERIFICATION:' || @email, 0));", connection, transaction);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Text, email);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static EmailVerificationRequest MapVerification(NpgsqlDataReader reader)
    {
        DateTimeOffset? Timestamp(string column)
        {
            int ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : new DateTimeOffset(reader.GetDateTime(ordinal));
        }
        string? OptionalText(string column)
        {
            int ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        int userOrdinal = reader.GetOrdinal("user_id");
        return new EmailVerificationRequest
        {
            VerifierVersion = reader.GetInt16(reader.GetOrdinal("verifier_version")),
            Purpose = reader.GetString(reader.GetOrdinal("purpose")),
            RequestId = reader.GetGuid(reader.GetOrdinal("request_id")),
            EmailNormalized = reader.GetString(reader.GetOrdinal("email_normalized")),
            UserId = reader.IsDBNull(userOrdinal) ? null : reader.GetInt64(userOrdinal),
            OtpVerifier = reader.GetFieldValue<byte[]>(reader.GetOrdinal("otp_verifier")),
            CreatedAt = Timestamp("created_at")!.Value, ExpiresAt = Timestamp("expires_at")!.Value,
            DatabaseNow = Timestamp("database_now")!.Value,
            FailedAttempts = reader.GetInt16(reader.GetOrdinal("failed_attempts")),
            DeliveredAt = Timestamp("delivered_at"), ConsumedAt = Timestamp("consumed_at"), InvalidatedAt = Timestamp("invalidated_at"),
            IsActiveAccount = reader.GetBoolean(reader.GetOrdinal("is_active_account")),
            Email = OptionalText("user_email"), PasswordHash = OptionalText("password_hash")
        };
    }
}
