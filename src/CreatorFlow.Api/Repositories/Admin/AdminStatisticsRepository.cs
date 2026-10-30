using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Data;
using Npgsql;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>
/// Tổng hợp số liệu toàn hệ thống bằng SQL aggregate trên một connection.
/// Không cache để con số luôn khớp dữ liệu thật.
/// </summary>
public sealed class AdminStatisticsRepository : IAdminStatisticsRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public AdminStatisticsRepository(IDbConnectionFactory connectionFactory) =>
        this.connectionFactory = connectionFactory;

    public async Task<AdminStatisticsRecord> GetAsync(CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        (int total, int active, int locked, int disabled, int admins) = await GetUsersAsync(connection, cancellationToken);
        (int projects, int activeProjects) = await GetProjectsAsync(connection, cancellationToken);
        IReadOnlyList<(string, int)> contentsByStatus = await GetContentsByStatusAsync(connection, cancellationToken);
        IReadOnlyList<AdminPlanUsageRecord> usage = await GetSubscriptionsByPlanAsync(connection, cancellationToken);
        IReadOnlyList<(string, int)> reportsByStatus = await GetReportsByStatusAsync(connection, cancellationToken);

        return new AdminStatisticsRecord
        {
            UsersTotal = total,
            UsersActive = active,
            UsersLocked = locked,
            UsersDisabled = disabled,
            SystemAdmins = admins,
            ProjectsTotal = projects,
            ProjectsActiveLast30Days = activeProjects,
            ContentsByStatus = contentsByStatus,
            SubscriptionsByPlan = usage,
            ReportsByStatus = reportsByStatus,
        };
    }

    private static async Task<(int, int, int, int, int)> GetUsersAsync(NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)::int,
                   COUNT(*) FILTER (WHERE account_status = 'ACTIVE')::int,
                   COUNT(*) FILTER (WHERE account_status = 'LOCKED')::int,
                   COUNT(*) FILTER (WHERE account_status = 'DISABLED')::int,
                   COUNT(*) FILTER (WHERE is_system_admin)::int
            FROM users;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (0, 0, 0, 0, 0);
        }

        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4));
    }

    private static async Task<(int, int)> GetProjectsAsync(NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT (SELECT COUNT(*) FROM projects)::int,
                   (SELECT COUNT(DISTINCT project_id) FROM contents
                    WHERE status = 'PUBLISHED' AND updated_at >= now() - interval '30 days')::int;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? (reader.GetInt32(0), reader.GetInt32(1)) : (0, 0);
    }

    private static async Task<IReadOnlyList<(string, int)>> GetContentsByStatusAsync(NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT status::text, COUNT(*)::int
            FROM contents
            GROUP BY status
            ORDER BY status;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(string, int)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<AdminPlanUsageRecord>> GetSubscriptionsByPlanAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.plan_id, p.code, p.name,
                   COUNT(s.subscription_id)::int AS total,
                   COUNT(s.subscription_id) FILTER (WHERE s.status = 'ACTIVE')::int AS active
            FROM plans p
            LEFT JOIN subscriptions s ON s.plan_id = p.plan_id
            GROUP BY p.plan_id, p.code, p.name
            ORDER BY p.plan_id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<AdminPlanUsageRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminPlanUsageRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt32(3), reader.GetInt32(4)));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<(string, int)>> GetReportsByStatusAsync(NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT status::text, COUNT(*)::int
            FROM reports
            GROUP BY status
            ORDER BY status;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(string, int)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        return rows;
    }
}
