using CreatorFlow.Api.Services.Admin;
using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Api.Endpoints;

/// <summary>Endpoint tố cáo của user thường (không cần quyền admin).</summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder routes)
    {
        var reports = routes.MapGroup("/api/reports").RequireAuthorization();
        reports.MapPost("/", async (CreateReportRequest request, AdminReportService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.CreateReportAsync(request, token)));
    }
}
