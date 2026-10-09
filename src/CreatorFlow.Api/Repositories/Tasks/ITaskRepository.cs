using CreatorFlow.Api.Models.Tasks;

namespace CreatorFlow.Api.Repositories.Tasks;

/// <summary>Truy cập assignment / My Tasks bằng Npgsql (chỉ Backend được chạm database).</summary>
public interface ITaskRepository
{
    /// <summary>Vai trò đang hoạt động của User trong Project, hoặc null nếu không phải thành viên.</summary>
    Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default);

    /// <summary>Thành viên đang hoạt động của Project (tài khoản ACTIVE).</summary>
    Task<IReadOnlyList<ProjectMember>> GetMembersAsync(long projectId, CancellationToken cancellationToken = default);

    Task<ContentInfo?> GetContentAsync(long contentId, CancellationToken cancellationToken = default);

    /// <summary>Công việc của User trong Project; bỏ assignment đã huỷ và Content đã Archived.</summary>
    Task<IReadOnlyList<TaskAssignment>> GetMyTasksAsync(long projectId, long userId, CancellationToken cancellationToken = default);

    /// <summary>Assignment đang hiệu lực (không huỷ) của một Creator trên một Content.</summary>
    Task<TaskAssignment?> GetAssignmentAsync(long contentId, long userId, CancellationToken cancellationToken = default);

    /// <summary>Mọi assignment đang hiệu lực của Content theo thứ tự được giao.</summary>
    Task<IReadOnlyList<TaskAssignment>> GetAssignmentsAsync(long contentId, CancellationToken cancellationToken = default);

    /// <summary>Creator được giao của mọi Content trong Project (để hiện avatar trên thẻ Board).</summary>
    Task<IReadOnlyList<BoardAssignee>> GetBoardAssigneesAsync(long projectId, CancellationToken cancellationToken = default);

    Task UpdateProgressAsync(long assignmentId, int percent, AssignmentStatus status, CancellationToken cancellationToken = default);

    Task UpdateAssignmentDeadlineAsync(long assignmentId, DateOnly? deadline, CancellationToken cancellationToken = default);

    /// <summary>Huỷ assignment, đổi deadline và tạo assignment mới trong MỘT transaction.</summary>
    Task ApplyAssignmentsAsync(
        long contentId,
        long actorUserId,
        IReadOnlyList<long> cancelAssignmentIds,
        IReadOnlyList<DeadlineChange> deadlineChanges,
        IReadOnlyList<NewAssignment> additions,
        CancellationToken cancellationToken = default);
}
