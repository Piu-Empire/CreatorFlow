using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Ideas;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Idea Bank chạy qua CreatorFlow.Api (quyền và validation kiểm tra ở Backend; người thao tác lấy từ JWT).
/// Vai trò trong Project được nhớ từ lần tải danh sách gần nhất để bật/tắt nút mà không gọi API mỗi lần chọn dòng.
/// Lỗi API được đổi sang IdeaValidationException / UnauthorizedWorkflowActionException — đúng các ngoại lệ IdeaBankControl đã xử lý.
/// </summary>
public sealed class ApiIdeaBackend(ApiClient api, UserSession session) : IIdeaBackend
{
    private readonly Dictionary<long, ProjectRole> _roles = new();
    private long _roleGeneration = -1;

    private T Run<T>(Func<Task<T>> call)
    {
        try
        {
            return ApiTaskCall.Run(call, session);
        }
        catch (UnauthorizedWorkflowActionException)
        {
            _roles.Clear();
            throw;
        }
        catch (Exception ex) when (ex is ContentValidationException or InvalidOperationException)
        {
            throw new IdeaValidationException(ex.Message); // mất mạng / quá thời gian chờ
        }
    }

    private string Bearer => ApiTaskCall.BearerOf(session);

    // ---------- Đọc ----------

    public List<Idea> GetIdeas(long projectId, long userId, IdeaFilter? filter = null)
    {
        filter ??= new IdeaFilter();
        if (_roleGeneration != session.Generation) { _roles.Clear(); _roleGeneration = session.Generation; }
        var response = Run(() => api.GetIdeasAsync(projectId, filter.SearchText, filter.Status?.ToString(), filter.Tag, Bearer));
        if (Enum.TryParse<ProjectRole>(response.MyRole, out var role)) _roles[projectId] = role;
        return response.Ideas.Select(ToIdea).ToList();
    }

    public List<string> GetProjectTags(long projectId, long userId) =>
        Run(() => api.GetIdeaTagsAsync(projectId, Bearer)).Tags.ToList();

    public int CountBacklog(long projectId, long userId)
    {
        return GetIdeas(projectId, userId, new IdeaFilter { Status = IdeaStatus.Backlog }).Count;
    }

    // ---------- Quyền (chỉ để bật/tắt nút; Backend kiểm tra lại khi ghi) ----------

    public bool CanCreate(long projectId, long userId) => IdeaRules.CanCreate(RoleOf(projectId));

    public bool CanEdit(Idea idea, long userId) => IdeaRules.CanEdit(RoleOf(idea.ProjectId), idea, userId);

    public bool CanDelete(Idea idea, long userId) => IdeaRules.CanDelete(RoleOf(idea.ProjectId), idea, userId);

    public bool CanConvert(Idea idea, long userId) => IdeaRules.CanConvert(RoleOf(idea.ProjectId), idea, userId);

    private ProjectRole? RoleOf(long projectId)
    {
        if (_roleGeneration != session.Generation)
        {
            _roles.Clear();
            _roleGeneration = session.Generation;
        }
        if (_roles.TryGetValue(projectId, out var cached)) return cached;
        try
        {
            var response = Run(() => api.GetMyProjectRoleAsync(projectId, Bearer));
            if (Enum.TryParse<ProjectRole>(response.Role, out var role)) { _roles[projectId] = role; return role; }
        }
        catch (Exception ex) when (ex is IdeaValidationException or UnauthorizedWorkflowActionException) { }
        return null; // không phải thành viên / không gọi được API: coi như không có quyền, nút bị khoá
    }

    // ---------- Ghi ----------

    public long Create(long projectId, IdeaDraft draft, long userId) =>
        Run(() => api.CreateIdeaAsync(projectId, ToRequest(draft), Bearer)).IdeaId;

    public void Update(long ideaId, IdeaDraft draft, long userId) =>
        Run(() => api.UpdateIdeaAsync(ideaId, ToRequest(draft), Bearer));

    public long ConvertToContent(long ideaId, long userId) =>
        Run(() => api.ConvertIdeaAsync(ideaId, Bearer)).ContentId;

    public void Delete(long ideaId, long userId) =>
        Run(() => api.DeleteIdeaAsync(ideaId, Bearer));

    // ---------- Chuyển đổi ----------

    private static IdeaRequest ToRequest(IdeaDraft d) => new(d.Title, d.Description, d.Note, d.Status.ToString(), d.Tags);

    private static Idea ToIdea(IdeaResponse r) => new()
    {
        IdeaId = r.IdeaId,
        ProjectId = r.ProjectId,
        Title = r.Title,
        Description = r.Description,
        Note = r.Note,
        Status = Enum.TryParse<IdeaStatus>(r.Status, out var status) ? status : IdeaStatus.Draft,
        Tags = r.Tags.ToList(),
        CreatedByUserId = r.CreatedByUserId,
        CreatedByName = r.CreatedByName,
        CreatedAt = r.CreatedAt.ToLocalTime(),
        UpdatedAt = r.UpdatedAt.ToLocalTime(),
        ConvertedContentId = r.ConvertedContentId,
    };
}
