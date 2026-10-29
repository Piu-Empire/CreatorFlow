using CreatorFlow.Api.Services.Statistics;

namespace CreatorFlow.Api.Endpoints;

/// <summary>Thống kê theo Creator và Project (SCRUM-55). Người xem lấy từ JWT; quyền do StatisticsService kiểm tra.</summary>
public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        // ?creatorId=... : chỉ thu hẹp trong đúng Project trong URL; bỏ trống = mọi Creator của Project.
        api.MapGet("/projects/{projectId:long}/statistics/creators",
            async (long projectId, long? creatorId, StatisticsService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetCreatorStatisticsAsync(projectId, creatorId, token)));
    }
}
