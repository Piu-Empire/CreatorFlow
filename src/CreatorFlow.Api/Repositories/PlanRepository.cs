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
