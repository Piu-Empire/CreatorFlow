using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

/// <summary>Không làm gì cả — dữ liệu trong RAM không cần transaction thật.</summary>
public class InMemoryUnitOfWork : IUnitOfWork
{
    public void Begin() { }
    public void Commit() { }
    public void Rollback() { }
    public void Dispose() { }
}
