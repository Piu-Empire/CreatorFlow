using CreatorFlow.Data;
using Npgsql;

namespace CreatorFlow.Repositories;

public sealed class DatabaseReadinessRepository(IDbConnectionFactory connectionFactory) : IDatabaseReadinessRepository
{
    public async Task ProbeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        // Resolve Auth/User tables and columns without reading any user or OTP values.
        const string sql = """
            SELECT 1;
            SELECT user_id, email, display_name, avatar_url, password_hash, account_status,
                   is_system_admin, email_verified_at, token_version FROM public.users LIMIT 0;
            SELECT request_id, email_normalized, user_id, purpose, verifier_version, otp_verifier,
                   created_at, expires_at, failed_attempts, delivered_at, consumed_at, invalidated_at
            FROM public.password_reset_requests LIMIT 0;
            SELECT request_id, email_normalized, user_id, purpose, verifier_version, otp_verifier,
                   created_at, expires_at, failed_attempts, delivered_at, consumed_at, invalidated_at
            FROM public.email_verification_requests LIMIT 0;
            SELECT user_id, image_data, content_type, width, height, updated_at FROM public.user_avatars LIMIT 0;
            """;
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 5 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
