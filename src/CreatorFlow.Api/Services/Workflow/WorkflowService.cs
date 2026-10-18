using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Models.Workflow;
using CreatorFlow.Api.Repositories.Workflow;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Workflow;

namespace CreatorFlow.Api.Services.Workflow;

/// <summary>
/// Nghiệp vụ Workflow trạng thái Content. Mọi quyền được kiểm tra Ở ĐÂY (không tin giao diện):
///   - bước đi thẳng (IDEA→SCRIPT→PRODUCTION→EDITING, READY→PUBLISHED): thành viên Project;
///   - gửi duyệt (EDITING→REVIEW): thành viên Project, tạo Review mới không ghi đè;
///   - duyệt/từ chối (REVIEW→READY / REVIEW→EDITING): chỉ Owner/Manager, từ chối bắt buộc Feedback.
/// Người thao tác luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public sealed class WorkflowService(IWorkflowRepository repository, CurrentAuthenticatedUser current)
{
    // Luồng chuyển trạng thái "đi thẳng" (không liên quan Reviews).
    private static readonly Dictionary<WorkflowContentStatus, WorkflowContentStatus> SimpleTransitions = new()
    {
        [WorkflowContentStatus.Idea] = WorkflowContentStatus.Script,
        [WorkflowContentStatus.Script] = WorkflowContentStatus.Production,
        [WorkflowContentStatus.Production] = WorkflowContentStatus.Editing,
        [WorkflowContentStatus.Ready] = WorkflowContentStatus.Published,
    };

    // ------------------------------------------------------------------ chuyển bước đi thẳng

    public async Task<AuthOperationResult<ContentStatusResponse>> ChangeStatusAsync(
        long contentId, ChangeContentStatusRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ContentStatusResponse>();

        if (!Enum.TryParse<WorkflowContentStatus>(request.Status, ignoreCase: true, out var target)
            || target is WorkflowContentStatus.Archived)
            return Validation<ContentStatusResponse>("Trạng thái không hợp lệ.", "status");

        WorkflowContent? content = await repository.GetContentAsync(contentId, token);
        if (content is null) return ContentNotFound<ContentStatusResponse>();

        if (await repository.GetRoleAsync(content.ProjectId, user.UserId, token) is null)
            return NotMember<ContentStatusResponse>();

        if (IsLocked(content.Status))
            return Locked<ContentStatusResponse>("Nội dung đã Published nên không thể đổi trạng thái.");

        if (!SimpleTransitions.TryGetValue(content.Status, out var expected) || expected != target)
            return InvalidTransition<ContentStatusResponse>(content.Status, target);

        WorkflowContent? updated = await repository.ChangeStatusAsync(
            contentId, content.Status, target, user.UserId, request.Note, token);
        return updated is null
            ? Conflict<ContentStatusResponse>("Trạng thái vừa bị thay đổi, hãy tải lại.")
            : new(ToResponse(updated));
    }

    // ------------------------------------------------------------------ gửi duyệt

    public async Task<AuthOperationResult<ContentStatusResponse>> SubmitReviewAsync(
        long contentId, SubmitReviewRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ContentStatusResponse>();

        WorkflowContent? content = await repository.GetContentAsync(contentId, token);
        if (content is null) return ContentNotFound<ContentStatusResponse>();

        if (await repository.GetRoleAsync(content.ProjectId, user.UserId, token) is null)
            return NotMember<ContentStatusResponse>();

        if (IsLocked(content.Status))
            return Locked<ContentStatusResponse>("Nội dung đã Published nên không thể gửi duyệt.");

        if (content.Status != WorkflowContentStatus.Editing)
            return InvalidTransition<ContentStatusResponse>(content.Status, WorkflowContentStatus.Review);

        WorkflowContent? updated = await repository.SubmitReviewAsync(
            contentId, user.UserId, request.Note, token);
        return updated is null
            ? Conflict<ContentStatusResponse>("Trạng thái vừa bị thay đổi, hãy tải lại.")
            : new(ToResponse(updated));
    }

    // ------------------------------------------------------------------ duyệt / từ chối

    public async Task<AuthOperationResult<ContentStatusResponse>> DecideReviewAsync(
        long contentId, ReviewDecisionRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ContentStatusResponse>();

        WorkflowContent? content = await repository.GetContentAsync(contentId, token);
        if (content is null) return ContentNotFound<ContentStatusResponse>();

        ProjectRole? role = await repository.GetRoleAsync(content.ProjectId, user.UserId, token);
        if (role is null) return NotMember<ContentStatusResponse>();
        if (role != ProjectRole.Owner && role != ProjectRole.Manager)
            return Forbidden<ContentStatusResponse>("Chỉ Owner hoặc Manager mới được duyệt/từ chối Review.");

        if (IsLocked(content.Status))
            return Locked<ContentStatusResponse>("Nội dung đã Published nên không thể duyệt.");

        if (content.Status != WorkflowContentStatus.Review)
            return InvalidTransition<ContentStatusResponse>(
                content.Status, request.Approve ? WorkflowContentStatus.Ready : WorkflowContentStatus.Editing);

        if (!request.Approve && string.IsNullOrWhiteSpace(request.Feedback))
            return Validation<ContentStatusResponse>("Từ chối Review bắt buộc phải có Feedback.", "feedback");

        WorkflowContent? updated = await repository.DecideReviewAsync(
            contentId, user.UserId, request.Approve, request.Feedback, token);
        return updated is null
            ? Conflict<ContentStatusResponse>("Không có Review đang chờ duyệt, hãy tải lại.")
            : new(ToResponse(updated));
    }

    // ------------------------------------------------------------------ lịch sử

    public async Task<AuthOperationResult<StatusHistoryResponse>> GetStatusHistoryAsync(
        long contentId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<StatusHistoryResponse>();

        WorkflowContent? content = await repository.GetContentAsync(contentId, token);
        if (content is null) return ContentNotFound<StatusHistoryResponse>();

        if (await repository.GetRoleAsync(content.ProjectId, user.UserId, token) is null)
            return NotMember<StatusHistoryResponse>();

        IReadOnlyList<StatusHistoryEntry> entries = await repository.GetStatusHistoryAsync(contentId, token);
        return new(new StatusHistoryResponse(
            contentId, entries.Select(ToResponse).ToList()));
    }

    // ------------------------------------------------------------------ helpers

    private static bool IsLocked(WorkflowContentStatus status) => status is WorkflowContentStatus.Published;

    private static ContentStatusResponse ToResponse(WorkflowContent c) =>
        new(c.ContentId, c.Status.ToString(), c.UpdatedAt);

    private static StatusHistoryEntryResponse ToResponse(StatusHistoryEntry e) =>
        new(e.HistoryId, e.FromStatus, e.ToStatus, e.ChangedBy, e.ChangedByName, e.Note, e.ChangedAt);

    private static AuthOperationResult<T> Validation<T>(string message, string field) =>
        AuthOperationResult<T>.Fail("validation", message, field);

    private static AuthOperationResult<T> InvalidTransition<T>(WorkflowContentStatus from, WorkflowContentStatus to) =>
        AuthOperationResult<T>.Fail("invalid_transition", $"Không thể chuyển từ {from} sang {to}.", "status");

    private static AuthOperationResult<T> Conflict<T>(string message) =>
        AuthOperationResult<T>.Fail("conflict", message, status: 409);

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
