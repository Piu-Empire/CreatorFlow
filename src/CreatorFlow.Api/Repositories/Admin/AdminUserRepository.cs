using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Data;

using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>Truy vấn SQL parameterized cho màn hình quản trị người dùng.</summary>
public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public AdminUserRepository(IDbConnectionFactory connectionFactory) => this.connectionFactory = connectionFactory;

    /// <summary>Tìm người dùng theo từ khóa (đã escape LIKE) và trạng thái.</summary>
    public async Task<IReadOnlyList<AdminUserRecord>> SearchAsync(string? search, string? status, int limit, int offset,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT user_id, email, display_name, account_status::text AS account_status,
                   is_system_admin, created_at, last_login_at
            FROM users
            WHERE (@status IS NULL OR account_status = @status::account_status)
              AND (@like IS NULL OR email ILIKE @like ESCAPE '\' OR display_name ILIKE @like ESCAPE '\')
            ORDER BY user_id
            LIMIT @limit OFFSET @offset;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddFilterParameters(command, search, status);
        command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, offset);
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<AdminUserRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapRecord(reader));
        }

        return items;
    }

    /// <summary>Đếm số người dùng khớp cùng điều kiện lọc với SearchAsync.</summary>
    public async Task<int> CountAsync(string? search, string? status,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM users
            WHERE (@status IS NULL OR account_status = @status::account_status)
              AND (@like IS NULL OR email ILIKE @like ESCAPE '\' OR display_name ILIKE @like ESCAPE '\');
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddFilterParameters(command, search, status);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is long count ? checked((int)count) : 0;
    }

    /// <summary>Đổi trạng thái kèm audit trong một transaction, chống khóa admin cuối.</summary>
    public async Task<AdminStatusChange?> UpdateStatusAsync(long targetUserId, AccountStatus status, long actorUserId,
        string traceId, CancellationToken cancellationToken = default)
    {
        string statusText = status == AccountStatus.Locked ? "LOCKED" : "ACTIVE";
        string action = status == AccountStatus.Locked ? "USER_LOCK" : "USER_UNLOCK";

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        // Khóa dòng target trước rồi mới đọc: mọi request cùng user xếp hàng tại đây.
        await using var lockTarget = new NpgsqlCommand(
            "SELECT email, account_status::text AS account_status, is_system_admin FROM users WHERE user_id = @id FOR UPDATE;",
            connection, transaction);
        lockTarget.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, targetUserId);
        AccountStatus previous;
        bool targetIsAdmin;
        string email;
        await using (var reader = await lockTarget.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            email = reader.GetString(reader.GetOrdinal("email"));
            previous = Enum.Parse<AccountStatus>(reader.GetString(reader.GetOrdinal("account_status")), ignoreCase: true);
            targetIsAdmin = reader.GetBoolean(reader.GetOrdinal("is_system_admin"));
        }

        // Idempotent: đã đúng trạng thái thì coi như thành công, không UPDATE và không audit.
        if (previous == status)
        {
            await transaction.CommitAsync(cancellationToken);
            return new AdminStatusChange(Found: true, Changed: false, BlockedLastAdmin: false,
                PreviousStatus: previous, TargetIsSystemAdmin: targetIsAdmin, Email: email);
        }

        // Khóa các dòng admin theo thứ tự cố định để hai transaction khóa chéo không deadlock.
        await using (var lockAdmins = new NpgsqlCommand(
            "SELECT user_id FROM users WHERE is_system_admin = TRUE ORDER BY user_id FOR UPDATE;",
            connection, transaction))
        {
            await lockAdmins.ExecuteNonQueryAsync(cancellationToken);
        }

        if (targetIsAdmin)
        {
            const string remainingSql = """
                SELECT COUNT(*) FROM users
                WHERE is_system_admin AND account_status = 'ACTIVE' AND user_id <> @id;
                """;
            await using var remaining = new NpgsqlCommand(remainingSql, connection, transaction);
            remaining.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, targetUserId);
            object? value = await remaining.ExecuteScalarAsync(cancellationToken);
            if (value is not long remainingCount || remainingCount == 0)
            {
                return new AdminStatusChange(Found: true, Changed: false, BlockedLastAdmin: true,
                    PreviousStatus: previous, TargetIsSystemAdmin: true, Email: email);
            }
        }

        await using (var update = new NpgsqlCommand(
            "UPDATE users SET account_status = @status::account_status, updated_at = CURRENT_TIMESTAMP WHERE user_id = @id;",
            connection, transaction))
        {
            update.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, targetUserId);
            update.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, statusText);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                return null;
            }
        }

        await using (var audit = new NpgsqlCommand(
            "INSERT INTO audit_logs (actor_user_id, action, entity_type, entity_id, description) VALUES (@actor, @action, 'users', @id, @detail);",
            connection, transaction))
        {
            string verb = status == AccountStatus.Locked ? "LOCK" : "UNLOCK";
            string oldText = previous.ToString().ToUpperInvariant();
            audit.Parameters.AddWithValue("actor", NpgsqlDbType.Bigint, actorUserId);
            audit.Parameters.AddWithValue("action", NpgsqlDbType.Varchar, action);
            audit.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, targetUserId);
            audit.Parameters.AddWithValue("detail", NpgsqlDbType.Text,
                $"{verb} {email} (id={targetUserId}) {oldText}->{statusText} trace={traceId}");
            await audit.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdminStatusChange(Found: true, Changed: true, BlockedLastAdmin: false,
            PreviousStatus: previous, TargetIsSystemAdmin: targetIsAdmin, Email: email);
    }

    private static void AddFilterParameters(NpgsqlCommand command, string? search, string? status)
    {
        string? pattern = string.IsNullOrWhiteSpace(search) ? null : $"%{EscapeLike(search.Trim())}%";
        command.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("like", NpgsqlDbType.Varchar, (object?)pattern ?? DBNull.Value);
    }

    /// <summary>Escape các ký tự đại diện của LIKE để từ khóa chỉ match nghĩa đen.</summary>
    private static string EscapeLike(string value) => value
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal);

    private static AdminUserRecord MapRecord(NpgsqlDataReader reader)
    {
        int lastLogin = reader.GetOrdinal("last_login_at");
        int createdAt = reader.GetOrdinal("created_at");
        return new AdminUserRecord
        {
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            DisplayName = reader.GetString(reader.GetOrdinal("display_name")),
            AccountStatus = Enum.Parse<AccountStatus>(reader.GetString(reader.GetOrdinal("account_status")), ignoreCase: true),
            IsSystemAdmin = reader.GetBoolean(reader.GetOrdinal("is_system_admin")),
            CreatedAt = new DateTimeOffset(reader.GetDateTime(createdAt)),
            LastLoginAt = reader.IsDBNull(lastLogin) ? null : new DateTimeOffset(reader.GetDateTime(lastLogin)),
        };
    }
}
