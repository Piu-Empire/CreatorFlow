using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Projects;
using CreatorFlow.Api.Repositories.Projects;
using CreatorFlow.Contracts.Projects;

namespace CreatorFlow.Api.Services.Projects;

/// <summary>Owner-only member administration. Roles are read from and written to project_members.</summary>
public sealed class ProjectMembersService(IProjectRepository repository, CurrentAuthenticatedUser current)
{
    private sealed class Rejected(string code, string message, int status, string? field = null) : Exception(message)
    {
        public string Code { get; } = code;
        public int Status { get; } = status;
        public string? Field { get; } = field;
    }

    private long UserId() => current.User is { UserId: > 0 } user ? user.UserId :
        throw new Rejected("unauthorized", "Vui lòng đăng nhập lại.", 401);

    private static ProjectRecord Accessible(ProjectRecord? project) => project ??
        throw new Rejected("project_not_found", "Project không tồn tại hoặc bạn không có quyền truy cập.", 404);

    private static void Owner(string role)
    {
        if (role != "OWNER") throw new Rejected("forbidden", "Chỉ Owner được quản lý thành viên của Project.", 403);
    }

    private static string ParseRole(string? role)
    {
        string value = role?.Trim().ToUpperInvariant() ?? string.Empty;
        if (value is not ("OWNER" or "MANAGER" or "CREATOR"))
            throw new Rejected("validation", "Vai trò phải là Owner, Manager hoặc Creator.", 400, "Role");
        return value;
    }

    private static async Task PreserveOwnerAsync(IProjectTransaction tx, ProjectRecord project,
        ProjectMemberRecord member, CancellationToken token)
    {
        if (member.Role != "OWNER") return;

        int owners = await tx.CountActiveOwnersAsync(project.ProjectId, token);
        if (owners <= 1)
            throw new Rejected("last_owner_required", "Project phải luôn có ít nhất một Owner.", 409);

        if (project.OwnerId == member.UserId)
        {
            long? nextOwner = await tx.FindActiveOwnerAsync(project.ProjectId, member.UserId, token);
            if (nextOwner is null)
                throw new Rejected("owner_inconsistent", "Không tìm thấy Owner thay thế cho Project.", 409);
            await tx.SetProjectOwnerAsync(project.ProjectId, nextOwner.Value, token);
        }
    }

    private static async Task<ProjectResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return new(await action()); }
        catch (Rejected error) { return new(default, error.Code, error.Message, error.Status, error.Field); }
    }

    public Task<ProjectResult<ProjectMembersResponse>> ListAsync(long projectId, CancellationToken token) => Execute(async () =>
    {
        if (projectId <= 0) throw new Rejected("validation", "Project không hợp lệ.", 400);
        ProjectMembersRecord members = await repository.ListMembersAsync(projectId, UserId(), token) ??
            throw new Rejected("project_not_found", "Project không tồn tại hoặc bạn không có quyền truy cập.", 404);
        Owner(members.ActorRole);
        return new ProjectMembersResponse(members.Members.Select(member => new ProjectMemberResponse(
            member.UserId, member.DisplayName, member.Email, member.Role, member.JoinedAt)).ToArray());
    });

    public Task<ProjectResult<ProjectMemberMutationResponse>> UpdateRoleAsync(long projectId, long memberUserId,
        UpdateProjectMemberRoleRequest? request, CancellationToken token) => Execute(async () =>
    {
        if (projectId <= 0 || memberUserId <= 0) throw new Rejected("validation", "Project hoặc thành viên không hợp lệ.", 400);
        long actorUserId = UserId();
        return await repository.InTransactionAsync(async (tx, ct) =>
        {
            ProjectRecord project = Accessible(await tx.LockAsync(projectId, actorUserId, ct));
            Owner(project.Role);
            ProjectMemberRecord member = await tx.LockMemberAsync(projectId, memberUserId, ct) ??
                throw new Rejected("member_not_found", "Thành viên không còn hoạt động trong Project.", 404);
            string role = ParseRole(request?.Role);
            if (member.Role == role) return new ProjectMemberMutationResponse(projectId, memberUserId, role, false);
            if (member.Role == "OWNER" && role != "OWNER") await PreserveOwnerAsync(tx, project, member, ct);
            await tx.UpdateMemberRoleAsync(projectId, memberUserId, role, ct);
            return new ProjectMemberMutationResponse(projectId, memberUserId, role, false);
        }, token);
    });

    public Task<ProjectResult<ProjectMemberMutationResponse>> RemoveAsync(long projectId, long memberUserId,
        CancellationToken token) => Execute(async () =>
    {
        if (projectId <= 0 || memberUserId <= 0) throw new Rejected("validation", "Project hoặc thành viên không hợp lệ.", 400);
        long actorUserId = UserId();
        return await repository.InTransactionAsync(async (tx, ct) =>
        {
            ProjectRecord project = Accessible(await tx.LockAsync(projectId, actorUserId, ct));
            Owner(project.Role);
            ProjectMemberRecord member = await tx.LockMemberAsync(projectId, memberUserId, ct) ??
                throw new Rejected("member_not_found", "Thành viên không còn hoạt động trong Project.", 404);
            await PreserveOwnerAsync(tx, project, member, ct);
            await tx.RemoveMemberAsync(projectId, memberUserId, ct);
            return new ProjectMemberMutationResponse(projectId, memberUserId, member.Role, true);
        }, token);
    });
}
