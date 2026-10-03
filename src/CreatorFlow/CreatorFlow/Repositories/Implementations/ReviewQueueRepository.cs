using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>Cài đặt Npgsql cho IReviewQueueRepository (dành cho Owner/Manager — mục 2 UX spec).</summary>
public class ReviewQueueRepository : IReviewQueueRepository
{
    private readonly IDbSession _session;

    public ReviewQueueRepository(IDbSession session) => _session = session;

    public List<ReviewQueueItem> GetPendingReviews(long projectId)
    {
        const string sql = @"
SELECT
    c.id,
    c.projectid,
    'CNT-' || LPAD(c.id::text, 3, '0') AS code,
    c.title,
    u.displayname,
    r.submittedat,
    r.reviewno
FROM reviews r
JOIN contents c ON c.id = r.contentid
JOIN users u ON u.id = r.submittedbyuserid
WHERE c.projectid = @projectId AND r.status = 'Pending'
ORDER BY r.submittedat";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("projectId", projectId);

        var items = new List<ReviewQueueItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new ReviewQueueItem
            {
                ContentId = reader.GetInt64(0),
                ProjectId = reader.GetInt64(1),
                ContentCode = reader.GetString(2),
                ContentTitle = reader.GetString(3),
                SubmittedByName = reader.GetString(4),
                SubmittedAt = reader.GetDateTime(5),
                ReviewNo = reader.GetInt32(6),
            });
        }

        return items;
    }
}