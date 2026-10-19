using CreatorFlow.Api.Models.Notifications;

namespace CreatorFlow.Api.Repositories.Notifications;

/// <summary>Truy cập bảng notifications bằng Npgsql (chỉ Backend được chạm database).</summary>
public interface INotificationRepository
{
    /// <summary>Ghi thông báo; trả false nếu DedupeKey đã tồn tại (ON CONFLICT DO NOTHING).</summary>
    Task<bool> InsertAsync(NotificationInsert row, CancellationToken cancellationToken = default);

    /// <summary>Thông báo mới nhất của người nhận (mới nhất trước, tối đa limit dòng).</summary>
    Task<IReadOnlyList<NotificationRecord>> GetForUserAsync(
        long userId, bool unreadOnly, int limit, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Đánh dấu 1 thông báo đã đọc; null nếu không tồn tại hoặc không thuộc người gọi.</summary>
    Task<DateTime?> MarkReadAsync(
        long notificationId, long userId, DateTime readAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Đánh dấu tất cả thông báo chưa đọc của người gọi; trả về số dòng đã đổi.</summary>
    Task<int> MarkAllReadAsync(long userId, DateTime readAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assignment đang hiệu lực của User có hạn (deadline riêng của assignment, nếu không có thì của Content)
    /// không muộn hơn <paramref name="dueOn"/>; bỏ assignment đã xong/huỷ và Content đã Published/Archived.
    /// </summary>
    Task<IReadOnlyList<DueAssignment>> GetDueAssignmentsForUserAsync(
        long userId, DateOnly dueOn, CancellationToken cancellationToken = default);
}
