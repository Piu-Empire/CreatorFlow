using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryPlatformRepository : IPlatformRepository
{
    public List<Platform> GetActive() => InMemoryDataStore.Platforms.Where(p => p.IsActive).ToList();

    public List<ContentPlatform> GetByContentId(long contentId)
    {
        var record = InMemoryDataStore.Contents.FirstOrDefault(c => c.Id == contentId);
        if (record == null) return new List<ContentPlatform>();

        return record.Platforms
            .Select(name => new ContentPlatform
            {
                ContentId = contentId,
                PlatformName = name,
                PublicationStatus = record.PublishedPlatforms.Contains(name)
                    ? PublicationStatus.Published
                    : PublicationStatus.Planned,
            })
            .ToList();
    }
}
