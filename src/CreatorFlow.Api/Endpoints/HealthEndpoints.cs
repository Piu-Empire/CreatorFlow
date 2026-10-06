using CreatorFlow.Api.Services;

namespace CreatorFlow.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", (HealthService service) => TypedResults.Ok(service.GetHealth()));
    }
}
