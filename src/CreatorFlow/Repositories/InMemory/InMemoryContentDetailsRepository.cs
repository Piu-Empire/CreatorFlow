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
        InMemoryDataStore.Contents.Add(record);
        return newId;
    }

    public void Update(long contentId, ContentDraft draft)
    {
        var record = InMemoryDataStore.Contents.First(c => c.Id == contentId);
        Apply(record, draft);
        record.UpdatedAt = DateTime.Now;
    }

    public Content? GetDetail(long contentId)
    {
        var r = InMemoryDataStore.Contents.FirstOrDefault(c => c.Id == contentId);
        if (r == null)
            return null;

        return new Content
        {
            ContentId = r.Id,
            ProjectId = r.ProjectId,
            SourceIdeaId = r.IdeaId,
            Title = r.Title,
            Description = r.Description,
            Script = string.IsNullOrEmpty(r.Script) ? null : r.Script,
            ContentType = string.IsNullOrEmpty(r.ContentType) ? null : r.ContentType,
            Priority = r.Priority,
            Status = r.Status,
            Deadline = r.Deadline,
            PlannedPublishAt = r.PlannedPublishAt,
            CreatedBy = r.CreatedByUserId,
            CreatedAt = r.UpdatedAt,
            UpdatedAt = r.UpdatedAt,
        };
    }

    private static void Apply(InMemoryContentRecord r, ContentDraft d)
    {
        r.Title = d.Title;
        r.Description = d.Description;
        r.Script = d.Script;
        r.ContentType = d.ContentType;
        r.PlannedPublishAt = d.PlannedPublishAt;
        r.Priority = d.Priority;
        r.Sprint = d.Sprint;
        r.Deadline = d.Deadline;
        r.EstimatedDuration = d.EstimatedDuration;
        r.AssigneeUserId = d.AssigneeUserId;
        r.Platforms = d.Platforms.ToList(); // nền tảng đã được ContentService chuẩn hóa tên
    }
}