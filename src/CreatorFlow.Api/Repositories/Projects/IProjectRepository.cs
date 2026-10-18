using CreatorFlow.Api.Models.Projects;

namespace CreatorFlow.Api.Repositories.Projects;

public interface IProjectRepository
{
    Task<IReadOnlyList<ProjectRecord>> ListAsync(long userId, CancellationToken token);
    Task<ProjectRecord?> FindAsync(long projectId, long userId, CancellationToken token);
    Task<ProjectMembersRecord?> ListMembersAsync(long projectId, long actorUserId, CancellationToken token);
    Task<T> InTransactionAsync<T>(Func<IProjectTransaction, CancellationToken, Task<T>> action, CancellationToken token);
}

public interface IProjectTransaction
{
    Task<ProjectRecord?> LockAsync(long projectId, long userId, CancellationToken token);
    Task<ProjectMemberRecord?> LockMemberAsync(long projectId, long memberUserId, CancellationToken token);
    Task<int> CountActiveOwnersAsync(long projectId, CancellationToken token);
    Task<long?> FindActiveOwnerAsync(long projectId, long exceptUserId, CancellationToken token);
    Task SetProjectOwnerAsync(long projectId, long userId, CancellationToken token);
    Task UpdateMemberRoleAsync(long projectId, long memberUserId, string role, CancellationToken token);
    Task RemoveMemberAsync(long projectId, long memberUserId, CancellationToken token);
    Task<ProjectRecord> CreateAsync(long userId, string name, string? description, CancellationToken token);
    Task<ProjectRecord> UpdateAsync(long projectId, long userId, string name, string? description, CancellationToken token);
    Task<ProjectRecord> ArchiveAsync(long projectId, long userId, CancellationToken token);
    Task<ProjectRecord> RestoreAsync(long projectId, long userId, CancellationToken token);
    Task DeleteAsync(long projectId, CancellationToken token);
}
