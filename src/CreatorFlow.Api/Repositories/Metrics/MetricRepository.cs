using CreatorFlow.Api.Models.Metrics;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Metrics;

public interface IMetricRepository
{
    Task<IReadOnlyList<PlatformTarget>> GetPlatformsAsync(long contentId, CancellationToken cancellationToken = default);
    /// <summary>Snapshot mới nhất của mỗi platform trong Content (platform chưa có số liệu thì không có dòng).</summary>
    Task<IReadOnlyList<MetricSnapshot>> GetLatestAsync(long contentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetricSnapshot>> GetHistoryAsync(long contentPlatformId, CancellationToken cancellationToken = default);
    Task<MetricSnapshot> AddSnapshotAsync(NewMetric metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tổng hợp Views/Likes/Comments/Shares của một Project theo chiều (Content/Creator/Platform/Project) — SCRUM-52.
    /// Chỉ cộng trên snapshot MỚI NHẤT của mỗi ContentPlatform nên nhiều lần nhập không bị đếm trùng.
    /// </summary>
    Task<IReadOnlyList<MetricSummaryRecord>> GetProjectSummaryAsync(long projectId, MetricGroupBy groupBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// APPEND-SNAPSHOT: mỗi lần nhập = 1 INSERT mới (giữ lịch sử để vẽ chart); "giá trị hiện tại" = snapshot mới nhất
/// theo recorded_at DESC, metric_id DESC. Field null được gộp từ snapshot trước NGAY TRONG câu INSERT (không race).
/// </summary>
public sealed class MetricRepository(IDbConnectionFactory connectionFactory) : IMetricRepository
{
    private const string Columns =
        "m.metric_id, m.content_platform_id, m.views, m.likes, m.comments, m.shares, m.recorded_by, u.display_name, m.recorded_at";

    public async Task<IReadOnlyList<PlatformTarget>> GetPlatformsAsync(long contentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT cp.content_platform_id, cp.content_id, c.project_id, cp.platform_id, p.name,
                   cp.publication_status::text, c.status::text
            FROM content_platforms cp
            JOIN contents c ON c.content_id = cp.content_id
            JOIN platforms p ON p.platform_id = cp.platform_id
            WHERE cp.content_id = @id
            ORDER BY p.name;
            """;
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, contentId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<PlatformTarget>();
        while (await reader.ReadAsync(cancellationToken))
            list.Add(new PlatformTarget(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6)));
        return list;
    }

    public Task<IReadOnlyList<MetricSnapshot>> GetLatestAsync(long contentId, CancellationToken cancellationToken = default) =>
        QueryAsync($"""
            SELECT DISTINCT ON (m.content_platform_id) {Columns}
            FROM content_metrics m
            JOIN content_platforms cp ON cp.content_platform_id = m.content_platform_id
            LEFT JOIN users u ON u.user_id = m.recorded_by
            WHERE cp.content_id = @id
            ORDER BY m.content_platform_id, m.recorded_at DESC, m.metric_id DESC;
            """, contentId, cancellationToken);

    public Task<IReadOnlyList<MetricSnapshot>> GetHistoryAsync(long contentPlatformId, CancellationToken cancellationToken = default) =>
        QueryAsync($"""
            SELECT {Columns}
            FROM content_metrics m
            LEFT JOIN users u ON u.user_id = m.recorded_by
            WHERE m.content_platform_id = @id
            ORDER BY m.recorded_at DESC, m.metric_id DESC
            LIMIT 200;
            """, contentPlatformId, cancellationToken);

    /// <summary>
    /// CTE "giá trị hiện tại": snapshot mới nhất của mỗi ContentPlatform trong Project. Dùng chung cho câu tổng hợp.
    /// </summary>
    private const string LatestCte = """
        WITH latest AS (
            SELECT DISTINCT ON (m.content_platform_id)
                cp.content_id, cp.platform_id, c.project_id, c.created_by,
                m.views, m.likes, m.comments, m.shares
            FROM content_metrics m
            JOIN content_platforms cp ON cp.content_platform_id = m.content_platform_id
            JOIN contents c ON c.content_id = cp.content_id
            WHERE c.project_id = @pid
            ORDER BY m.content_platform_id, m.recorded_at DESC, m.metric_id DESC
        )
        """;

    public async Task<IReadOnlyList<MetricSummaryRecord>> GetProjectSummaryAsync(
        long projectId, MetricGroupBy groupBy, CancellationToken cancellationToken = default)
    {
        string body = groupBy switch
        {
            MetricGroupBy.Project => """
                SELECT p.project_id, p.name,
                       COALESCE(SUM(l.views), 0)::bigint, COALESCE(SUM(l.likes), 0)::bigint,
                       COALESCE(SUM(l.comments), 0)::bigint, COALESCE(SUM(l.shares), 0)::bigint
                FROM projects p
                LEFT JOIN latest l ON l.project_id = p.project_id
                WHERE p.project_id = @pid
                GROUP BY p.project_id, p.name;
                """,
            MetricGroupBy.Creator => """
                SELECT u.user_id, COALESCE(NULLIF(u.display_name, ''), u.email),
                       SUM(l.views)::bigint, SUM(l.likes)::bigint, SUM(l.comments)::bigint, SUM(l.shares)::bigint
                FROM latest l
                JOIN users u ON u.user_id = l.created_by
                GROUP BY u.user_id, u.display_name, u.email
                ORDER BY SUM(l.views) DESC, u.user_id;
                """,
            MetricGroupBy.Platform => """
                SELECT p.platform_id, p.name,
                       SUM(l.views)::bigint, SUM(l.likes)::bigint, SUM(l.comments)::bigint, SUM(l.shares)::bigint
                FROM latest l
                JOIN platforms p ON p.platform_id = l.platform_id
                GROUP BY p.platform_id, p.name
                ORDER BY SUM(l.views) DESC, p.name;
                """,
            _ => """
                SELECT c.content_id, c.title,
                       SUM(l.views)::bigint, SUM(l.likes)::bigint, SUM(l.comments)::bigint, SUM(l.shares)::bigint
                FROM latest l
                JOIN contents c ON c.content_id = l.content_id
                GROUP BY c.content_id, c.title
                ORDER BY SUM(l.views) DESC, c.content_id;
                """,
        };

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(LatestCte + body, connection);
        command.Parameters.AddWithValue("pid", NpgsqlDbType.Bigint, projectId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<MetricSummaryRecord>();
        while (await reader.ReadAsync(cancellationToken))
            list.Add(new MetricSummaryRecord(reader.GetInt64(0), reader.GetString(1),
                reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5)));
        return list;
    }

    public async Task<MetricSnapshot> AddSnapshotAsync(NewMetric metric, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            WITH ins AS (
                INSERT INTO content_metrics (content_platform_id, views, likes, comments, shares, recorded_by)
                SELECT @cp, COALESCE(@views, prev.views, 0), COALESCE(@likes, prev.likes, 0),
                       COALESCE(@comments, prev.comments, 0), COALESCE(@shares, prev.shares, 0), @by
                FROM (SELECT 1) AS one
                LEFT JOIN LATERAL (
                    SELECT views, likes, comments, shares FROM content_metrics
                    WHERE content_platform_id = @cp
                    ORDER BY recorded_at DESC, metric_id DESC LIMIT 1) prev ON TRUE
                RETURNING *)
            SELECT {Columns} FROM ins m LEFT JOIN users u ON u.user_id = m.recorded_by;
            """;
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("cp", NpgsqlDbType.Bigint, metric.ContentPlatformId);
        command.Parameters.AddWithValue("by", NpgsqlDbType.Bigint, metric.RecordedBy);
        AddNullable(command, "views", metric.Views);
        AddNullable(command, "likes", metric.Likes);
        AddNullable(command, "comments", metric.Comments);
        AddNullable(command, "shares", metric.Shares);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return Read(reader);
    }

    private async Task<IReadOnlyList<MetricSnapshot>> QueryAsync(string sql, long id, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, id);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<MetricSnapshot>();
        while (await reader.ReadAsync(cancellationToken)) list.Add(Read(reader));
        return list;
    }

    private static void AddNullable(NpgsqlCommand command, string name, long? value) =>
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Bigint) { Value = (object?)value ?? DBNull.Value });

    private static MetricSnapshot Read(NpgsqlDataReader r) => new(
        r.GetInt64(0), r.GetInt64(1), r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetInt64(5),
        r.IsDBNull(6) ? null : r.GetInt64(6), r.IsDBNull(7) ? null : r.GetString(7),
        new DateTimeOffset(r.GetDateTime(8)));
}
