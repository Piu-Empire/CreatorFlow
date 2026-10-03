using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IProjectMemberRepository
{
    /// <summary>Trả về Role của User trong Project, hoặc null nếu không phải thành viên.</summary>
    ProjectRole? GetRole(long projectId, long userId);
}