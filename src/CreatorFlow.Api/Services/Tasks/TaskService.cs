using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Tasks;

namespace CreatorFlow.Api.Services.Tasks;

/// <summary>
/// Nghiệp vụ giao việc / My Tasks. Mọi quyền được kiểm tra Ở ĐÂY (không tin giao diện):
///   - đọc: chỉ thành viên đang hoạt động của Project;
///   - progress: chỉ Creator được giao, mỗi người một assignment riêng, 0–100;
///   - deadline: chỉ Owner/Manager, đặt riêng từng Creator, không được đặt về quá khứ;
///   - giao việc: chỉ Owner/Manager (hoặc Creator tự nhận Content do chính mình tạo), người nhận phải là Creator của đúng Project.
/// </summary>
public sealed class TaskService(ITaskRepository repository, CurrentAuthenticatedUser current, TimeProvider timeProvider)
{
    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    // ------------------------------------------------------------------ đọc

    public async Task<AuthOperationResult<MyTasksResponse>> GetMyTasksAsync(long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<MyTasksResponse>();
        if (await repository.GetRoleAsync(projectId, user.UserId, token) is null) return NotMember<MyTasksResponse>();

        var tasks = await repository.GetMyTasksAsync(projectId, user.UserId, token);
        return new(new MyTasksResponse(tasks.Select(ToResponse).ToList()));
    }

    /// <summary>Vai trò của người đang đăng nhập trong Project (403 nếu không phải thành viên).</summary>
    public async Task<AuthOperationResult<ProjectRoleResponse>> GetMyRoleAsync(long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ProjectRoleResponse>();
        ProjectRole? role = await repository.GetRoleAsync(projectId, user.UserId, token);
        return role is null ? NotMember<ProjectRoleResponse>() : new(new ProjectRoleResponse(role.Value.ToString()));
    }

    public async Task<AuthOperationResult<AssignableMembersResponse>> GetAssignableMembersAsync(long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<AssignableMembersResponse>();
        ProjectRole? role = await repository.GetRoleAsync(projectId, user.UserId, token);
        if (role is null) return NotMember<AssignableMembersResponse>();

        var members = await repository.GetMembersAsync(projectId, token);
        IEnumerable<ProjectMember> allowed =
            TaskRules.CanAssign(role) ? members.Where(m => TaskRules.CanBeAssignee(m.Role))
            : members.Where(m => m.UserId == user.UserId && TaskRules.CanBeAssignee(m.Role)); // Creator chỉ thấy chính mình

        return new(new AssignableMembersResponse(
            allowed.Select(m => new AssignableMemberResponse(m.UserId, m.DisplayName, m.Role.ToString())).ToList()));
    }

    public async Task<AuthOperationResult<AssignmentsResponse>> GetAssignmentsAsync(long contentId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<AssignmentsResponse>();
        ContentInfo? content = await repository.GetContentAsync(contentId, token);
        if (content is null) return ContentNotFound<AssignmentsResponse>();
        if (await repository.GetRoleAsync(content.ProjectId, user.UserId, token) is null) return NotMember<AssignmentsResponse>();

        var rows = await repository.GetAssignmentsAsync(contentId, token);
        return new(new AssignmentsResponse(rows.Select(ToResponse).ToList()));
    }

    public async Task<AuthOperationResult<BoardAssigneesResponse>> GetBoardAssigneesAsync(long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<BoardAssigneesResponse>();
        if (await repository.GetRoleAsync(projectId, user.UserId, token) is null) return NotMember<BoardAssigneesResponse>();

        var rows = await repository.GetBoardAssigneesAsync(projectId, token);
        return new(new BoardAssigneesResponse(rows
            .Select(r => new BoardAssigneeResponse(r.ContentId, r.UserId, r.DisplayName, r.Status.ToString(), r.ProgressPercent, r.Deadline))
            .ToList()));
    }

    // ------------------------------------------------------------------ progress

