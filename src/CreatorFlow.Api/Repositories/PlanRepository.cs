using CreatorFlow.Data;
using CreatorFlow.Models;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Repositories;

public sealed class PlanRepository(IDbConnectionFactory connectionFactory) : IPlanRepository
{
    private const string SelectColumns = """
        plan_id,
        code,
        name,
        description,
        price_monthly,
        max_projects,
        max_members,
        ai_request_limit,
        is_active,
        created_at
        """;

    public async Task<Plan?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {SelectColumns}
            FROM plans
            WHERE plan_id = @plan_id;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("plan_id", NpgsqlDbType.Bigint, id);

        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? MapPlan(reader)
            : null;
    }

    public async Task<Plan> AddAsync(
        Plan entity,
        CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            INSERT INTO plans (
                code,
                name,
                description,
                price_monthly,
                max_projects,
                max_members,
                ai_request_limit,
                is_active
            )
            VALUES (
                @code,
                @name,
                @description,
                @price_monthly,
                @max_projects,
                @max_members,
                @ai_request_limit,
                @is_active
            )
            RETURNING {SelectColumns};
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("code", NpgsqlDbType.Varchar, entity.Code);
        command.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, entity.Name);
        command.Parameters.AddWithValue(
            "description",
            NpgsqlDbType.Text,
            (object?)entity.Description ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "price_monthly",
            NpgsqlDbType.Numeric,
            entity.PriceMonthly);
        command.Parameters.AddWithValue(
            "max_projects",
            NpgsqlDbType.Integer,
            (object?)entity.MaxProjects ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "max_members",
            NpgsqlDbType.Integer,
            (object?)entity.MaxMembers ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "ai_request_limit",
            NpgsqlDbType.Integer,
            (object?)entity.AiRequestLimit ?? DBNull.Value);
        command.Parameters.AddWithValue("is_active", NpgsqlDbType.Boolean, entity.IsActive);

        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The inserted plan was not returned.");
        }

        return MapPlan(reader);
    }

    /// <summary>SCRUM-23: toàn bộ gói cho màn quản trị, gói mới nhất lên trước.</summary>
    public async Task<IReadOnlyList<Plan>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {SelectColumns}
            FROM plans
            ORDER BY plan_id;
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var plans = new List<Plan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            plans.Add(MapPlan(reader));
        }

        return plans;
    }

    /// <summary>SCRUM-23: dùng để chặn trùng mã gói (không phân biệt hoa/thường).</summary>
    public async Task<Plan?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {SelectColumns}
            FROM plans
            WHERE lower(code) = lower(@code);
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("code", NpgsqlDbType.Varchar, code);

        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? MapPlan(reader)
            : null;
    }

    /// <summary>SCRUM-23: cập nhật gói hiện có; null khi gói không tồn tại.</summary>
    public async Task<Plan?> UpdateAsync(
        Plan entity,
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            UPDATE plans
            SET name = @name,
                description = @description,
                price_monthly = @price_monthly,
                max_projects = @max_projects,
                max_members = @max_members,
                ai_request_limit = @ai_request_limit,
                is_active = @is_active
            WHERE plan_id = @plan_id
            RETURNING {SelectColumns};
            """;

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("plan_id", NpgsqlDbType.Bigint, entity.PlanId);
        command.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, entity.Name);
        command.Parameters.AddWithValue(
            "description",
            NpgsqlDbType.Text,
            (object?)entity.Description ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "price_monthly",
            NpgsqlDbType.Numeric,
            entity.PriceMonthly);
        command.Parameters.AddWithValue(
            "max_projects",
            NpgsqlDbType.Integer,
            (object?)entity.MaxProjects ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "max_members",
            NpgsqlDbType.Integer,
            (object?)entity.MaxMembers ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "ai_request_limit",
            NpgsqlDbType.Integer,
            (object?)entity.AiRequestLimit ?? DBNull.Value);
        command.Parameters.AddWithValue("is_active", NpgsqlDbType.Boolean, entity.IsActive);

        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? MapPlan(reader)
            : null;
    }

    private static Plan MapPlan(NpgsqlDataReader reader)
    {
        int descriptionOrdinal = reader.GetOrdinal("description");
        int maxProjectsOrdinal = reader.GetOrdinal("max_projects");
        int maxMembersOrdinal = reader.GetOrdinal("max_members");
        int aiRequestLimitOrdinal = reader.GetOrdinal("ai_request_limit");

        return new Plan
        {
            PlanId = reader.GetInt64(reader.GetOrdinal("plan_id")),
            Code = reader.GetString(reader.GetOrdinal("code")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(descriptionOrdinal)
                ? null
                : reader.GetString(descriptionOrdinal),
            PriceMonthly = reader.GetDecimal(reader.GetOrdinal("price_monthly")),
            MaxProjects = reader.IsDBNull(maxProjectsOrdinal)
                ? null
                : reader.GetInt32(maxProjectsOrdinal),
            MaxMembers = reader.IsDBNull(maxMembersOrdinal)
                ? null
                : reader.GetInt32(maxMembersOrdinal),
            AiRequestLimit = reader.IsDBNull(aiRequestLimitOrdinal)
                ? null
                : reader.GetInt32(aiRequestLimitOrdinal),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}
