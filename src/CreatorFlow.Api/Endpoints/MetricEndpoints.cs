using CreatorFlow.Api.Services.Metrics;
using CreatorFlow.Contracts.Metrics;

namespace CreatorFlow.Api.Endpoints;

/// <summary>Metrics theo ContentPlatform. Người thao tác lấy từ JWT; quyền chi tiết do MetricService kiểm tra.</summary>
public static class MetricEndpoints
{
    public static void MapMetricEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        // Metrics mới nhất của mọi platform trong Content (platform chưa có số liệu: latest = null).
        api.MapGet("/contents/{contentId:long}/metrics", async (long contentId, MetricService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetContentMetricsAsync(contentId, token)));

        // Một platform; ?history=true trả thêm lịch sử (tối đa 200, mới → cũ).
        api.MapGet("/contents/{contentId:long}/platforms/{platformId:long}/metrics",
            async (long contentId, long platformId, bool? history, MetricService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetPlatformMetricsAsync(contentId, platformId, history ?? false, token)));

        // Nhập snapshot mới (Owner/Manager, chỉ sau Published).
        api.MapPost("/contents/{contentId:long}/platforms/{platformId:long}/metrics",
            async (long contentId, long platformId, RecordMetricRequest request, MetricService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.RecordAsync(contentId, platformId, request, token)));

        // Tổng hợp Metrics của Project theo chiều (SCRUM-52): ?groupBy=content|creator|platform|project.
        api.MapGet("/projects/{projectId:long}/metrics/summary",
            async (long projectId, string? groupBy, MetricService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetSummaryAsync(projectId, groupBy, token)));
    }
}
