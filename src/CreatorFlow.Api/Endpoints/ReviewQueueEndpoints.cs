using CreatorFlow.Api.Services.ReviewQueue;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Review Queue: danh sách Content đang chờ duyệt của một Project.
/// Yêu cầu đăng nhập; quyền Owner/Manager do ReviewQueueService kiểm tra.
/// </summary>
public static class ReviewQueueEndpoints
{
    public static void MapReviewQueueEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        api.MapGet("/projects/{projectId:long}/reviews/pending",
            async (long projectId, ReviewQueueService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetPendingAsync(projectId, token)));
    }
}
