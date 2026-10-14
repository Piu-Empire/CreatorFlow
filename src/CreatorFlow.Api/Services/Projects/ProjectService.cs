using System.Text;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Projects;
using CreatorFlow.Api.Repositories.Projects;
using CreatorFlow.Contracts.Projects;

namespace CreatorFlow.Api.Services.Projects;

public sealed record ProjectResult<T>(T? Value, string? Code = null, string? Message = null, int Status = 200, string? Field = null);

public sealed class ProjectService(IProjectRepository repository, CurrentAuthenticatedUser current)
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
    private static void Owner(ProjectRecord project, long userId)
    {
        if (project.Role != "OWNER") throw new Rejected("forbidden", "Chỉ Owner được quản trị Project.", 403);
        if (project.OwnerId != userId) throw new Rejected("owner_inconsistent", "Thông tin Owner không nhất quán. Vui lòng liên hệ quản trị.", 409);
    }
    private static string Name(string? name)
    {
        string value = name?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.EnumerateRunes().Count() > 200 || value.Contains('\0'))
            throw new Rejected("validation", "Tên Project phải có từ 1 đến 200 ký tự.", 400, "Name");
        return value;
    }
    private static void Description(string? description)
    {
        if (description?.Contains('\0') == true)
            throw new Rejected("validation", "Mô tả chứa ký tự không hợp lệ.", 400, "Description");
    }
    private static ProjectResponse Response(ProjectRecord p) => new(p.ProjectId, p.Name, p.Description, p.OwnerName, p.Role, p.Status, p.CreatedAt);
    private static async Task<ProjectResult<T>> Execute<T>(Func<Task<T>> action, int status = 200)
    {
        try { return new(await action(), Status: status); }
        catch (Rejected error) { return new(default, error.Code, error.Message, error.Status, error.Field); }
    }

    public Task<ProjectResult<ProjectListResponse>> ListAsync(CancellationToken token) => Execute(async () =>
        new ProjectListResponse((await repository.ListAsync(UserId(), token)).Select(p =>
            new ProjectListItem(p.ProjectId, p.Name, p.Role, p.Status)).ToArray()));
    public Task<ProjectResult<ProjectResponse>> GetAsync(long id, CancellationToken token) => Execute(async () =>
        Response(Accessible(await repository.FindAsync(id, UserId(), token))));
    public Task<ProjectResult<ProjectAccessResponse>> AccessAsync(long id, CancellationToken token) => Execute(async () =>
    {
        var p = Accessible(await repository.FindAsync(id, UserId(), token));
        if (p.Status != "ACTIVE") throw new Rejected("project_archived", "Project đã lưu trữ và không thể mở workspace.", 409);
        return new ProjectAccessResponse(p.ProjectId, p.Name, p.Role, p.Status);
    });
    public Task<ProjectResult<ProjectResponse>> CreateAsync(CreateProjectRequest input, CancellationToken token) => Execute(async () =>
    {
        long user = UserId(); string name = Name(input.Name); Description(input.Description);
        return Response(await repository.InTransactionAsync((tx, ct) => tx.CreateAsync(user, name, input.Description, ct), token));
    }, 201);
    public Task<ProjectResult<ProjectResponse>> UpdateAsync(long id, UpdateProjectRequest input, CancellationToken token) => Execute(async () =>
    {
        long user = UserId();
        return await repository.InTransactionAsync(async (tx, ct) =>
        {
            Owner(Accessible(await tx.LockAsync(id, user, ct)), user);
            string name = Name(input.Name); Description(input.Description);
            return Response(await tx.UpdateAsync(id, user, name, input.Description, ct));
        }, token);
    });
    public Task<ProjectResult<ProjectResponse>> ArchiveAsync(long id, CancellationToken token) => Execute(async () =>
    {
        long user = UserId();
        return await repository.InTransactionAsync(async (tx, ct) =>
        {
            Owner(Accessible(await tx.LockAsync(id, user, ct)), user);
            return Response(await tx.ArchiveAsync(id, user, ct));
        }, token);
    });
    public Task<ProjectResult<ProjectMutationResponse>> DeleteAsync(long id, CancellationToken token) => Execute(async () =>
    {
        long user = UserId();
        return await repository.InTransactionAsync(async (tx, ct) =>
        {
            Owner(Accessible(await tx.LockAsync(id, user, ct)), user);
            await tx.DeleteAsync(id, ct);
            return new ProjectMutationResponse(id, true);
        }, token);
    });
}
