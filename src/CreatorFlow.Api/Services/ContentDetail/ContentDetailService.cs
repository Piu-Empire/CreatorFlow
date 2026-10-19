using System.Security.Cryptography;
using System.Text;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.ContentDetail;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.ContentDetail;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Services.Tasks;
using CreatorFlow.Contracts.ContentDetail;

namespace CreatorFlow.Api.Services.ContentDetail;

/// <summary>
/// Nghiệp vụ Content Detail (xem / sửa thông tin / script / activity). Mọi quyền kiểm tra Ở ĐÂY:
///   - Xem: thành viên Project. Người thao tác luôn lấy từ JWT.
///   - Sửa thông tin: Owner/Manager sửa mọi Content, Creator chỉ sửa Content do mình tạo; Content đã Published bị khóa;
///     chỉ Owner/Manager được đổi deadline; không được bỏ platform đã PUBLISHED.
///   - Script: Owner/Manager sửa mọi Content; Creator chỉ sửa Content mình tạo hoặc được giao; Published/Archived bị khóa;
///     lưu bằng compare-and-set theo Version để không ghi đè bản của người khác.
/// </summary>
public sealed class ContentDetailService(
    IContentDetailRepository repository,
    ITaskRepository taskRepository,
    CurrentAuthenticatedUser current)
{
    public static readonly string[] AvailableContentTypes =
        { "Short video", "Long video", "Reel", "Livestream", "Image post", "Story", "Article" };

    public const string DefaultContentType = "Short video";
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxScriptLength = 100_000;

    private static readonly Dictionary<string, string> Priorities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Low"] = "LOW",
        ["Medium"] = "MEDIUM",
        ["High"] = "HIGH",
        ["Urgent"] = "URGENT",
    };

    // ------------------------------------------------------------------ nền tảng

    public async Task<AuthOperationResult<PlatformOptionsResponse>> GetPlatformsAsync(CancellationToken token = default)
    {
        if (current.User is null) return Unauthorized<PlatformOptionsResponse>();
        return new(new PlatformOptionsResponse(await repository.GetActivePlatformNamesAsync(token)));
    }

    // ------------------------------------------------------------------ chi tiết

    public async Task<AuthOperationResult<ContentDetailResponse>> GetDetailAsync(long contentId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ContentDetailResponse>();

        ContentDetailRecord? content = await repository.GetDetailAsync(contentId, token);
        if (content is null) return NotFound<ContentDetailResponse>();

        ProjectRole? role = await taskRepository.GetRoleAsync(content.ProjectId, user.UserId, token);
        if (role is null) return NotMember<ContentDetailResponse>();

        return new(new ContentDetailResponse(
            content.ContentId, content.ProjectId, $"CNT-{content.ContentId:000}", content.Title, content.Description,
            content.ContentType, content.Status, content.Priority, content.Deadline, content.PlannedPublishAt,
            content.CreatedBy, content.Platforms, role.Value.ToString()));
    }

    public async Task<AuthOperationResult<ContentDetailResponse>> UpdateAsync(
        long contentId, UpdateContentRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ContentDetailResponse>();

        ContentDetailRecord? content = await repository.GetDetailAsync(contentId, token);
        if (content is null) return NotFound<ContentDetailResponse>();

        ProjectRole? role = await taskRepository.GetRoleAsync(content.ProjectId, user.UserId, token);
        if (role is null) return NotMember<ContentDetailResponse>();

        if (content.Status == "Published")
            return Fail<ContentDetailResponse>("content_locked", "Nội dung đã Published nên không thể chỉnh sửa.", 409);

        if (role == ProjectRole.Creator && content.CreatedBy != user.UserId)
            return Fail<ContentDetailResponse>("forbidden", "Creator chỉ được sửa nội dung do chính mình tạo.", 403);

        PlanningCheck check = CheckPlanning(request.Title, request.Description, request.ContentType, request.Priority,
            request.Deadline, request.PlannedPublishAt);
        if (check.Error is { } planningError) return planningError.As<ContentDetailResponse>();

        // Chỉ Owner/Manager được đổi deadline; Creator gửi lại đúng deadline cũ thì vẫn lưu được.
        if (request.Deadline != content.Deadline)
        {
            if (!TaskRules.CanChangeDeadline(role))
                return Fail<ContentDetailResponse>("forbidden", "Chỉ Owner/Manager mới được đổi deadline.", 403);

            string? deadlineError = TaskRules.ValidateNewDeadline(request.Deadline, content.Deadline, DateOnly.FromDateTime(DateTime.UtcNow));
            if (deadlineError is not null) return Validation<ContentDetailResponse>(deadlineError, "deadline");
        }

        (List<string> platforms, string? platformError) = await ResolvePlatformsAsync(request.Platforms, token);
        if (platformError is not null) return Validation<ContentDetailResponse>(platformError, "platforms");

        string[] removed = (await repository.GetPublishedPlatformNamesAsync(contentId, token))
            .Where(p => !platforms.Contains(p, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (removed.Length > 0)
            return Validation<ContentDetailResponse>($"Không thể bỏ nền tảng đã đăng: {string.Join(", ", removed)}.", "platforms");

        await repository.UpdateAsync(contentId,
            new ContentUpdate(check.Title, check.Description, check.ContentType, check.PriorityDb, request.Deadline, request.PlannedPublishAt, platforms), token);

        return await GetDetailAsync(contentId, token);
    }

    // ------------------------------------------------------------------ tạo mới

    private static readonly Dictionary<string, string> InitialStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Idea"] = "IDEA",
        ["Script"] = "SCRIPT",
        ["Production"] = "PRODUCTION",
        ["Editing"] = "EDITING",
        ["Ready"] = "READY",
        ["Published"] = "PUBLISHED",
    };

    /// <summary>
    /// Tạo Content mới: mọi thành viên Project được tạo; không tạo thẳng ở Review (phải qua Submit Review) hay Archived.
    /// Creator chỉ được tự nhận việc; Owner/Manager giao được cho bất kỳ Creator nào của Project.
    /// </summary>
    public async Task<AuthOperationResult<ContentDetailResponse>> CreateAsync(
        long projectId, CreateContentRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ContentDetailResponse>();

        ProjectRole? role = await taskRepository.GetRoleAsync(projectId, user.UserId, token);
        if (role is null) return NotMember<ContentDetailResponse>();

        string statusName = string.IsNullOrWhiteSpace(request.Status) ? "Idea" : request.Status.Trim();
        if (statusName.Equals("Review", StringComparison.OrdinalIgnoreCase))
            return Validation<ContentDetailResponse>("Không thể tạo trực tiếp ở giai đoạn Review. Hãy tạo ở Editing rồi Submit Review.", "status");
        if (!InitialStatuses.TryGetValue(statusName, out string? statusDb))
            return Validation<ContentDetailResponse>("Giai đoạn ban đầu không hợp lệ.", "status");

        PlanningCheck check = CheckPlanning(request.Title, request.Description, request.ContentType, request.Priority,
            request.Deadline, request.PlannedPublishAt);
        if (check.Error is { } planningError) return planningError.As<ContentDetailResponse>();

        (List<string> platforms, string? platformError) = await ResolvePlatformsAsync(request.Platforms, token);
        if (platformError is not null) return Validation<ContentDetailResponse>(platformError, "platforms");

        if (request.AssigneeUserId is long assigneeId)
        {
            if (!TaskRules.CanAssign(role) && assigneeId != user.UserId)
                return Fail<ContentDetailResponse>("forbidden", "Chỉ Owner/Manager mới được giao Content cho người khác.", 403);

            ProjectRole? assigneeRole = await taskRepository.GetRoleAsync(projectId, assigneeId, token);
            if (assigneeRole is null)
                return Validation<ContentDetailResponse>("Người được giao không thuộc Project này.", "assigneeUserId");
            if (!TaskRules.CanBeAssignee(assigneeRole))
                return Validation<ContentDetailResponse>("Chỉ có thể giao Content cho thành viên có vai trò Creator.", "assigneeUserId");
        }

        long contentId = await repository.CreateAsync(new NewContent(projectId, statusDb,
            new ContentUpdate(check.Title, check.Description, check.ContentType, check.PriorityDb, request.Deadline, request.PlannedPublishAt, platforms),
            user.UserId), token);

        if (request.AssigneeUserId is long assignee)
            await taskRepository.ApplyAssignmentsAsync(contentId, user.UserId, Array.Empty<long>(),
                Array.Empty<DeadlineChange>(), new[] { new NewAssignment(assignee, request.Deadline) }, token);

        return await GetDetailAsync(contentId, token);
    }

    // ------------------------------------------------------------------ script

    public async Task<AuthOperationResult<ScriptResponse>> GetScriptAsync(long contentId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ScriptResponse>();

        ScriptRecord? script = await repository.GetScriptAsync(contentId, token);
        if (script is null) return NotFound<ScriptResponse>();

        ProjectRole? role = await taskRepository.GetRoleAsync(script.ProjectId, user.UserId, token);
        if (role is null) return NotMember<ScriptResponse>();

        return new(await ToResponseAsync(script, role.Value, user.UserId, token));
    }

    public async Task<AuthOperationResult<ScriptResponse>> SaveScriptAsync(
        long contentId, SaveScriptRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ScriptResponse>();

        string normalized = NormalizeScript(request.Text);
        if (normalized.Length > MaxScriptLength)
            return Validation<ScriptResponse>($"Kịch bản tối đa {MaxScriptLength:N0} ký tự (hiện có {normalized.Length:N0}).", "text");

        ScriptRecord? script = await repository.GetScriptAsync(contentId, token);
        if (script is null) return NotFound<ScriptResponse>();

        ProjectRole? role = await taskRepository.GetRoleAsync(script.ProjectId, user.UserId, token);
        if (role is null) return NotMember<ScriptResponse>();

        ScriptAccessResult access = await EvaluateAccessAsync(script, role.Value, user.UserId, token);
        if (!access.CanEdit)
        {
            string reason = access.ReadOnlyReason ?? "Không thể sửa kịch bản này.";
            return access.IsContentLocked
                ? Fail<ScriptResponse>("content_locked", reason, 409)
                : Fail<ScriptResponse>("forbidden", reason, 403);
        }

        if (!string.Equals(ComputeVersion(script.Script), request.ExpectedVersion, StringComparison.Ordinal))
            return ScriptConflict();

        if (string.Equals(NormalizeScript(script.Script), normalized, StringComparison.Ordinal))
            return new(await ToResponseAsync(script, role.Value, user.UserId, token)); // không đổi gì → không ghi DB

        string? toStore = normalized.Length == 0 ? null : normalized;
        if (!await repository.TryUpdateScriptAsync(contentId, script.Script, toStore, token))
            return ScriptConflict();

        ScriptRecord saved = await repository.GetScriptAsync(contentId, token) ?? script;
        return new(await ToResponseAsync(saved, role.Value, user.UserId, token));
    }

    // ------------------------------------------------------------------ activity

    public async Task<AuthOperationResult<ActivityResponse>> GetActivityAsync(long contentId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ActivityResponse>();

        ContentDetailRecord? content = await repository.GetDetailAsync(contentId, token);
        if (content is null) return NotFound<ActivityResponse>();

        if (await taskRepository.GetRoleAsync(content.ProjectId, user.UserId, token) is null)
            return NotMember<ActivityResponse>();

        IReadOnlyList<ActivityRecord> items = await repository.GetActivityAsync(contentId, token);
        return new(new ActivityResponse(contentId,
            items.Select(i => new ActivityItemResponse(i.Timestamp, i.ActorName, i.Description, i.Feedback,
                i.Kind, i.FromStatus, i.ToStatus, i.ReviewNo, i.ReviewStatus)).ToList()));
    }

    // ------------------------------------------------------------------ helpers

    private readonly record struct ScriptAccessResult(bool CanEdit, string? ReadOnlyReason, bool IsContentLocked);

    private async Task<ScriptAccessResult> EvaluateAccessAsync(ScriptRecord script, ProjectRole role, long userId, CancellationToken token)
    {
        if (script.Status == "Published")
            return new(false, "Nội dung đã Published nên không thể sửa kịch bản.", true);
        if (script.Status == "Archived")
            return new(false, "Nội dung đã Archived nên không thể sửa kịch bản. Hãy khôi phục nội dung trước.", true);
        if (role is ProjectRole.Owner or ProjectRole.Manager)
            return new(true, null, false);

        bool isCreator = script.CreatedBy == userId;
        bool isAssignee = !isCreator && await taskRepository.GetAssignmentAsync(script.ContentId, userId, token) is not null;
        return role == ProjectRole.Creator && (isCreator || isAssignee)
            ? new(true, null, false)
            : new(false, "Bạn chỉ được sửa kịch bản của nội dung do mình tạo hoặc được giao.", false);
    }

    private async Task<ScriptResponse> ToResponseAsync(ScriptRecord script, ProjectRole role, long userId, CancellationToken token)
    {
        ScriptAccessResult access = await EvaluateAccessAsync(script, role, userId, token);
        return new ScriptResponse(script.ContentId, script.ProjectId, NormalizeScript(script.Script),
            ComputeVersion(script.Script), access.CanEdit, access.IsContentLocked, access.ReadOnlyReason);
    }

    /// <summary>Xuống dòng về "\n", bỏ NUL/BOM, cắt khoảng trắng hai đầu (cùng quy tắc ScriptTextAnalyzer.Normalize phía WinForms).</summary>
    private static string NormalizeScript(string? text) =>
        string.IsNullOrEmpty(text)
            ? string.Empty
            : text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\0", string.Empty).Replace("\uFEFF", string.Empty).Trim();

    /// <summary>Dấu vân tay của bản script đang lưu (null và rỗng cùng nghĩa "chưa có script").</summary>
    private static string ComputeVersion(string? rawScript) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawScript ?? string.Empty)));

    private sealed record PlanningFailure(string Code, string Message, string? Field)
    {
        public AuthOperationResult<T> As<T>() => AuthOperationResult<T>.Fail(Code, Message, Field);
    }

    private sealed record PlanningCheck(string Title, string Description, string ContentType, string PriorityDb, PlanningFailure? Error);

    /// <summary>Chuẩn hóa và kiểm tra các trường lập kế hoạch dùng chung cho tạo mới và sửa.</summary>
    private static PlanningCheck CheckPlanning(string? rawTitle, string? rawDescription, string? rawContentType, string? rawPriority,
        DateOnly? deadline, DateOnly? plannedPublishAt)
    {
        string title = (rawTitle ?? string.Empty).Trim();
        string description = (rawDescription ?? string.Empty).Trim();
        string contentType = ResolveContentType(rawContentType);
        string? priorityDb = null;
        bool priorityOk = rawPriority is not null && Priorities.TryGetValue(rawPriority.Trim(), out priorityDb);

        PlanningFailure? error =
            title.Length == 0 ? new("validation", "Vui lòng nhập tiêu đề.", "title")
            : title.Length > MaxTitleLength ? new("validation", $"Tiêu đề tối đa {MaxTitleLength} ký tự.", "title")
            : description.Length > MaxDescriptionLength ? new("validation", $"Mô tả tối đa {MaxDescriptionLength} ký tự.", "description")
            : !AvailableContentTypes.Contains(contentType, StringComparer.Ordinal) ? new("validation", $"Loại nội dung '{contentType}' không hợp lệ.", "contentType")
            : !priorityOk ? new("validation", "Mức độ ưu tiên không hợp lệ.", "priority")
            : deadline.HasValue && plannedPublishAt.HasValue && plannedPublishAt.Value < deadline.Value
                ? new("validation", "Ngày dự kiến đăng không được sớm hơn hạn hoàn thành.", "plannedPublishAt")
            : null;

        return new PlanningCheck(title, description, contentType, priorityDb ?? string.Empty, error);
    }

    /// <summary>Mỗi platform phải tồn tại và đang bật; đổi về đúng cách viết trong DB (VD "youtube" → "YouTube"), bỏ trùng.</summary>
    private async Task<(List<string> Platforms, string? Error)> ResolvePlatformsAsync(IReadOnlyList<string>? requested, CancellationToken token)
    {
        IReadOnlyList<string> active = await repository.GetActivePlatformNamesAsync(token);
        var platforms = new List<string>();
        foreach (string name in (requested ?? Array.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()))
        {
            string? match = active.FirstOrDefault(p => p.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match is null) return (platforms, $"Nền tảng '{name}' không tồn tại hoặc đã ngừng hoạt động.");
            if (!platforms.Contains(match, StringComparer.OrdinalIgnoreCase)) platforms.Add(match);
        }
        return (platforms, null);
    }

    private static string ResolveContentType(string? contentType)
    {
        string trimmed = (contentType ?? string.Empty).Trim();
        if (trimmed.Length == 0) return DefaultContentType;
        // So khớp bỏ qua hoa/thường, '_', '-' và khoảng trắng để nhận cả giá trị cũ trong DB (VD seed lưu "SHORT_VIDEO") và trả về tên chuẩn "Short video".
        string key = ContentTypeKey(trimmed);
        return AvailableContentTypes.FirstOrDefault(t => ContentTypeKey(t) == key) ?? trimmed;
    }

    private static string ContentTypeKey(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static AuthOperationResult<ScriptResponse> ScriptConflict() =>
        Fail<ScriptResponse>("script_conflict",
            "Kịch bản đã được người khác cập nhật sau khi bạn mở. Hãy tải lại bản mới nhất rồi chỉnh sửa tiếp.", 409);

    private static AuthOperationResult<T> Fail<T>(string code, string message, int status) =>
        AuthOperationResult<T>.Fail(code, message, status: status);

    private static AuthOperationResult<T> Validation<T>(string message, string field) =>
        AuthOperationResult<T>.Fail("validation", message, field);

    private static AuthOperationResult<T> Unauthorized<T>() => Fail<T>("unauthorized", "Vui lòng đăng nhập lại.", 401);

    private static AuthOperationResult<T> NotMember<T>() => Fail<T>("forbidden", "Bạn không thuộc Project này.", 403);

    private static AuthOperationResult<T> NotFound<T>() => Fail<T>("not_found", "Không tìm thấy Content.", 404);
}
