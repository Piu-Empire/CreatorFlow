using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryBoardRepository : IBoardRepository
{
    public List<ContentBoardCard> GetBoardCards(long projectId) =>
        InMemoryDataStore.Contents
            .Where(c => c.ProjectId == projectId)
            .Select(c =>
            {
                // Mọi Creator đang được giao (assignment chưa Cancelled), theo thứ tự được giao.
                var assignees = InMemoryDataStore.Assignments
                    .Where(a => a.ContentId == c.Id && a.Status != AssignmentStatus.Cancelled)
                    .OrderBy(a => a.Id)
                    .Select(a => new CardAssignee(
                        a.AssigneeUserId,
                        InMemoryDataStore.UserNames.TryGetValue(a.AssigneeUserId, out var n) ? n : $"User {a.AssigneeUserId}",
                        a.Status,
                        a.ProgressPercent,
                        a.Deadline ?? c.Deadline))
                    .ToList();

                return new ContentBoardCard
                {
                    ContentId = c.Id,
                    ProjectId = c.ProjectId,
                    Code = $"CNT-{c.Id:000}",
                    Title = c.Title,
                    Description = c.Description,
                    Sprint = c.Sprint,
                    EstimatedDuration = c.EstimatedDuration,
                    Assignees = assignees,
                    AssigneeUserId = assignees.FirstOrDefault()?.UserId,
                    AssigneeName = assignees.FirstOrDefault()?.Name,
                    Status = c.Status,
                    Priority = c.Priority,
                    Deadline = c.Deadline, // deadline chung của Content (mỗi Creator có deadline riêng ở assignment)
                    Platforms = new List<string>(c.Platforms),
                    Tags = new List<string>(c.Tags),
                    HasPendingReview = InMemoryDataStore.Reviews.Any(r => r.ContentId == c.Id && r.Status == ReviewStatus.Pending),
                };
            })
            .OrderBy(c => c.ContentId)
            .ToList();
}