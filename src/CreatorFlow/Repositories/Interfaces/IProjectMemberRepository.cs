using CreatorFlow.Models.Enums;
using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IProjectMemberRepository
{
    /// <summary>Trả về Role của User trong Project, hoặc null nếu không phải thành viên.</summary>
    ProjectRole? GetRole(long projectId, long userId);

    /// <summary>Danh sách thành viên của Project (dùng chọn người phụ trách).</summary>
    List<ProjectMemberInfo> GetMembers(long projectId);
}