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

    private static void Apply(InMemoryContentRecord r, ContentDraft d)
    {
        r.Title = d.Title;
        r.Description = d.Description;
        r.Priority = d.Priority;
        r.Platforms = new List<string>(d.Platforms);
        r.Sprint = d.Sprint;
        r.Deadline = d.Deadline;
        r.EstimatedDuration = d.EstimatedDuration;
        r.AssigneeUserId = d.AssigneeUserId;
    }
}
