using CreatorFlow.Contracts.ContentDetail;
using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IActivityRepository (mục 6 UX spec).
/// Gộp 2 nguồn: ContentStatusHistory (đổi trạng thái) và Reviews (submit/approve/reject/feedback),
/// rồi dùng chung <see cref="ActivityTimeline.Compose"/> với API để không lệch cách hiển thị.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    private readonly IDbSession _session;

    public ActivityRepository(IDbSession session) => _session = session;

    public List<ActivityItem> GetActivity(long contentId)
    {
        var entries = new List<ActivityItemResponse>();
        entries.AddRange(GetStatusHistoryEntries(contentId));
        entries.AddRange(GetReviewEntries(contentId));

        return ActivityTimeline.Compose(entries)
            .Select(i => new ActivityItem
            {
                Timestamp = i.Timestamp,
                ActorName = i.ActorName,
                Description = i.Description,
                Feedback = i.Feedback,
                Kind = i.Kind,
                FromStatus = i.FromStatus,
                ToStatus = i.ToStatus,
                ReviewNo = i.ReviewNo,
                ReviewStatus = i.ReviewStatus,
            })
            .ToList();
    }

    private List<ActivityItemResponse> GetStatusHistoryEntries(long contentId)
    {
        const string sql = @"
SELECT h.changedat, u.displayname, h.fromstatus, h.tostatus, h.note
FROM contentstatushistory h
JOIN users u ON u.id = h.changedbyuserid
WHERE h.contentid = @id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", contentId);

        var entries = new List<ActivityItemResponse>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string? from = reader.IsDBNull(2) ? null : ToStatusName(reader.GetString(2));
            string to = ToStatusName(reader.GetString(3));
            entries.Add(new ActivityItemResponse(
                reader.GetDateTime(0),
                reader.GetString(1),
                from is null ? to : $"{from} → {to}",
                reader.IsDBNull(4) ? null : reader.GetString(4),
                ActivityKinds.StatusChange,
                from,
                to));
        }

        return entries;
    }

    private List<ActivityItemResponse> GetReviewEntries(long contentId)
    {
        const string sql = @"
SELECT r.submittedat, su.displayname, r.reviewno, r.status, r.decidedat, ru.displayname, r.feedback
FROM reviews r
JOIN users su ON su.id = r.submittedbyuserid
LEFT JOIN users ru ON ru.id = r.revieweruserid
WHERE r.contentid = @id";

        using var cmd = _session.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", contentId);

        var entries = new List<ActivityItemResponse>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int reviewNo = reader.GetInt32(2);
            string status = ToReviewStatusName(reader.GetString(3));

            entries.Add(new ActivityItemResponse(
                reader.GetDateTime(0),
                reader.GetString(1),
                $"submitted Review #{reviewNo}",
                null,
                ActivityKinds.ReviewSubmitted,
                ContentStatus.Editing.ToString(),
                ContentStatus.Review.ToString(),
                reviewNo,
                ReviewStatus.Pending.ToString()));

            if (!reader.IsDBNull(4))
            {
                bool approved = status == ReviewStatus.Approved.ToString();
                entries.Add(new ActivityItemResponse(
                    reader.GetDateTime(4),
                    reader.IsDBNull(5) ? "" : reader.GetString(5),
                    approved ? $"approved Review #{reviewNo}" : $"rejected Review #{reviewNo}",
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    ActivityKinds.ReviewDecided,
                    ContentStatus.Review.ToString(),
                    (approved ? ContentStatus.Ready : ContentStatus.Editing).ToString(),
                    reviewNo,
                    status));
            }
        }

        return entries;
    }

    /// <summary>"EDITING" → "Editing"; giá trị đã đúng PascalCase thì giữ nguyên.</summary>
    private static string ToStatusName(string databaseValue) =>
        Enum.TryParse<ContentStatus>(databaseValue, ignoreCase: true, out var status)
            ? status.ToString()
            : databaseValue;

    /// <summary>"APPROVED" → "Approved".</summary>
    private static string ToReviewStatusName(string databaseValue) =>
        Enum.TryParse<ReviewStatus>(databaseValue, ignoreCase: true, out var status)
            ? status.ToString()
            : databaseValue;
}
