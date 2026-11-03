using CreatorFlow.Api.Models.Statistics;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Statistics;

/// <summary>
/// SCRUM-55: một câu SQL tổng hợp theo Creator trong đúng một Project.
/// - <c>latest</c>: snapshot mới nhất của mỗi ContentPlatform (tái dùng mẫu SCRUM-52) để nhiều lần nhập không đếm trùng.
/// - <c>rollup</c>: đếm content theo trạng thái; Archived không tính (khớp Board).
/// - Overdue: dùng đúng luật SCRUM-47 (Published/Archived là xong; deadline là NGÀY, so theo UTC như TaskRepository).
/// - Join <c>project_members</c> active để creator đã rời Project không còn dòng.
/// </summary>
public sealed class StatisticsRepository(IDbConnectionFactory connectionFactory) : IStatisticsRepository
{
    private const string Sql = """
        WITH latest AS (
            SELECT DISTINCT ON (m.content_platform_id)
                   c.created_by,
                   m.views, m.likes, m.comments, m.shares
            FROM content_metrics m
            JOIN content_platforms cp ON cp.content_platform_id = m.content_platform_id
            JOIN contents c ON c.content_id = cp.content_id
            WHERE c.project_id = @pid
              AND c.status <> 'ARCHIVED'
            ORDER BY m.content_platform_id, m.recorded_at DESC, m.metric_id DESC
        ),
        rollup AS (
            SELECT c.created_by,
                   COUNT(*) AS total,
                   COUNT(*) FILTER (WHERE c.status IN ('SCRIPT','PRODUCTION','EDITING','REVIEW','READY')) AS in_progress,
                   COUNT(*) FILTER (WHERE c.status = 'PUBLISHED') AS published,
                   COUNT(*) FILTER (WHERE c.status NOT IN ('PUBLISHED','ARCHIVED')
                                      AND c.deadline IS NOT NULL
                                      AND (c.deadline AT TIME ZONE 'UTC')::date
                                          < (now() AT TIME ZONE 'UTC')::date) AS overdue
            FROM contents c
            WHERE c.project_id = @pid
              AND c.status <> 'ARCHIVED'
            GROUP BY c.created_by
        )
        SELECT u.user_id,
               COALESCE(NULLIF(u.display_name, ''), u.email),
               r.total, r.in_progress, r.published, r.overdue,
               COALESCE(SUM(l.views), 0)::bigint,
               COALESCE(SUM(l.likes), 0)::bigint,
               COALESCE(SUM(l.comments), 0)::bigint,
               COALESCE(SUM(l.shares), 0)::bigint
        FROM rollup r
        JOIN project_members pm
          ON pm.user_id = r.created_by AND pm.project_id = @pid AND pm.is_active
        JOIN users u ON u.user_id = r.created_by
        LEFT JOIN latest l ON l.created_by = r.created_by
        GROUP BY u.user_id, u.display_name, u.email, r.total, r.in_progress, r.published, r.overdue
        ORDER BY r.total DESC, u.user_id;
        """;

    public async Task<IReadOnlyList<CreatorStatisticsRecord>> GetCreatorStatisticsAsync(
        long projectId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("pid", NpgsqlDbType.Bigint, projectId);

        var rows = new List<CreatorStatisticsRecord>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new CreatorStatisticsRecord(
                reader.GetInt64(0),
                reader.GetString(1),
                checked((int)reader.GetInt64(2)),
                checked((int)reader.GetInt64(3)),
                checked((int)reader.GetInt64(4)),
                checked((int)reader.GetInt64(5)),
                reader.GetInt64(6),
                reader.GetInt64(7),
                reader.GetInt64(8),
                reader.GetInt64(9)));
        }

        return rows;
    }
}
