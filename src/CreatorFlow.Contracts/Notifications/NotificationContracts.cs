namespace CreatorFlow.Contracts.Notifications;

/// <summary>
/// Một thông báo nghiệp vụ của người đang đăng nhập.
/// Người nhận lấy từ JWT ở Backend, không bao giờ nhận từ client.
/// </summary>
public sealed record NotificationResponse(
    long NotificationId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt,
    long? ProjectId,
    long? ContentId,
    string? ContentCode);

/// <summary>Thông báo mới nhất của người gọi kèm số chưa đọc (để hiện badge trên icon Bell).</summary>
public sealed record NotificationsResponse(int UnreadCount, IReadOnlyList<NotificationResponse> Items);

/// <summary>Kết quả đánh dấu một thông báo đã đọc.</summary>
public sealed record NotificationReadResponse(long NotificationId, bool IsRead, DateTime? ReadAt);

/// <summary>Kết quả đánh dấu tất cả thông báo của người gọi đã đọc.</summary>
public sealed record MarkAllReadResponse(int MarkedCount);
