using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryActivityRepository : IActivityRepository
{
    public List<ActivityItem> GetActivity(long contentId)
    {
        var items = new List<ActivityItem>();

        foreach (var h in InMemoryDataStore.StatusHistory.Where(h => h.ContentId == contentId))
        {
            items.Add(new ActivityItem
            {
                Timestamp = h.ChangedAt,
                // ChangedByUserId là long? nên phải .Value trước khi tra dictionary key long
                ActorName = h.ChangedByUserId.HasValue
                    ? InMemoryDataStore.UserNames.GetValueOrDefault(h.ChangedByUserId.Value, "?")
                    : "?",
                Description = $"{h.FromStatus} → {h.ToStatus}",
                Feedback = h.Note,
            });
        }

        foreach (var r in InMemoryDataStore.Reviews.Where(r => r.ContentId == contentId))
        {
            items.Add(new ActivityItem
            {
                Timestamp = r.SubmittedAt,
                ActorName = InMemoryDataStore.UserNames.GetValueOrDefault(r.SubmittedByUserId, "?"),
                Description = $"submitted Review #{r.ReviewNo}",
            });

            if (r.DecidedAt.HasValue)
            {
                items.Add(new ActivityItem
                {
                    Timestamp = r.DecidedAt.Value,
                    ActorName = r.ReviewerUserId.HasValue
                        ? InMemoryDataStore.UserNames.GetValueOrDefault(r.ReviewerUserId.Value, "?")
                        : "",
                    Description = r.Status == ReviewStatus.Approved ? $"approved Review #{r.ReviewNo}" : $"rejected Review #{r.ReviewNo}",
                    Feedback = r.Feedback,
                });
            }
        }

        return items.OrderBy(i => i.Timestamp).ToList();
    }
}