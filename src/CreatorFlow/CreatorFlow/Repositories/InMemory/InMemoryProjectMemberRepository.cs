using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryProjectMemberRepository : IProjectMemberRepository
{
    public ProjectRole? GetRole(long projectId, long userId) =>
        InMemoryDataStore.Members.TryGetValue((projectId, userId), out var role) ? role : null;
}
