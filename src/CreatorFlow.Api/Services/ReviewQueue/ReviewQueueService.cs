using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.ReviewQueue;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.ReviewQueue;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.ReviewQueue;

namespace CreatorFlow.Api.Services.ReviewQueue;

/// <summary>
/// Review Queue (UX mục 2, dành cho Owner/Manager). Quyền được kiểm tra Ở ĐÂY:
///   - phải đăng nhập;
///   - phải là thành viên Project;
///   - chỉ Owner/Manager được xem danh sách chờ duyệt.
/// Người xem luôn lấy từ JWT, không nhận từ query/body.
/// </summary>
public sealed class ReviewQueueService(
    IReviewQueueRepository repository,
    ITaskRepository tasks,
    CurrentAuthenticatedUser current)
{
    public async Task<AuthOperationResult<ReviewQueueResponse>> GetPendingAsync(
        long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized();

        ProjectRole? role = await tasks.GetRoleAsync(projectId, user.UserId, token);
        if (role is null) return Forbidden("Bạn không thuộc Project này.");

        if (role != ProjectRole.Owner && role != ProjectRole.Manager)
            return Forbidden("Chỉ Owner hoặc Manager mới xem được Review Queue.");

        IReadOnlyList<PendingReviewRecord> items = await repository.GetPendingAsync(projectId, token);
        return new(new ReviewQueueResponse(projectId,
            items.Select(ToResponse).ToList()));
    }

    private static PendingReviewResponse ToResponse(PendingReviewRecord item) =>
        new(item.ContentId, item.ProjectId, $"CNT-{item.ContentId:000}", item.ContentTitle,
            item.SubmittedByName, item.SubmittedAt, item.ReviewNo);

    private static AuthOperationResult<ReviewQueueResponse> Unauthorized() =>
        AuthOperationResult<ReviewQueueResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);

    private static AuthOperationResult<ReviewQueueResponse> Forbidden(string message) =>
        AuthOperationResult<ReviewQueueResponse>.Fail("forbidden", message, status: 403);
}
