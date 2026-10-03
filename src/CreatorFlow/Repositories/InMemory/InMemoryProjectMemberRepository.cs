using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryProjectMemberRepository : IProjectMemberRepository
{
    public ProjectRole? GetRole(long projectId, long userId) =>
        InMemoryDataStore.Members.TryGetValue((projectId, userId), out var role) ? role : null;

    public List<ProjectMemberInfo> GetMembers(long projectId) =>
        InMemoryDataStore.Members
            .Where(kv => kv.Key.ProjectId == projectId)
            .Select(kv => new ProjectMemberInfo
            {
                UserId = kv.Key.UserId,
                Name = InMemoryDataStore.UserNames.GetValueOrDefault(kv.Key.UserId, $"User #{kv.Key.UserId}"),
                Role = kv.Value,
            })
            .OrderBy(m => m.Name)
            .ToList();
}
