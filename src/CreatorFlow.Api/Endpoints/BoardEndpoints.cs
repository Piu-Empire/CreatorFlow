using CreatorFlow.Api.Services.Board;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Board (bảng Content theo giai đoạn). Yêu cầu đăng nhập; quyền chi tiết do BoardService kiểm tra.
/// Người thao tác luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public static class BoardEndpoints
{
    public static void MapBoardEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        // Các thẻ Content của Project (kèm nền tảng, tag, Creator được giao, cờ Review đang chờ).
        api.MapGet("/projects/{projectId:long}/board", async (long projectId, BoardService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetBoardAsync(projectId, token)));

        // Kho Content dùng cùng dữ liệu chi tiết nhưng có cả Content đã Archived.
        api.MapGet("/projects/{projectId:long}/contents", async (long projectId, BoardService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetContentLibraryAsync(projectId, token)));
    }
}
