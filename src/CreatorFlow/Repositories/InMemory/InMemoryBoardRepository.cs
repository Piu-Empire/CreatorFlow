using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryBoardRepository : IBoardRepository
{
    public List<ContentBoardCard> GetBoardCards(long projectId) =>
        InMemoryDataStore.Contents
            .Where(c => c.ProjectId == projectId)
            .Select(c => new ContentBoardCard
            {
                ContentId = c.Id,
                ProjectId = c.ProjectId,
                Code = $"CNT-{c.Id:000}",
                Title = c.Title,
                Description = c.Description,
                Sprint = c.Sprint,
                EstimatedDuration = c.EstimatedDuration,
                AssigneeUserId = c.AssigneeUserId,
                Status = c.Status,
                Priority = c.Priority,
                Deadline = c.Deadline,
                Platforms = new List<string>(c.Platforms),
                Tags = new List<string>(c.Tags),
                AssigneeName = c.AssigneeUserId.HasValue && InMemoryDataStore.UserNames.TryGetValue(c.AssigneeUserId.Value, out var name)
                    ? name
                    : null,
                HasPendingReview = InMemoryDataStore.Reviews.Any(r => r.ContentId == c.Id && r.Status == ReviewStatus.Pending),
            })
            .OrderBy(c => c.ContentId)
            .ToList();
}
