using CreatorFlow.Contracts.ContentDetail;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

/// <summary>
/// Activity timeline cho chế độ in-memory: dựng dữ liệu thô từ InMemoryDataStore rồi dùng chung
/// <see cref="ActivityTimeline.Compose"/> với API để cách gộp dòng không bị lệch.
/// </summary>
public class InMemoryActivityRepository : IActivityRepository
{
    public List<ActivityItem> GetActivity(long contentId)
    {
        var entries = new List<ActivityItemResponse>();

        foreach (var h in InMemoryDataStore.StatusHistory.Where(h => h.ContentId == contentId))
        {
            entries.Add(new ActivityItemResponse(
                h.ChangedAt,
                // ChangedByUserId là long? nên phải .Value trước khi tra dictionary key long
                h.ChangedByUserId.HasValue
                    ? InMemoryDataStore.UserNames.GetValueOrDefault(h.ChangedByUserId.Value, "?")
                    : "?",
                h.FromStatus is null ? h.ToStatus.ToString() : $"{h.FromStatus} → {h.ToStatus}",
                h.Note,
                ActivityKinds.StatusChange,
                h.FromStatus?.ToString(),
                h.ToStatus.ToString()));
        }

        foreach (var r in InMemoryDataStore.Reviews.Where(r => r.ContentId == contentId))
        {
            entries.Add(new ActivityItemResponse(
                r.SubmittedAt,
                InMemoryDataStore.UserNames.GetValueOrDefault(r.SubmittedByUserId, "?"),
                $"submitted Review #{r.ReviewNo}",
                null,
                ActivityKinds.ReviewSubmitted,
                ContentStatus.Editing.ToString(),
                ContentStatus.Review.ToString(),
                r.ReviewNo,
                ReviewStatus.Pending.ToString()));

            if (r.DecidedAt.HasValue)
            {
                entries.Add(new ActivityItemResponse(
                    r.DecidedAt.Value,
                    r.ReviewerUserId.HasValue
                        ? InMemoryDataStore.UserNames.GetValueOrDefault(r.ReviewerUserId.Value, "?")
                        : "",
                    r.Status == ReviewStatus.Approved ? $"approved Review #{r.ReviewNo}" : $"rejected Review #{r.ReviewNo}",
                    r.Feedback,
                    ActivityKinds.ReviewDecided,
                    ContentStatus.Review.ToString(),
                    (r.Status == ReviewStatus.Approved ? ContentStatus.Ready : ContentStatus.Editing).ToString(),
                    r.ReviewNo,
                    r.Status.ToString()));
            }
        }

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
}
