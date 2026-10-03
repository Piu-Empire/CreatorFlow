using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryReviewQueueRepository : IReviewQueueRepository
{
    public List<ReviewQueueItem> GetPendingReviews(long projectId) =>
        InMemoryDataStore.Reviews
            .Where(r => r.Status == ReviewStatus.Pending)
            .Join(InMemoryDataStore.Contents, r => r.ContentId, c => c.Id, (r, c) => (r, c))
            .Where(x => x.c.ProjectId == projectId)
            .Select(x => new ReviewQueueItem
            {
                ContentId = x.c.Id,
                ProjectId = x.c.ProjectId,
                ContentCode = $"CNT-{x.c.Id:000}",
                ContentTitle = x.c.Title,
                SubmittedByName = InMemoryDataStore.UserNames.GetValueOrDefault(x.r.SubmittedByUserId, "?"),
                SubmittedAt = x.r.SubmittedAt,
                ReviewNo = x.r.ReviewNo,
            })
            .OrderBy(i => i.SubmittedAt)
            .ToList();
}
