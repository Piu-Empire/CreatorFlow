using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryContentDetailsRepository : IContentDetailsRepository
{
    public long Create(long projectId, ContentStatus status, ContentDraft draft, long createdByUserId)
    {
        long newId = InMemoryDataStore.Contents.Count > 0 ? InMemoryDataStore.Contents.Max(c => c.Id) + 1 : 1;
        var record = new InMemoryContentRecord
        {
            Id = newId,
            ProjectId = projectId,
            Status = status,
            CreatedByUserId = createdByUserId,
            UpdatedAt = DateTime.Now,
        };
        Apply(record, draft);
        record.AssignedByUserId = createdByUserId; // người tạo = người giao việc (hiện ở cột "Giao bởi" của My Tasks)
        InMemoryDataStore.Contents.Add(record);
        if (record.AssigneeUserId is long assignee)
        {
            // Content tạo kèm người phụ trách → sinh assignment đầu tiên (Assigned, 0%).
            InMemoryDataStore.Assignments.Add(new InMemoryAssignmentRecord
            {
                Id = InMemoryDataStore.NextAssignmentId(),
                ContentId = newId,
                AssigneeUserId = assignee,
                AssignedByUserId = createdByUserId,
                Status = AssignmentStatus.Assigned,
                ProgressPercent = 0,
                Deadline = record.Deadline,
            });
        }
        return newId;
    }

    public void Update(long contentId, ContentDraft draft)
    {
        var record = InMemoryDataStore.Contents.First(c => c.Id == contentId);
        long? assigneeBefore = record.AssigneeUserId;
        Apply(record, draft);
        // Đổi người phụ trách chỉ đi qua ContentService.AssignContent (kiểm tra quyền + Project) nên Update giữ nguyên assignee.
        record.AssigneeUserId = assigneeBefore;
        record.UpdatedAt = DateTime.Now;
    }

    private static void Apply(InMemoryContentRecord r, ContentDraft d)
    {
        r.Title = d.Title;
        r.Description = d.Description;
        r.Priority = d.Priority;
        r.Sprint = d.Sprint;
        r.Deadline = d.Deadline;
        r.EstimatedDuration = d.EstimatedDuration;
        r.AssigneeUserId = d.AssigneeUserId;
        r.Platforms = d.Platforms.ToList(); // nền tảng đã được ContentService chuẩn hóa tên
    }
}