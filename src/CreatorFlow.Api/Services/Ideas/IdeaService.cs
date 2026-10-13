using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Ideas;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Ideas;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Ideas;

namespace CreatorFlow.Api.Services.Ideas;

/// <summary>
/// Nghiệp vụ Idea Bank. Mọi quyền kiểm tra Ở ĐÂY: chỉ thành viên đang hoạt động của Project mới xem/ghi Idea của Project đó;
/// người thao tác luôn lấy từ JWT. Vai trò lấy qua ITaskRepository.GetRoleAsync (giống MetricService).
/// </summary>
public sealed class IdeaService(IIdeaRepository ideas, ITaskRepository tasks, CurrentAuthenticatedUser current)
{
    public async Task<AuthOperationResult<IdeasResponse>> GetIdeasAsync(
        long projectId, string? search, string? status, string? tag, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<IdeasResponse>();
        if (await tasks.GetRoleAsync(projectId, user.UserId, token) is not { } role) return NotMember<IdeasResponse>();

        IdeaStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse(status, ignoreCase: true, out IdeaStatus value) || !Enum.IsDefined(value))
                return Invalid<IdeasResponse>("Trạng thái Idea không hợp lệ.", "status");
            parsed = value;
        }

        var rows = await ideas.GetByProjectAsync(projectId, new IdeaFilter(search ?? string.Empty, parsed, tag), token);
        return new(new IdeasResponse(rows.Select(ToResponse).ToList(), role.ToString()));
    }

    public async Task<AuthOperationResult<IdeaTagsResponse>> GetTagsAsync(long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<IdeaTagsResponse>();
        if (await tasks.GetRoleAsync(projectId, user.UserId, token) is null) return NotMember<IdeaTagsResponse>();
        return new(new IdeaTagsResponse(await ideas.GetProjectTagsAsync(projectId, token)));
    }

    public async Task<AuthOperationResult<IdeaResponse>> CreateAsync(long projectId, IdeaRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<IdeaResponse>();
        if (await tasks.GetRoleAsync(projectId, user.UserId, token) is null) return NotMember<IdeaResponse>();

        var (draft, error) = await BuildDraftAsync(projectId, request, null, token);
        if (draft is null) return Invalid<IdeaResponse>(error!);

        long id = await ideas.CreateAsync(projectId, draft, user.UserId, token);
        return await ReloadAsync(id, token);
    }

    public async Task<AuthOperationResult<IdeaResponse>> UpdateAsync(long ideaId, IdeaRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<IdeaResponse>();
        if (await ideas.GetByIdAsync(ideaId, token) is not { } idea) return NotFound<IdeaResponse>();
        ProjectRole? role = await tasks.GetRoleAsync(idea.ProjectId, user.UserId, token);
        if (role is null) return NotMember<IdeaResponse>();
        if (!IdeaRules.CanEdit(role, idea, user.UserId)) return Forbidden<IdeaResponse>("Bạn chỉ được sửa Idea do chính mình tạo.");

        var (draft, error) = await BuildDraftAsync(idea.ProjectId, request, idea.Status, token);
        if (draft is null) return Invalid<IdeaResponse>(error!);

        await ideas.UpdateAsync(ideaId, draft, token);
        return await ReloadAsync(ideaId, token);
    }

    public async Task<AuthOperationResult<ConvertIdeaResponse>> ConvertAsync(long ideaId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ConvertIdeaResponse>();
        if (await ideas.GetByIdAsync(ideaId, token) is not { } idea) return NotFound<ConvertIdeaResponse>();
        ProjectRole? role = await tasks.GetRoleAsync(idea.ProjectId, user.UserId, token);
        if (role is null) return NotMember<ConvertIdeaResponse>();
        if (!IdeaRules.CanEdit(role, idea, user.UserId))
            return Forbidden<ConvertIdeaResponse>("Bạn chỉ được chuyển Idea do chính mình tạo thành Content.");
        if (IdeaRules.ValidateConvertible(idea.Status) is { } notConvertible) return Invalid<ConvertIdeaResponse>(notConvertible);

        if (idea.Title.Length > IdeaRules.MaxContentTitleLength)
            return Invalid<ConvertIdeaResponse>($"Tiêu đề Idea dài {idea.Title.Length} ký tự, vượt giới hạn {IdeaRules.MaxContentTitleLength} ký tự của Content. Hãy rút gọn tiêu đề rồi chuyển lại.");
        if (idea.Description.Length > IdeaRules.MaxContentDescriptionLength)
            return Invalid<ConvertIdeaResponse>($"Mô tả Idea dài {idea.Description.Length} ký tự, vượt giới hạn {IdeaRules.MaxContentDescriptionLength} ký tự của Content. Hãy rút gọn mô tả rồi chuyển lại.");

        string code = $"IDEA-{idea.IdeaId:000}";
        long? contentId = await ideas.ConvertToContentAsync(ideaId, IdeaRules.DefaultContentType, user.UserId, $"Tạo từ {code} — {idea.Title}", token);
        return contentId is { } id
            ? new(new ConvertIdeaResponse(ideaId, id))
            : AuthOperationResult<ConvertIdeaResponse>.Fail("validation",
                "Idea này vừa được chuyển hoặc không còn ở trạng thái có thể chuyển. Hãy tải lại danh sách.", status: 409);
    }

    public async Task<AuthOperationResult<DeleteIdeaResponse>> DeleteAsync(long ideaId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<DeleteIdeaResponse>();
        if (await ideas.GetByIdAsync(ideaId, token) is not { } idea) return NotFound<DeleteIdeaResponse>();
        ProjectRole? role = await tasks.GetRoleAsync(idea.ProjectId, user.UserId, token);
        if (role is null) return NotMember<DeleteIdeaResponse>();
        if (!IdeaRules.CanEdit(role, idea, user.UserId)) return Forbidden<DeleteIdeaResponse>("Bạn chỉ được xóa Idea do chính mình tạo.");
        if (idea.Status == IdeaStatus.Converted)
            return Invalid<DeleteIdeaResponse>("Idea đã chuyển thành Content nên không thể xóa. Hãy chuyển sang Lưu trữ nếu không dùng nữa.");

        await ideas.DeleteAsync(ideaId, token);
        return new(new DeleteIdeaResponse(ideaId));
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Chuẩn hóa + kiểm tra request; tag trùng (không phân biệt hoa/thường) với tag có sẵn trong Project thì dùng đúng cách viết đó.</summary>
    private async Task<(IdeaDraft? Draft, string? Error)> BuildDraftAsync(
        long projectId, IdeaRequest request, IdeaStatus? currentStatus, CancellationToken token)
    {
        string title = (request.Title ?? string.Empty).Trim();
        string description = (request.Description ?? string.Empty).Trim();
        string note = (request.Note ?? string.Empty).Trim();
        string[] tags = IdeaRules.NormalizeTags(request.Tags);

        if (!Enum.TryParse(request.Status, ignoreCase: true, out IdeaStatus status) || !Enum.IsDefined(status))
            return (null, "Trạng thái Idea không hợp lệ.");
        if (IdeaRules.Validate(title, description, note, tags) is { } error) return (null, error);
        if (IdeaRules.ValidateStatusChange(currentStatus, status) is { } statusError) return (null, statusError);

        var existing = await ideas.GetProjectTagsAsync(projectId, token);
        string[] reused = tags
            .Select(t => existing.FirstOrDefault(e => e.Equals(t, StringComparison.OrdinalIgnoreCase)) ?? t)
            .ToArray();
        return (new IdeaDraft(title, description, note, status, reused), null);
    }

    private async Task<AuthOperationResult<IdeaResponse>> ReloadAsync(long ideaId, CancellationToken token) =>
        await ideas.GetByIdAsync(ideaId, token) is { } idea ? new(ToResponse(idea)) : NotFound<IdeaResponse>();

    private static IdeaResponse ToResponse(Idea i) => new(i.IdeaId, i.ProjectId, i.Title, i.Description, i.Note,
        i.Status.ToString(), i.Tags, i.CreatedByUserId, i.CreatedByName, i.CreatedAt, i.UpdatedAt, i.ConvertedContentId);

    private static AuthOperationResult<T> Unauthorized<T>() =>
        AuthOperationResult<T>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);

    private static AuthOperationResult<T> Forbidden<T>(string message) =>
        AuthOperationResult<T>.Fail("forbidden", message, status: 403);

    private static AuthOperationResult<T> NotMember<T>() => Forbidden<T>("Bạn không thuộc Project này.");

    private static AuthOperationResult<T> NotFound<T>() =>
        AuthOperationResult<T>.Fail("not_found", "Không tìm thấy Idea.", status: 404);

    private static AuthOperationResult<T> Invalid<T>(string message, string? field = null) =>
        AuthOperationResult<T>.Fail("validation", message, field);
}
