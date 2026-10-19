using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Notifications;
using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>Thông báo chạy qua CreatorFlow.Api (Backend quyết định đúng người, dedupe và gửi email).</summary>
public sealed class ApiNotificationBackend(ApiClient api, UserSession session) : INotificationBackend
{
    public NotificationListResult GetNotifications(bool unreadOnly) =>
        ApiTaskCall.Run(async () =>
        {
            var response = await api.GetNotificationsAsync(unreadOnly, ApiTaskCall.BearerOf(session));
            return new NotificationListResult
            {
                UnreadCount = response.UnreadCount,
                Items = response.Items.Select(i => new NotificationItem
                {
                    NotificationId = i.NotificationId,
                    Type = i.Type,
                    Title = i.Title,
                    Message = i.Message,
                    IsRead = i.IsRead,
                    CreatedAt = i.CreatedAt.ToLocalTime(),
                    ReadAt = i.ReadAt?.ToLocalTime(),
                    ProjectId = i.ProjectId,
                    ContentId = i.ContentId,
                    ContentCode = i.ContentCode,
                }).ToList(),
            };
        }, session);

    public void MarkRead(long notificationId) =>
        ApiTaskCall.Run(async () =>
        {
            await api.MarkNotificationReadAsync(notificationId, ApiTaskCall.BearerOf(session));
            return (object?)null;
        }, session);

    public void MarkAllRead() =>
        ApiTaskCall.Run(async () =>
        {
            await api.MarkAllNotificationsReadAsync(ApiTaskCall.BearerOf(session));
            return (object?)null;
        }, session);
}
