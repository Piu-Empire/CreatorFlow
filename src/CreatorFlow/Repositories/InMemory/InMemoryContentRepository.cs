using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryContentRepository : IContentRepository
{
    public Content GetById(long contentId)
    {
        var r = InMemoryDataStore.Contents.FirstOrDefault(c => c.Id == contentId);
        if (r == null)
            return null!;

        return new Content
        {
            Id = r.Id,
            ProjectId = r.ProjectId,
            IdeaId = r.IdeaId,
            Title = r.Title,
            Status = r.Status,
            CreatedByUserId = r.CreatedByUserId,
            UpdatedAt = r.UpdatedAt,
        };
    }

    public void UpdateStatus(long contentId, ContentStatus newStatus)
    {
        var r = InMemoryDataStore.Contents.First(c => c.Id == contentId);
        r.Status = newStatus;
        r.UpdatedAt = DateTime.Now;
    }
}
