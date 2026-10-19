using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Services.Search;

namespace CreatorFlow.Api.Endpoints;

public static class GlobalSearchEndpoints
{
    public static void MapGlobalSearchEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/search", async (string? q, int? limit, GlobalSearchService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.SearchAsync(q, limit ?? 30, token)))
            .RequireAuthorization();
    }
}
