using CreatorFlow.Api.Models.ReviewQueue;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.ReviewQueue;

/// <summary>
/// Cài đặt Npgsql cho IReviewQueueRepository, viết theo database/01_schema.sql.
/// Chỉ lấy Review đang PENDING; mỗi lần gửi duyệt là một dòng Review mới nên không sợ trùng.
/// </summary>
public sealed class ReviewQueueRepository(IDbConnectionFactory connectionFactory) : IReviewQueueRepository
{
    private const string Sql = """
        SELECT r.content_id, c.project_id, c.title, COALESCE(u.display_name, ''),
               r.submitted_at, r.review_no
        FROM reviews r
        JOIN contents c ON c.content_id = r.content_id
        LEFT JOIN users u ON u.user_id = r.submitted_by
        WHERE c.project_id = @project_id
          AND r.status = 'PENDING'
        ORDER BY r.submitted_at, r.content_id;
        """;

    public async Task<IReadOnlyList<PendingReviewRecord>> GetPendingAsync(
        long projectId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("project_id", NpgsqlDbType.Bigint, projectId);

        var items = new List<PendingReviewRecord>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PendingReviewRecord(
                ContentId: reader.GetInt64(0),
                ProjectId: reader.GetInt64(1),
                ContentTitle: reader.GetString(2),
                SubmittedByName: reader.GetString(3),
                SubmittedAt: reader.GetDateTime(4),
                ReviewNo: reader.GetInt32(5)));
        }

        return items;
    }
}
