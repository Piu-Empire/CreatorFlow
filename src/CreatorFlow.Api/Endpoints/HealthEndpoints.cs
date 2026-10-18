using CreatorFlow.Api.Services;

namespace CreatorFlow.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", (HealthService service) => TypedResults.Ok(service.GetHealth()));
        endpoints.MapGet("/api/health/ready", async (DatabaseReadinessService service, CancellationToken cancellationToken) =>
        {
            bool ready = await service.IsReadyAsync(cancellationToken);
            return Results.Json(new { status = ready ? "ready" : "not_ready" },
                statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
        });
    }
}
