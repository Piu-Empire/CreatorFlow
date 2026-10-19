using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Tasks;

namespace CreatorFlow.Api.Tests.Services;

/// <summary>
/// ITaskRepository giả chỉ hiểu vai trò thành viên và assignment — phần mà IdeaService / ContentDetailService dùng.
/// Các hàm còn lại không được gọi nên ném NotSupportedException để test lộ ngay nếu service bắt đầu phụ thuộc vào chúng.
/// </summary>
internal sealed class MemberRolesFake : ITaskRepository
{
    public Dictionary<(long Project, long User), ProjectRole> Roles { get; } = new();
    public Dictionary<(long Content, long User), TaskAssignment> Assignments { get; } = new();
    public List<(long ContentId, long Actor, IReadOnlyList<NewAssignment> Additions)> Applied { get; } = new();
    public List<BoardAssignee> BoardAssignees { get; } = new();

    public void Assign(long contentId, long projectId, long userId) =>
        Assignments[(contentId, userId)] = new TaskAssignment(
            1, contentId, projectId, userId, $"CNT-{contentId:000}", "title", "Script", "Medium",
            AssignmentStatus.Assigned, 0, null, null, Array.Empty<string>(), 1, 0);

    public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out ProjectRole role) ? role : null);

    public Task<TaskAssignment?> GetAssignmentAsync(long contentId, long userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Assignments.GetValueOrDefault((contentId, userId)));

    public Task ApplyAssignmentsAsync(long contentId, long actorUserId, IReadOnlyList<long> cancelAssignmentIds,
        IReadOnlyList<DeadlineChange> deadlineChanges, IReadOnlyList<NewAssignment> additions, CancellationToken cancellationToken = default)
    {
        Applied.Add((contentId, actorUserId, additions));
        return Task.CompletedTask;
    }

    public bool ThrowOnMembers { get; set; }

    public Task<IReadOnlyList<ProjectMember>> GetMembersAsync(long projectId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnMembers) throw new InvalidOperationException("db down");
        return Task.FromResult<IReadOnlyList<ProjectMember>>(Roles
            .Where(r => r.Key.Item1 == projectId)
            .Select(r => new ProjectMember(r.Key.Item2, $"User {r.Key.Item2}", r.Value))
            .ToList());
    }

    public Task<ContentInfo?> GetContentAsync(long contentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<TaskAssignment>> GetMyTasksAsync(long projectId, long userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<TaskAssignment>> GetAssignmentsAsync(long contentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<BoardAssignee>> GetBoardAssigneesAsync(long projectId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BoardAssignee>>(BoardAssignees);
    public Task UpdateProgressAsync(long assignmentId, int percent, AssignmentStatus status, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateAssignmentDeadlineAsync(long assignmentId, DateOnly? deadline, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
