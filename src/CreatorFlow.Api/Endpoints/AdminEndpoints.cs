using CreatorFlow.Api.Services.Admin;
using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Api.Endpoints;

/// <summary>Endpoint quản trị hệ thống, bảo vệ bởi policy SystemAdminOnly ở cấp group.</summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var admin = routes.MapGroup("/api/admin").RequireAuthorization("SystemAdminOnly");
        admin.MapGet("/users", async (string? search, string? status, int? limit, int? offset,
            AdminUserService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetUsersAsync(search, status, limit, offset, token)));
        admin.MapPatch("/users/{userId:long}/status", async (long userId, UpdateUserStatusRequest request,
            HttpContext context, AdminUserService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.UpdateUserStatusAsync(userId, request, context.TraceIdentifier, token)));
        admin.MapGet("/reports", async (string? search, string? status, int? limit, int? offset,
            AdminReportService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetReportsAsync(search, status, limit, offset, token)));
        admin.MapPatch("/reports/{reportId:long}", async (long reportId, HandleReportRequest request,
            HttpContext context, AdminReportService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.HandleReportAsync(reportId, request, context.TraceIdentifier, token)));
    }
}
