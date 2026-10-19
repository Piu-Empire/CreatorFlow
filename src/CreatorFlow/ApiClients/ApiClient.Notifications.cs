using CreatorFlow.Contracts.Notifications;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi endpoint Thông báo của CreatorFlow.Api (SCRUM-50).
/// Người nhận luôn là chủ của <c>bearer</c> (JWT); Backend quyết định đúng người và dedupe.
/// </summary>
public sealed partial class ApiClient
{
    public Task<NotificationsResponse> GetNotificationsAsync(bool unreadOnly, string bearer, CancellationToken token = default) =>
        SendTasksAsync<NotificationsResponse>(HttpMethod.Get, $"api/notifications?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}", null, bearer, token);

    public Task<NotificationReadResponse> MarkNotificationReadAsync(long notificationId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<NotificationReadResponse>(HttpMethod.Post, $"api/notifications/{notificationId}/read", null, bearer, token);

    public Task<MarkAllReadResponse> MarkAllNotificationsReadAsync(string bearer, CancellationToken token = default) =>
        SendTasksAsync<MarkAllReadResponse>(HttpMethod.Post, "api/notifications/read-all", null, bearer, token);
}
