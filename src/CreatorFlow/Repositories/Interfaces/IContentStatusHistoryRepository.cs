using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IContentStatusHistoryRepository
{
    void Add(ContentStatusHistory history);
}