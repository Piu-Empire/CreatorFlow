using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nguồn dữ liệu Thông báo khi chạy qua CreatorFlow.Api (SCRUM-50).
/// Người nhận lấy từ JWT ở Backend; client chỉ đổi DTO sang model hiển thị và đánh dấu đã đọc.
/// </summary>
public interface INotificationBackend
{
    /// <summary>Danh sách thông báo mới nhất kèm số chưa đọc.</summary>
    NotificationListResult GetNotifications(bool unreadOnly);

    /// <summary>Đánh dấu một thông báo đã đọc.</summary>
    void MarkRead(long notificationId);

    /// <summary>Đánh dấu tất cả thông báo của người gọi đã đọc.</summary>
    void MarkAllRead();
}

/// <summary>Kết quả đọc danh sách thông báo: số chưa đọc + các dòng.</summary>
public sealed class NotificationListResult
{
    public int UnreadCount { get; set; }
    public List<NotificationItem> Items { get; set; } = new();
}
