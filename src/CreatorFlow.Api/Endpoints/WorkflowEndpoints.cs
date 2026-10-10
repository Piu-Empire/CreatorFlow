using CreatorFlow.Api.Services.Workflow;
using CreatorFlow.Contracts.Workflow;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Workflow trạng thái Content: chuyển bước đi thẳng, gửi duyệt, duyệt/từ chối,
/// và lịch sử đổi trạng thái. Tất cả yêu cầu đăng nhập; quyền chi tiết do WorkflowService kiểm tra.
/// Người thao tác luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        // Chuyển bước đi thẳng: IDEA→SCRIPT, SCRIPT→PRODUCTION, PRODUCTION→EDITING, READY→PUBLISHED.
        api.MapPut("/contents/{contentId:long}/status",
            async (long contentId, ChangeContentStatusRequest request, WorkflowService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.ChangeStatusAsync(contentId, request, token)));

        // Gửi duyệt: EDITING→REVIEW (tạo Review mới, không ghi đè lần duyệt cũ).
        api.MapPost("/contents/{contentId:long}/review",
            async (long contentId, SubmitReviewRequest request, WorkflowService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.SubmitReviewAsync(contentId, request, token)));

        // Duyệt (REVIEW→READY) hoặc từ chối (REVIEW→EDITING): chỉ Owner/Manager.
        api.MapPut("/contents/{contentId:long}/review/decision",
            async (long contentId, ReviewDecisionRequest request, WorkflowService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.DecideReviewAsync(contentId, request, token)));

        // Lịch sử đổi trạng thái của một Content (mới nhất trước).
        api.MapGet("/contents/{contentId:long}/status-history",
            async (long contentId, WorkflowService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetStatusHistoryAsync(contentId, token)));
    }
}
