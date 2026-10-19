using CreatorFlow.Api.Services.ContentDetail;
using CreatorFlow.Contracts.ContentDetail;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Content Detail: đọc/sửa thông tin lập kế hoạch, script (có phát hiện xung đột) và Activity timeline.
/// Tất cả yêu cầu đăng nhập; quyền chi tiết do ContentDetailService kiểm tra. Người thao tác luôn lấy từ JWT.
/// </summary>
public static class ContentDetailEndpoints
{
    public static void MapContentDetailEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        api.MapGet("/platforms",
            async (ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetPlatformsAsync(token)));

        api.MapPost("/projects/{projectId:long}/contents",
            async (long projectId, CreateContentRequest request, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.CreateAsync(projectId, request, token)));

        api.MapGet("/contents/{contentId:long}",
            async (long contentId, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetDetailAsync(contentId, token)));

        api.MapPut("/contents/{contentId:long}",
            async (long contentId, UpdateContentRequest request, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.UpdateAsync(contentId, request, token)));

        api.MapGet("/contents/{contentId:long}/script",
            async (long contentId, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetScriptAsync(contentId, token)));

        api.MapPut("/contents/{contentId:long}/script",
            async (long contentId, SaveScriptRequest request, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.SaveScriptAsync(contentId, request, token)));

        api.MapGet("/contents/{contentId:long}/activity",
            async (long contentId, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetActivityAsync(contentId, token)));

        api.MapGet("/contents/{contentId:long}/references",
            async (long contentId, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetReferencesAsync(contentId, token)));

        api.MapPost("/contents/{contentId:long}/references",
            async (long contentId, CreateContentReferenceRequest request, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.AddReferenceAsync(contentId, request, token)));

        api.MapDelete("/contents/{contentId:long}/references/{referenceId:long}",
            async (long contentId, long referenceId, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.DeleteReferenceAsync(contentId, referenceId, token)));

        api.MapPut("/contents/{contentId:long}/references",
            async (long contentId, SaveContentReferenceChangesRequest request, ContentDetailService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.SaveReferenceChangesAsync(contentId, request, token)));
    }
}
