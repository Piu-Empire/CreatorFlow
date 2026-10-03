using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IActivityRepository (mục 6 UX spec).
/// Gộp 2 nguồn: ContentStatusHistory (đổi trạng thái) và Reviews (submit/approve/reject/feedback),
/// mỗi Review PENDING/APPROVED/REJECTED sinh ra 1-2 dòng Activity (lúc submit, và lúc quyết định
/// nếu đã có DecidedAt), rồi sắp chung theo Timestamp tăng dần.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    private readonly IDbSession _session;

    public ActivityRepository(IDbSession session) => _session = session;

    public List<ActivityItem> GetActivity(long contentId)
    {
        var items = new List<ActivityItem>();
        items.AddRange(GetStatusHistoryItems(contentId));
        items.AddRange(GetReviewItems(contentId));
        return items.OrderBy(i => i.Timestamp).ToList();
    }

    private List<ActivityItem> GetStatusHistoryItems(long contentId)
    {
        const string sql = @"
SELECT h.changedat, u.displayname, h.fromstatus, h.tostatus, h.note
FROM contentstatushistory h
JOIN users u ON u.id = h.changedbyuserid
WHERE h.contentid = @id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", contentId);

        var items = new List<ActivityItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new ActivityItem
            {
                Timestamp = reader.GetDateTime(0),
                ActorName = reader.GetString(1),
                Description = $"{reader.GetString(2)} → {reader.GetString(3)}",
                Feedback = reader.IsDBNull(4) ? null : reader.GetString(4),
            });
        }

        return items;
    }

    private List<ActivityItem> GetReviewItems(long contentId)
    {
        const string sql = @"
SELECT r.submittedat, su.displayname, r.reviewno, r.status, r.decidedat, ru.displayname, r.feedback
FROM reviews r
JOIN users su ON su.id = r.submittedbyuserid
LEFT JOIN users ru ON ru.id = r.revieweruserid
WHERE r.contentid = @id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", contentId);

        var items = new List<ActivityItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var reviewNo = reader.GetInt32(2);
            var status = reader.GetString(3);

            items.Add(new ActivityItem
            {
                Timestamp = reader.GetDateTime(0),
                ActorName = reader.GetString(1),
                Description = $"submitted Review #{reviewNo}",
            });

            if (!reader.IsDBNull(4))
            {
                items.Add(new ActivityItem
                {
                    Timestamp = reader.GetDateTime(4),
                    ActorName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Description = status == "Approved" ? $"approved Review #{reviewNo}" : $"rejected Review #{reviewNo}",
                    Feedback = reader.IsDBNull(6) ? null : reader.GetString(6),
                });
            }
        }

        return items;
    }
}