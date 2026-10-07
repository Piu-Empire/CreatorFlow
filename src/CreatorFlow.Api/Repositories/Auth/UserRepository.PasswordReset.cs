using CreatorFlow.Api.Models.Auth;
using Npgsql;
using NpgsqlTypes;
using System.Security.Cryptography;

namespace CreatorFlow.Api.Repositories.Auth;

public sealed partial class UserRepository
{
    private const string ResetSelect = """
        SELECT r.request_id, r.email_normalized, r.user_id, r.verifier_version, r.purpose, r.otp_verifier,
               r.created_at, r.expires_at, r.failed_attempts, r.delivered_at, r.consumed_at,
               r.invalidated_at, clock_timestamp() AS database_now, u.email AS user_email,
               u.password_hash, COALESCE(u.account_status = 'ACTIVE' AND LOWER(u.email) = r.email_normalized, FALSE) AS is_active_account
        FROM password_reset_requests r LEFT JOIN users u ON u.user_id = r.user_id
        """;

    public async Task<bool> IsPasswordResetSchemaReadyAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT to_regclass('password_reset_requests') IS NOT NULL
                AND to_regclass('uq_password_reset_requests_pending_email') IS NOT NULL
                AND to_regclass('ix_password_reset_requests_email_created') IS NOT NULL;
            """;
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        if (!(bool)(await command.ExecuteScalarAsync(cancellationToken))!) return false;
        // Resolve the expected columns without reading account data or changing schema.
        await using var columns = new NpgsqlCommand(ResetSelect + " WHERE FALSE;", connection);
        await using NpgsqlDataReader reader = await columns.ExecuteReaderAsync(cancellationToken);
        return true;
    }

    public async Task<string> NormalizeResetEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT LOWER(@email);", connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<PasswordResetRequest?> FindLatestPasswordResetAsync(string emailNormalized,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ResetSelect +
            " WHERE r.email_normalized = @email ORDER BY r.created_at DESC, r.request_id DESC LIMIT 1;", connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, emailNormalized);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReset(reader) : null;
    }

    public async Task<PasswordResetRequest?> FindPasswordResetAsync(Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = ResetCommand(ResetSelect + " WHERE r.request_id = @request_id;", connection, null, requestId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReset(reader) : null;
    }

    public async Task<bool> TryCreatePasswordResetAsync(Guid requestId, string emailNormalized, byte[] verifier,
        Guid? expectedPreviousId, long? expectedUserId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        await LockResetEmailAsync(connection, transaction, emailNormalized, cancellationToken);
        DateTimeOffset now;
        await using (var clock = new NpgsqlCommand("SELECT clock_timestamp();", connection, transaction))
            now = new DateTimeOffset((DateTime)(await clock.ExecuteScalarAsync(cancellationToken))!);
        Guid? previousId = null;
        DateTimeOffset? previousCreatedAt = null;
        await using (var latest = new NpgsqlCommand("""
            SELECT request_id, created_at FROM password_reset_requests
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
            SELECT COUNT(*) FROM password_reset_requests
            WHERE email_normalized = @email AND created_at > @window_start;
            """, connection, transaction))
        {
            quota.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, emailNormalized);
            quota.Parameters.AddWithValue("window_start", NpgsqlDbType.TimestampTz, now.AddHours(-1));
            if ((long)(await quota.ExecuteScalarAsync(cancellationToken))! >= 5) return false;
        }
        const string sql = """
            UPDATE password_reset_requests SET invalidated_at = @now
            WHERE email_normalized = @email AND consumed_at IS NULL AND invalidated_at IS NULL;
            INSERT INTO password_reset_requests
                (request_id, email_normalized, user_id, otp_verifier, created_at, expires_at)
            VALUES (@request_id, @email,
                (SELECT user_id FROM users WHERE LOWER(email) = @email AND user_id = @expected_user AND account_status = 'ACTIVE'),
                @verifier, @now, @expires_at);
            """;
        await using var insert = ResetCommand(sql, connection, transaction, requestId);
        insert.Parameters.AddWithValue("expected_user", NpgsqlDbType.Bigint, (object?)expectedUserId ?? DBNull.Value);
        insert.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, emailNormalized);
        insert.Parameters.AddWithValue("verifier", NpgsqlDbType.Bytea, verifier);
        insert.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, now);
        insert.Parameters.AddWithValue("expires_at", NpgsqlDbType.TimestampTz, now.AddMinutes(10));
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<bool> MarkPasswordResetDeliveredAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        WithLockedResetAsync(requestId, async (connection, transaction, request) =>
        {
            if (!IsPendingReset(request) || !request.IsActiveAccount || request.UserId is null) return ResetMutation.Rollback;
            await using var command = ResetCommand("""
                UPDATE password_reset_requests SET delivered_at = clock_timestamp()
                WHERE request_id = @request_id AND expires_at > clock_timestamp()
                    AND delivered_at IS NULL;
                """, connection, transaction, requestId);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1 ? ResetMutation.Success : ResetMutation.Rollback;
        }, cancellationToken);

    public async Task InvalidatePasswordResetAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        _ = await WithLockedResetAsync(requestId, async (connection, transaction, request) =>
        {
            if (request.ConsumedAt is not null || request.InvalidatedAt is not null) return ResetMutation.Rollback;
            await using var command = ResetCommand("""
                UPDATE password_reset_requests SET invalidated_at = clock_timestamp()
                WHERE request_id = @request_id;
                """, connection, transaction, requestId);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1 ? ResetMutation.Success : ResetMutation.Rollback;
        }, cancellationToken);

    public async Task RecordPasswordResetFailureAsync(Guid requestId, byte[] expectedVerifier,
        CancellationToken cancellationToken = default) =>
        _ = await WithLockedResetAsync(requestId, async (connection, transaction, request) =>
        {
            if (!IsUsableReset(request) || !SameVerifier(request, expectedVerifier)) return ResetMutation.Rollback;
            await using var command = ResetCommand("""
                UPDATE password_reset_requests
                SET failed_attempts = failed_attempts + 1,
                    invalidated_at = CASE WHEN failed_attempts + 1 >= 5 THEN clock_timestamp() ELSE NULL END
                WHERE request_id = @request_id AND expires_at > clock_timestamp();
                """, connection, transaction, requestId);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1 ? ResetMutation.Success : ResetMutation.Rollback;
        }, cancellationToken);

    public Task<bool> CompletePasswordResetAsync(Guid requestId, byte[] expectedVerifier,
        string expectedPasswordHash, string newPasswordHash, CancellationToken cancellationToken = default) =>
        WithLockedResetAsync(requestId, async (connection, transaction, request) =>
        {
            if (!IsUsableReset(request) || !SameVerifier(request, expectedVerifier)) return ResetMutation.Rollback;
            await using var password = ResetCommand("""
                UPDATE users SET password_hash = @new_hash
                WHERE user_id = @user_id AND password_hash = @old_hash AND account_status = 'ACTIVE' AND LOWER(email)=@email;
                """, connection, transaction, requestId);
            password.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, request.EmailNormalized);
            password.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, request.UserId!.Value);
            password.Parameters.AddWithValue("old_hash", NpgsqlDbType.Text, expectedPasswordHash);
            password.Parameters.AddWithValue("new_hash", NpgsqlDbType.Text, newPasswordHash);
            if (await password.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                // Commit only invalidation, never a password change, on a stale user/hash.
                await using var invalidate = ResetCommand("""
                    UPDATE password_reset_requests SET invalidated_at = clock_timestamp()
                    WHERE request_id = @request_id;
                    """, connection, transaction, requestId);
                await invalidate.ExecuteNonQueryAsync(cancellationToken);
                return ResetMutation.InvalidateOnly;
            }
            await using var consume = ResetCommand("""
                UPDATE password_reset_requests SET consumed_at = clock_timestamp()
                WHERE request_id = @request_id AND expires_at > clock_timestamp()
                    AND delivered_at IS NOT NULL AND invalidated_at IS NULL AND consumed_at IS NULL
                    AND failed_attempts < 5;
                """, connection, transaction, requestId);
            return await consume.ExecuteNonQueryAsync(cancellationToken) == 1 ? ResetMutation.Success : ResetMutation.Rollback;
        }, cancellationToken);

    private async Task<bool> WithLockedResetAsync(Guid requestId,
        Func<NpgsqlConnection, NpgsqlTransaction, PasswordResetRequest, Task<ResetMutation>> operation,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        string? email;
        await using (var find = ResetCommand("SELECT email_normalized FROM password_reset_requests WHERE request_id = @request_id;",
            connection, transaction, requestId))
            email = (string?)await find.ExecuteScalarAsync(cancellationToken);
        if (email is null) return false;
        await LockResetEmailAsync(connection, transaction, email, cancellationToken);
        PasswordResetRequest request;
        await using (var read = ResetCommand(ResetSelect + " WHERE r.request_id = @request_id FOR UPDATE OF r;",
            connection, transaction, requestId))
        {
            await using NpgsqlDataReader reader = await read.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return false;
            request = MapReset(reader);
        }
        ResetMutation result = await operation(connection, transaction, request);
        if (result == ResetMutation.Rollback) await transaction.RollbackAsync(CancellationToken.None);
        else await transaction.CommitAsync(cancellationToken);
        return result == ResetMutation.Success;
    }

    private enum ResetMutation { Rollback, Success, InvalidateOnly }

    private static bool IsPendingReset(PasswordResetRequest request) => request.ConsumedAt is null &&
        request.InvalidatedAt is null && request.FailedAttempts < 5 && request.ExpiresAt > request.DatabaseNow;

    private static bool IsUsableReset(PasswordResetRequest request) => IsPendingReset(request) &&
        request.VerifierVersion == 2 && request.Purpose == "PASSWORD_RESET" &&
        request.DeliveredAt is not null && request.UserId is not null && request.IsActiveAccount;

    private static bool SameVerifier(PasswordResetRequest request, byte[] expected) => expected.Length == 32 &&
        CryptographicOperations.FixedTimeEquals(request.OtpVerifier, expected);

    private static NpgsqlCommand ResetCommand(string sql, NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid requestId)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("request_id", NpgsqlDbType.Uuid, requestId);
        return command;
    }

    private static async Task LockResetEmailAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string email, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('PASSWORD_RESET:' || @email, 0));", connection, transaction);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Text, email);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static PasswordResetRequest MapReset(NpgsqlDataReader reader)
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
        return new PasswordResetRequest
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