    /// <summary>Creator cập nhật tiến độ công việc của CHÍNH MÌNH (người gọi lấy từ JWT, không nhận từ request).</summary>
    public async Task<AuthOperationResult<MyTaskResponse>> UpdateProgressAsync(
        long contentId, UpdateProgressRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<MyTaskResponse>();
        if (TaskRules.ValidateProgress(request.ProgressPercent) is { } progressError)
            return AuthOperationResult<MyTaskResponse>.Fail("validation", progressError, "progressPercent");

        TaskAssignment? assignment = await repository.GetAssignmentAsync(contentId, user.UserId, token);
        if (assignment is null || await repository.GetRoleAsync(assignment.ProjectId, user.UserId, token) is null)
            return Forbidden<MyTaskResponse>("Bạn không được giao công việc này.");

        if (IsLocked(assignment.ContentStage))
            return Locked<MyTaskResponse>("Nội dung đã Published nên không thể đổi tiến độ.");

        AssignmentStatus newStatus = TaskRules.StatusForProgress(request.ProgressPercent);
        if (assignment.ProgressPercent == request.ProgressPercent && assignment.Status == newStatus)
            return new(ToResponse(assignment));

        await repository.UpdateProgressAsync(assignment.AssignmentId, request.ProgressPercent, newStatus, token);

        // Làm mới số liệu nhóm (TeamDone) sau khi ghi.
        TaskAssignment refreshed = await repository.GetAssignmentAsync(contentId, user.UserId, token)
            ?? assignment with { ProgressPercent = request.ProgressPercent, Status = newStatus };
        return new(ToResponse(refreshed));
    }

    // ------------------------------------------------------------------ deadline

    /// <summary>Owner/Manager đổi (hoặc bỏ) deadline RIÊNG của một Creator trên một Content.</summary>
    public async Task<AuthOperationResult<MyTaskResponse>> ChangeDeadlineAsync(
        long contentId, long assigneeUserId, ChangeDeadlineRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<MyTaskResponse>();

        TaskAssignment? assignment = await repository.GetAssignmentAsync(contentId, assigneeUserId, token);
        if (assignment is null)
            return AuthOperationResult<MyTaskResponse>.Fail("not_found", "Không tìm thấy công việc được giao.", status: 404);

        ProjectRole? role = await repository.GetRoleAsync(assignment.ProjectId, user.UserId, token);
        if (role is null) return NotMember<MyTaskResponse>();
        if (!TaskRules.CanChangeDeadline(role)) return Forbidden<MyTaskResponse>("Chỉ Owner/Manager mới được đổi deadline.");

        if (IsLocked(assignment.ContentStage))
            return Locked<MyTaskResponse>("Nội dung đã Published nên không thể đổi deadline.");

        if (TaskRules.ValidateNewDeadline(request.Deadline, assignment.Deadline, Today) is { } deadlineError)
            return AuthOperationResult<MyTaskResponse>.Fail("validation", deadlineError, "deadline");

        if (request.Deadline == assignment.Deadline) return new(ToResponse(assignment));

        await repository.UpdateAssignmentDeadlineAsync(assignment.AssignmentId, request.Deadline, token);
        return new(ToResponse(assignment with { Deadline = request.Deadline }));
    }

    // ------------------------------------------------------------------ giao việc

