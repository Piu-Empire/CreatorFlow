using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryContentStatusHistoryRepository : IContentStatusHistoryRepository
{
    public void Add(ContentStatusHistory history)
    {
        history.Id = InMemoryDataStore.NextContentStatusHistoryId++;
        InMemoryDataStore.StatusHistory.Add(history);
    }
}
