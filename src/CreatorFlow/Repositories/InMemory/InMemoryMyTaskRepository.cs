using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

/// <summary>Assignment lấy từ InMemoryDataStore.Assignments (mỗi Creator một dòng), gộp thông tin Content để hiển thị.</summary>
public class InMemoryMyTaskRepository : IMyTaskRepository
{
    public List<MyTaskItem> GetMyTasks(long projectId, long assigneeUserId) =>
        InMemoryDataStore.Assignments
            .Where(a => a.AssigneeUserId == assigneeUserId && a.Status != AssignmentStatus.Cancelled)
            .Select(a => (Assignment: a, Content: InMemoryDataStore.Contents.FirstOrDefault(c => c.Id == a.ContentId)))
            .Where(x => x.Content is not null
                        && x.Content.ProjectId == projectId
                        && x.Content.Status != ContentStatus.Archived)
            .Select(x => ToItem(x.Assignment, x.Content!))
            .OrderBy(t => t.ContentId)
            .ToList();

    public MyTaskItem? GetAssignment(long contentId, long assigneeUserId)
    {
        var a = InMemoryDataStore.Assignments.FirstOrDefault(x =>
            x.ContentId == contentId && x.AssigneeUserId == assigneeUserId && x.Status != AssignmentStatus.Cancelled);
        var c = InMemoryDataStore.Contents.FirstOrDefault(x => x.Id == contentId);
        return a is null || c is null ? null : ToItem(a, c);
    }

    public List<MyTaskItem> GetAssignments(long contentId)
    {
        var c = InMemoryDataStore.Contents.FirstOrDefault(x => x.Id == contentId);
        if (c is null) return new List<MyTaskItem>();

        return InMemoryDataStore.Assignments
            .Where(a => a.ContentId == contentId && a.Status != AssignmentStatus.Cancelled)
            .OrderBy(a => a.Id)
            .Select(a => ToItem(a, c))
            .ToList();
    }

    public void UpdateProgress(long assignmentId, int percent, AssignmentStatus status)
    {
        var a = InMemoryDataStore.Assignments.First(x => x.Id == assignmentId);
        a.ProgressPercent = percent;
        a.Status = status;
        Touch(a.ContentId);
    }

    public void UpdateAssignmentDeadline(long assignmentId, DateTime? deadline)
    {
        var a = InMemoryDataStore.Assignments.First(x => x.Id == assignmentId);
        a.Deadline = deadline;
        Touch(a.ContentId);
    }

    public void AddAssignment(long contentId, long assigneeUserId, long assignedByUserId, DateTime? deadline)
    {
        InMemoryDataStore.Assignments.Add(new InMemoryAssignmentRecord
        {
            Id = InMemoryDataStore.NextAssignmentId(),
            ContentId = contentId,
            AssigneeUserId = assigneeUserId,
            AssignedByUserId = assignedByUserId,
            Status = AssignmentStatus.Assigned, // người mới bắt đầu từ 0%
            ProgressPercent = 0,
            Deadline = deadline,
        });
        Touch(contentId);
    }

    public void CancelAssignment(long assignmentId)
    {
        var a = InMemoryDataStore.Assignments.First(x => x.Id == assignmentId);
        a.Status = AssignmentStatus.Cancelled;
        Touch(a.ContentId);
    }

    private static void Touch(long contentId)
    {
        var c = InMemoryDataStore.Contents.FirstOrDefault(x => x.Id == contentId);
        if (c is not null) c.UpdatedAt = DateTime.Now;
    }

    private static MyTaskItem ToItem(InMemoryAssignmentRecord a, InMemoryContentRecord c)
    {
        var team = InMemoryDataStore.Assignments
            .Where(x => x.ContentId == c.Id && x.Status != AssignmentStatus.Cancelled)
            .ToList();

        return new MyTaskItem
        {
            AssignmentId = a.Id,
            ContentId = c.Id,
            ProjectId = c.ProjectId,
            AssigneeUserId = a.AssigneeUserId,
            ContentCode = $"CNT-{c.Id:000}",
            ContentTitle = c.Title,
            ContentStage = c.Status,
            Status = a.Status,
            Priority = c.Priority,
            Deadline = a.Deadline ?? c.Deadline, // deadline riêng của người này, rơi về deadline chung của Content
            ProgressPercent = a.ProgressPercent,
            TeamTotal = team.Count,
            TeamDone = team.Count(x => x.Status == AssignmentStatus.Completed),
            AssignedByName = a.AssignedByUserId.HasValue
                             && InMemoryDataStore.UserNames.TryGetValue(a.AssignedByUserId.Value, out var name)
                ? name
                : null,
            Platforms = new List<string>(c.Platforms),
        };
    }
}