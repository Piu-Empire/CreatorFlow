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

        // SCRUM-23: gói dịch vụ + đăng ký gói + thống kê hệ thống.
        admin.MapGet("/plans", async (AdminPlanService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetPlansAsync(token)));
        admin.MapPost("/plans", async (CreatePlanRequest request, AdminPlanService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.CreatePlanAsync(request, token)));
        admin.MapPatch("/plans/{planId:long}", async (long planId, UpdatePlanRequest request,
            AdminPlanService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.UpdatePlanAsync(planId, request, token)));

        admin.MapGet("/subscriptions", async (string? status, long? planId, string? search, int? limit, int? offset,
            AdminSubscriptionService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetSubscriptionsAsync(status, planId, search, limit, offset, token)));
        admin.MapPost("/subscriptions", async (CreateSubscriptionRequest request,
            AdminSubscriptionService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.CreateSubscriptionAsync(request, token)));
        admin.MapPatch("/subscriptions/{subscriptionId:long}", async (long subscriptionId, UpdateSubscriptionRequest request,
            AdminSubscriptionService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.UpdateSubscriptionAsync(subscriptionId, request, token)));

        admin.MapGet("/statistics", async (AdminStatisticsService service, CancellationToken token) =>
            AuthEndpoints.ToHttp(await service.GetStatisticsAsync(token)));
    }
}
