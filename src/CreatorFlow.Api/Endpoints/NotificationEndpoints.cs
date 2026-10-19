using CreatorFlow.Api.Services.Notifications;

namespace CreatorFlow.Api.Endpoints;

/// <summary>
/// Thông báo nghiệp vụ của người đang đăng nhập. Yêu cầu đăng nhập; người nhận luôn lấy từ JWT,
/// không nhận từ route/body.
/// </summary>
public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").RequireAuthorization();

        api.MapGet("/notifications",
            async (bool? unreadOnly, NotificationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.GetMineAsync(unreadOnly ?? false, token)));

        api.MapPost("/notifications/{notificationId:long}/read",
            async (long notificationId, NotificationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.MarkReadAsync(notificationId, token)));

        api.MapPost("/notifications/read-all",
            async (NotificationService service, CancellationToken token) =>
                AuthEndpoints.ToHttp(await service.MarkAllReadAsync(token)));
    }
}
