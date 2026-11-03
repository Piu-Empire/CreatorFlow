using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>Truy vấn SQL parameterized cho đăng ký gói dịch vụ (SCRUM-23).</summary>
public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private const string SelectColumns = """
        s.subscription_id, s.user_id, u.email AS user_email,
        s.plan_id, p.code AS plan_code, p.name AS plan_name,
        s.status::text AS status, s.started_at, s.expires_at
        """;

    private readonly IDbConnectionFactory connectionFactory;

    public SubscriptionRepository(IDbConnectionFactory connectionFactory) =>
        this.connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<AdminSubscriptionRecord>> SearchAsync(string? status, long? planId,
        string? search, int limit, int offset, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {SelectColumns}
            FROM subscriptions s
            JOIN users u ON u.user_id = s.user_id
            JOIN plans p ON p.plan_id = s.plan_id
            WHERE (@status IS NULL OR s.status = @status::subscription_status)
              AND (@plan_id IS NULL OR s.plan_id = @plan_id)
              AND (@like IS NULL OR u.email ILIKE @like ESCAPE '\')
            ORDER BY s.subscription_id DESC
            LIMIT @limit OFFSET @offset;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddFilterParameters(command, status, planId, search);
        command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Integer, offset);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<AdminSubscriptionRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapRecord(reader));
        }

        return items;
    }

    public async Task<int> CountAsync(string? status, long? planId, string? search,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM subscriptions s
            JOIN users u ON u.user_id = s.user_id
            WHERE (@status IS NULL OR s.status = @status::subscription_status)
              AND (@plan_id IS NULL OR s.plan_id = @plan_id)
              AND (@like IS NULL OR u.email ILIKE @like ESCAPE '\');
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddFilterParameters(command, status, planId, search);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is long count ? checked((int)count) : 0;
    }

    public async Task<AdminSubscriptionRecord?> GetByIdAsync(long subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByIdAsync(connection, subscriptionId, cancellationToken);
    }

    public async Task<AdminSubscriptionRecord?> GetActiveByUserAsync(long userId,
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {SelectColumns}
            FROM subscriptions s
            JOIN users u ON u.user_id = s.user_id
            JOIN plans p ON p.plan_id = s.plan_id
            WHERE s.user_id = @user_id AND s.status = 'ACTIVE'
            ORDER BY s.subscription_id DESC
            LIMIT 1;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? MapRecord(reader) : null;
    }

    public async Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT 1 FROM users WHERE user_id = @user_id;";
        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<AdminSubscriptionRecord?> CreateAsync(long userId, long planId, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO subscriptions (user_id, plan_id, status, expires_at)
            SELECT @user_id, @plan_id, 'ACTIVE', @expires_at
            WHERE EXISTS (SELECT 1 FROM users WHERE user_id = @user_id)
              AND EXISTS (SELECT 1 FROM plans WHERE plan_id = @plan_id)
            RETURNING subscription_id;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        long subscriptionId;
        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
            command.Parameters.AddWithValue("plan_id", NpgsqlDbType.Bigint, planId);
            command.Parameters.AddWithValue("expires_at", NpgsqlDbType.TimestampTz,
                (object?)expiresAt ?? DBNull.Value);
            object? created = await command.ExecuteScalarAsync(cancellationToken);
            if (created is not long id)
            {
                return null;
            }

            subscriptionId = id;
        }

        return await GetByIdAsync(connection, subscriptionId, cancellationToken);
    }

    public async Task<AdminSubscriptionRecord?> UpdateAsync(long subscriptionId, string status,
        DateTimeOffset? expiresAt, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE subscriptions
            SET status = @status::subscription_status,
                expires_at = COALESCE(@expires_at, expires_at)
            WHERE subscription_id = @subscription_id
            RETURNING subscription_id;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("subscription_id", NpgsqlDbType.Bigint, subscriptionId);
            command.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, status);
            command.Parameters.AddWithValue("expires_at", NpgsqlDbType.TimestampTz,
                (object?)expiresAt ?? DBNull.Value);
            if (await command.ExecuteScalarAsync(cancellationToken) is null)
            {
                return null;
            }
        }

        return await GetByIdAsync(connection, subscriptionId, cancellationToken);
    }

    private static async Task<AdminSubscriptionRecord?> GetByIdAsync(NpgsqlConnection connection,
        long subscriptionId, CancellationToken cancellationToken)
    {
        string sql = $"""
            SELECT {SelectColumns}
            FROM subscriptions s
            JOIN users u ON u.user_id = s.user_id
            JOIN plans p ON p.plan_id = s.plan_id
            WHERE s.subscription_id = @subscription_id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("subscription_id", NpgsqlDbType.Bigint, subscriptionId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRecord(reader) : null;
    }

    private static void AddFilterParameters(NpgsqlCommand command, string? status, long? planId, string? search)
    {
        string? pattern = string.IsNullOrWhiteSpace(search) ? null : $"%{EscapeLike(search.Trim())}%";
        command.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("plan_id", NpgsqlDbType.Bigint, (object?)planId ?? DBNull.Value);
        command.Parameters.AddWithValue("like", NpgsqlDbType.Varchar, (object?)pattern ?? DBNull.Value);
    }

    private static string EscapeLike(string value) => value
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal);

    private static AdminSubscriptionRecord MapRecord(NpgsqlDataReader reader)
    {
        int expiresOrdinal = reader.GetOrdinal("expires_at");
        return new AdminSubscriptionRecord
        {
            SubscriptionId = reader.GetInt64(reader.GetOrdinal("subscription_id")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            UserEmail = reader.GetString(reader.GetOrdinal("user_email")),
            PlanId = reader.GetInt64(reader.GetOrdinal("plan_id")),
            PlanCode = reader.GetString(reader.GetOrdinal("plan_code")),
            PlanName = reader.GetString(reader.GetOrdinal("plan_name")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
            ExpiresAt = reader.IsDBNull(expiresOrdinal)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(expiresOrdinal),
        };
    }
}
