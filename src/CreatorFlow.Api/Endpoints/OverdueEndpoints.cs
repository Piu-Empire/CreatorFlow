using CreatorFlow.Api.Services.Overdue;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Overdue: danh sách Content quá deadline nhưng chưa hoàn thành của một Project.
/// Yêu cầu đăng nhập; quyền thành viên Project do OverdueService kiểm tra.
/// </summary>
public static class OverdueEndpoints
{
    public static void MapOverdueEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        api.MapGet("/projects/{projectId:long}/overdue",
            async (long projectId, OverdueService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetOverdueAsync(projectId, token)));
    }
}
