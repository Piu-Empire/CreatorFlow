using System.Net;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Projects;

namespace CreatorFlow.Services;

public sealed class ProjectApiFacade : IDisposable
{
    private readonly ApiClient api;
    private readonly UserSession session;
    private CancellationTokenSource lifetime = new();
    public ProjectContext Context { get; } = new();
    public ProjectApiFacade(ApiClient api, UserSession session)
    {
        this.api = api; this.session = session;
        session.Cleared += Cleared;
    }
    private void Cleared(object? sender, EventArgs e)
    {
        lifetime.Cancel(); lifetime.Dispose(); lifetime = new();
        Context.Clear();
    }
    public Task<ProjectListResponse> ListAsync(CancellationToken token) => Send<ProjectListResponse>(HttpMethod.Get, "api/projects", null, token);
    public async Task<ProjectResponse> GetAsync(long id, CancellationToken token)
    {
        var response = await Send<ProjectResponse>(HttpMethod.Get, $"api/projects/{id}", null, token);
        if (response.ProjectId != id) throw InvalidResponse();
        return response;
    }
    public async Task<ProjectAccessResponse> AccessAsync(long id, CancellationToken token)
    {
        var access = await Send<ProjectAccessResponse>(HttpMethod.Get, $"api/projects/{id}/access", null, token);
        if (access.ProjectId != id || access.Status != "ACTIVE") throw InvalidResponse();
        return access;
    }
    public Task<ProjectResponse> CreateAsync(string name, string description, CancellationToken token) => Send<ProjectResponse>(HttpMethod.Post, "api/projects", new CreateProjectRequest(name, description), token);
    public Task<ProjectResponse> UpdateAsync(long id, string name, string description, CancellationToken token) => Send<ProjectResponse>(HttpMethod.Put, $"api/projects/{id}", new UpdateProjectRequest(name, description), token);
    public Task<ProjectResponse> ArchiveAsync(long id, CancellationToken token) => Send<ProjectResponse>(HttpMethod.Post, $"api/projects/{id}/archive", null, token);
    public Task<ProjectResponse> RestoreAsync(long id, CancellationToken token) => Send<ProjectResponse>(HttpMethod.Post, $"api/projects/{id}/restore", null, token);
    public Task<ProjectMutationResponse> DeleteAsync(long id, CancellationToken token) => Send<ProjectMutationResponse>(HttpMethod.Delete, $"api/projects/{id}", null, token);
    public Task<ProjectMembersResponse> GetMembersAsync(long id, CancellationToken token) => Send<ProjectMembersResponse>(HttpMethod.Get, $"api/projects/{id}/members", null, token);
    public Task<ProjectMemberMutationResponse> UpdateMemberRoleAsync(long projectId, long userId, string role, CancellationToken token) =>
        Send<ProjectMemberMutationResponse>(HttpMethod.Put, $"api/projects/{projectId}/members/{userId}/role", new UpdateProjectMemberRoleRequest(role), token);
    public Task<ProjectMemberMutationResponse> RemoveMemberAsync(long projectId, long userId, CancellationToken token) =>
        Send<ProjectMemberMutationResponse>(HttpMethod.Delete, $"api/projects/{projectId}/members/{userId}", null, token);

    public async Task<bool> RevalidateAsync(CancellationToken token)
    {
        if (Context.Current is not { } current) return true;
        long generation = Context.Generation;
        try
        {
            var access = await AccessAsync(current.ProjectId, token);
            if (generation != Context.Generation) return true;
            if (access.Name != current.Name || !access.CurrentUserRole.Equals(current.Role.ToString(), StringComparison.OrdinalIgnoreCase)) Context.Apply(access);
            return true;
        }
        catch (ApiBusinessException error) when (error.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            if (generation == Context.Generation) Context.Clear();
            return false;
        }
    }

    private async Task<T> Send<T>(HttpMethod method, string path, object? body, CancellationToken token)
    {
        if (!session.IsAuthenticated) { session.Clear(); throw new ApiBusinessException(HttpStatusCode.Unauthorized, "unauthorized", "Vui lòng đăng nhập lại."); }
        long generation = session.Generation;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
        try
        {
            T response = await api.SendProjectJsonAsync<T>(method, path, body, session.AccessToken!, linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            if (generation != session.Generation) throw new OperationCanceledException(linked.Token);
            ValidateResponse(response);
            return response;
        }
        catch (ApiBusinessException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (generation == session.Generation) session.Clear();
            throw;
        }
    }
    private static ApiException InvalidResponse() => new(ApiErrorKind.InvalidResponse, "API trả dữ liệu Project không hợp lệ.");
    private static bool Valid(long id, string name, string role, string status) => id > 0 && !string.IsNullOrWhiteSpace(name) &&
        (role is "OWNER" or "MANAGER" or "CREATOR") && (status is "ACTIVE" or "ARCHIVED");
    private static void ValidateResponse<T>(T response)
    {
        bool valid = response switch
        {
            ProjectListResponse list => list.Items is not null && list.Items.All(p => p is not null && Valid(p.ProjectId, p.Name, p.CurrentUserRole, p.Status)),
            ProjectAccessResponse access => Valid(access.ProjectId, access.Name, access.CurrentUserRole, access.Status),
            ProjectResponse project => Valid(project.ProjectId, project.Name, project.CurrentUserRole, project.Status),
            ProjectMutationResponse mutation => mutation.ProjectId > 0 && mutation.Deleted,
            ProjectMembersResponse members => members.Members is not null && members.Members.All(member => member is not null &&
                member.UserId > 0 && !string.IsNullOrWhiteSpace(member.DisplayName) && !string.IsNullOrWhiteSpace(member.Email) &&
                IsRole(member.Role) && member.JoinedAt != default),
            ProjectMemberMutationResponse mutation => mutation.ProjectId > 0 && mutation.UserId > 0 && IsRole(mutation.Role),
            _ => false
        };
        if (!valid) throw InvalidResponse();
    }
    private static bool IsRole(string? role) => role is "OWNER" or "MANAGER" or "CREATOR";
    public void Dispose() { session.Cleared -= Cleared; lifetime.Cancel(); lifetime.Dispose(); }
}
