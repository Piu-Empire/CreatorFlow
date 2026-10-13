using CreatorFlow.Api.Services.Ideas;
using CreatorFlow.Contracts.Ideas;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Idea Bank (kho ý tưởng). Tất cả yêu cầu đăng nhập; quyền chi tiết do IdeaService kiểm tra.
/// Người thao tác luôn lấy từ JWT, không bao giờ nhận từ body/route.
/// </summary>
public static class IdeaEndpoints
{
    public static void MapIdeaEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        api.MapGet("/projects/{projectId:long}/ideas",
            async (long projectId, string? search, string? status, string? tag, IdeaService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetIdeasAsync(projectId, search, status, tag, token)));

        api.MapGet("/projects/{projectId:long}/idea-tags", async (long projectId, IdeaService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetTagsAsync(projectId, token)));

        api.MapPost("/projects/{projectId:long}/ideas", async (long projectId, IdeaRequest request, IdeaService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.CreateAsync(projectId, request, token)));

        api.MapPut("/ideas/{ideaId:long}", async (long ideaId, IdeaRequest request, IdeaService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.UpdateAsync(ideaId, request, token)));

        api.MapPost("/ideas/{ideaId:long}/convert", async (long ideaId, IdeaService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.ConvertAsync(ideaId, token)));

        api.MapDelete("/ideas/{ideaId:long}", async (long ideaId, IdeaService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.DeleteAsync(ideaId, token)));
    }
}