    public async Task<AuthOperationResult<AssignmentsResponse>> AssignContentAsync(
        long contentId, AssignContentRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<AssignmentsResponse>();

        ContentInfo? content = await repository.GetContentAsync(contentId, token);
        if (content is null) return ContentNotFound<AssignmentsResponse>();

        ProjectRole? role = await repository.GetRoleAsync(content.ProjectId, user.UserId, token);
        if (role is null) return NotMember<AssignmentsResponse>();

        var requested = request.Assignments ?? Array.Empty<AssigneeDeadlineRequest>();
        if (requested.Count == 0)
            return AuthOperationResult<AssignmentsResponse>.Fail("validation", "Hãy chọn ít nhất một Creator để giao việc.", "assignments");
        if (requested.Select(a => a.UserId).Distinct().Count() != requested.Count)
            return AuthOperationResult<AssignmentsResponse>.Fail("validation", "Một Creator chỉ được xuất hiện một lần trong danh sách giao việc.", "assignments");

        var existing = await repository.GetAssignmentsAsync(contentId, token);

        // Owner/Manager giao cho bất kỳ Creator nào; Creator chỉ được tự nhận Content do chính mình tạo (khi chưa có ai khác).
        bool selfAssign = role == ProjectRole.Creator
            && content.CreatedBy == user.UserId
            && requested.Count == 1 && requested[0].UserId == user.UserId
            && existing.All(e => e.AssigneeUserId == user.UserId);
        if (!TaskRules.CanAssign(role) && !selfAssign)
            return Forbidden<AssignmentsResponse>("Chỉ Owner/Manager mới được giao Content cho Creator.");

        if (IsLocked(content.Status) || content.Status == "Archived")
            return Locked<AssignmentsResponse>("Nội dung đã Published/Archived nên không thể giao việc.");

        var members = await repository.GetMembersAsync(content.ProjectId, token);
        foreach (AssigneeDeadlineRequest item in requested)
        {
            ProjectMember? member = members.FirstOrDefault(m => m.UserId == item.UserId);
            if (member is null)
                return AuthOperationResult<AssignmentsResponse>.Fail("validation", "Người được giao không thuộc Project này.", "assignments");
            if (!TaskRules.CanBeAssignee(member.Role))
                return AuthOperationResult<AssignmentsResponse>.Fail("validation", "Chỉ có thể giao Content cho thành viên có vai trò Creator.", "assignments");

            TaskAssignment? current = existing.FirstOrDefault(e => e.AssigneeUserId == item.UserId);
            if (TaskRules.ValidateNewDeadline(item.Deadline, current?.Deadline, Today) is { } deadlineError)
                return AuthOperationResult<AssignmentsResponse>.Fail("validation", $"{member.DisplayName}: {deadlineError}", "assignments");
        }

        var removed = existing.Where(e => requested.All(a => a.UserId != e.AssigneeUserId)).ToList();
        foreach (TaskAssignment gone in removed.Where(e => e.Status == AssignmentStatus.Completed))
        {
            string name = members.FirstOrDefault(m => m.UserId == gone.AssigneeUserId)?.DisplayName ?? $"User #{gone.AssigneeUserId}";
            return AuthOperationResult<AssignmentsResponse>.Fail("validation", $"{name} đã hoàn thành công việc nên không thể bỏ giao.", "assignments");
        }

        var deadlineChanges = new List<DeadlineChange>();
        var additions = new List<NewAssignment>();
        foreach (AssigneeDeadlineRequest item in requested)
        {
            TaskAssignment? kept = existing.FirstOrDefault(e => e.AssigneeUserId == item.UserId);
            if (kept is null) additions.Add(new NewAssignment(item.UserId, item.Deadline));
            else if (item.Deadline.HasValue && item.Deadline != kept.Deadline) deadlineChanges.Add(new DeadlineChange(kept.AssignmentId, item.Deadline));
        }

        await repository.ApplyAssignmentsAsync(contentId, user.UserId,
            removed.Select(r => r.AssignmentId).ToList(), deadlineChanges, additions, token);

        var rows = await repository.GetAssignmentsAsync(contentId, token);
        return new(new AssignmentsResponse(rows.Select(ToResponse).ToList()));
    }

    // ------------------------------------------------------------------ helpers

    private static bool IsLocked(string contentStage) => contentStage is "Published";

    private static MyTaskResponse ToResponse(TaskAssignment a) => new(
        a.AssignmentId, a.ContentId, a.ProjectId, a.AssigneeUserId, a.ContentCode, a.ContentTitle,
        a.ContentStage, a.Status.ToString(), a.ContentPriority, a.Deadline, a.ProgressPercent,
        a.AssignedByName, a.Platforms, a.TeamTotal, a.TeamDone);

    private static AuthOperationResult<T> Unauthorized<T>() =>
        AuthOperationResult<T>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);

    private static AuthOperationResult<T> Forbidden<T>(string message) =>
        AuthOperationResult<T>.Fail("forbidden", message, status: 403);

    private static AuthOperationResult<T> NotMember<T>() => Forbidden<T>("Bạn không thuộc Project này.");

    private static AuthOperationResult<T> Locked<T>(string message) =>
        AuthOperationResult<T>.Fail("content_locked", message, status: 409);

    private static AuthOperationResult<T> ContentNotFound<T>() =>
        AuthOperationResult<T>.Fail("not_found", "Không tìm thấy Content.", status: 404);
}
