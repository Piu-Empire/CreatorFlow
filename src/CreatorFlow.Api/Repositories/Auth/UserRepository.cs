using CreatorFlow.Data;
using CreatorFlow.Api.Models.Auth;

using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Auth;

public sealed partial class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory) => this.connectionFactory = connectionFactory;
    public async Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT user_id, email, display_name, avatar_url, password_hash,
                   account_status::text AS account_status, is_system_admin, email_verified_at, token_version
            FROM users
            WHERE LOWER(email) = LOWER(@email);
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapUser(reader);
    }

    public async Task<User?> RecordSuccessfulLoginAsync(
        long userId, string expectedPasswordHash, long expectedTokenVersion,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE users
            SET last_login_at = CURRENT_TIMESTAMP
            WHERE user_id = @user_id AND password_hash = @expected_hash AND token_version = @expected_version
                AND account_status = 'ACTIVE' AND email_verified_at IS NOT NULL
            RETURNING user_id, email, display_name, avatar_url, password_hash,
                account_status::text AS account_status, is_system_admin, email_verified_at, token_version;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
        command.Parameters.AddWithValue("expected_hash", NpgsqlDbType.Text, expectedPasswordHash);
        command.Parameters.AddWithValue("expected_version", NpgsqlDbType.Bigint, expectedTokenVersion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
    }

    public async Task<User?> FindByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT user_id, email, display_name, avatar_url, password_hash,
                   account_status::text AS account_status, is_system_admin, email_verified_at, token_version
            FROM users WHERE user_id = @user_id;
            """;
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
    }

    public async Task<long> CreateAsync(string displayName, string email, string passwordHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO users (display_name, email, password_hash, account_status, is_system_admin)
            VALUES (@display_name, @email, @password_hash, 'ACTIVE', FALSE)
            RETURNING user_id;
            """;
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("display_name", NpgsqlDbType.Varchar, displayName);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, email);
        command.Parameters.AddWithValue("password_hash", NpgsqlDbType.Text, passwordHash);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<bool> UpdatePasswordAsync(long userId, string currentPasswordHash, string newPasswordHash, long expectedTokenVersion,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE users SET password_hash = @new_hash
            WHERE user_id = @user_id AND password_hash = @current_hash AND token_version = @expected_version
                AND account_status = 'ACTIVE' AND email_verified_at IS NOT NULL;
            """;
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
        command.Parameters.AddWithValue("current_hash", NpgsqlDbType.Text, currentPasswordHash);
        command.Parameters.AddWithValue("new_hash", NpgsqlDbType.Text, newPasswordHash);
        command.Parameters.AddWithValue("expected_version", NpgsqlDbType.Bigint, expectedTokenVersion);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static User MapUser(NpgsqlDataReader reader) => new()
    {
        UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
        TokenVersion = reader.GetInt64(reader.GetOrdinal("token_version")),
        Email = reader.GetString(reader.GetOrdinal("email")),
        DisplayName = reader.GetString(reader.GetOrdinal("display_name")),
        AvatarUrl = ReadAvatarUrl(reader),
        EmailVerifiedAt = reader.IsDBNull(reader.GetOrdinal("email_verified_at")) ? null : new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("email_verified_at"))),
        PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
        AccountStatus = Enum.Parse<AccountStatus>(reader.GetString(reader.GetOrdinal("account_status")), ignoreCase: true),
        IsSystemAdmin = reader.GetBoolean(reader.GetOrdinal("is_system_admin"))
    };

    private static UserProfile MapProfile(NpgsqlDataReader reader) => new()
    {
        UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
        Email = reader.GetString(reader.GetOrdinal("email")),
        DisplayName = reader.GetString(reader.GetOrdinal("display_name")),
        AvatarUrl = ReadAvatarUrl(reader),
        AccountStatus = Enum.Parse<AccountStatus>(reader.GetString(reader.GetOrdinal("account_status")), ignoreCase: true),
        IsSystemAdmin = reader.GetBoolean(reader.GetOrdinal("is_system_admin"))
    };

    private static string? ReadAvatarUrl(NpgsqlDataReader reader)
    {
        int ordinal = reader.GetOrdinal("avatar_url");
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
