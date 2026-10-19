using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Notifications;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Repositories.Notifications;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Notifications;

namespace CreatorFlow.Api.Services.Notifications;

/// <summary>
/// Thông báo nghiệp vụ (SCRUM-50). Quyền và người nhận được xác định Ở ĐÂY, không tin client:
///   - đọc / đánh dấu đọc: chỉ thông báo của chính người đang đăng nhập (JWT);
///   - giao việc → Creator được giao; gửi duyệt → Owner/Manager đang hoạt động; duyệt/từ chối → người gửi Review;
///   - không gửi thông báo cho chính người vừa thao tác.
/// Thông báo deadline được sinh LƯỜI khi người dùng mở danh sách và chống trùng bằng dedupe_key
/// (một khoá cho mỗi người nhận, mỗi sự kiện) nên không cần job nền và gọi lại không tạo thêm dòng.
/// Ghi thông báo là hiệu ứng phụ: mọi lỗi chỉ được ghi log, không làm hỏng nghiệp vụ chính.
/// </summary>
public sealed class NotificationService(
    INotificationRepository repository,
    ITaskRepository tasks,
    IUserRepository users,
    IEmailSender emailSender,
    CurrentAuthenticatedUser current,
    TimeProvider timeProvider,
    ILogger<NotificationService> logger)
{
    private const int MaxItems = 50;

    // Hạn trong vòng bao nhiêu ngày thì coi là "sắp tới hạn".
    private const int NearDays = 2;

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    // ------------------------------------------------------------------ đọc

    public async Task<AuthOperationResult<NotificationsResponse>> GetMineAsync(
        bool unreadOnly, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<NotificationsResponse>();

        bool schemaReady;
        try
        {
            schemaReady = await repository.IsSchemaReadyAsync(token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Không kiểm tra được schema notifications.");
            schemaReady = false;
        }

        // Sinh thông báo deadline cho chính người gọi trước khi đọc (idempotent nhờ dedupe_key).
        // Chỉ sinh khi schema đã sẵn sàng; nếu chưa thì bỏ qua để không spam log.
        if (schemaReady)
            await EnsureDeadlineNotificationsAsync(user.UserId, token);

        IReadOnlyList<NotificationRecord> items =
            await repository.GetForUserAsync(user.UserId, unreadOnly, MaxItems, token);
        int unread = await repository.CountUnreadAsync(user.UserId, token);

        string? schemaMessage = schemaReady
            ? null
            : "Thông báo cần chạy migration database/09_notifications.sql trên DB (thêm cột dedupe_key) trước khi hoạt động đầy đủ.";

        return new(new NotificationsResponse(unread, items.Select(ToResponse).ToList(), schemaReady, schemaMessage));
    }

    public async Task<AuthOperationResult<NotificationReadResponse>> MarkReadAsync(
        long notificationId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<NotificationReadResponse>();

        DateTime? readAt = await repository.MarkReadAsync(
            notificationId, user.UserId, timeProvider.GetUtcNow().UtcDateTime, token);
        return readAt is null
            ? AuthOperationResult<NotificationReadResponse>.Fail(
                "not_found", "Không tìm thấy thông báo.", status: 404)
            : new(new NotificationReadResponse(notificationId, true, readAt));
    }

    public async Task<AuthOperationResult<MarkAllReadResponse>> MarkAllReadAsync(CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<MarkAllReadResponse>();

        int marked = await repository.MarkAllReadAsync(user.UserId, timeProvider.GetUtcNow().UtcDateTime, token);
        return new(new MarkAllReadResponse(marked));
    }

    // ------------------------------------------------------------------ ghi (hiệu ứng phụ, không ném ra ngoài)

    /// <summary>Báo cho Creator vừa được giao Content. Không báo khi tự nhận việc của chính mình.</summary>
    public async Task NotifyAssignmentAsync(
        long contentId, long projectId, string contentCode, string contentTitle,
        long assignmentId, long recipientUserId, long actorUserId, CancellationToken token = default)
    {
        if (recipientUserId == actorUserId) return;

        await InsertBestEffortAsync(new NotificationInsert(
            recipientUserId, projectId, contentId, NotificationType.Assignment,
            $"Bạn được giao nội dung {contentCode}",
            $"Bạn được giao thực hiện nội dung \"{contentTitle}\".",
            NotificationKeys.Assignment(contentId, recipientUserId, assignmentId)), sendEmail: true, token);
    }

    /// <summary>Báo cho Owner/Manager đang hoạt động (trừ người gửi) rằng có Review mới cần duyệt.</summary>
    public async Task NotifyReviewSubmittedAsync(
        long contentId, long projectId, string contentCode, string contentTitle,
        int reviewNo, long actorUserId, CancellationToken token = default)
    {
        IReadOnlyList<ProjectMember> members;
        try
        {
            members = await tasks.GetMembersAsync(projectId, token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Không đọc được thành viên Project {ProjectId} để gửi thông báo duyệt.", projectId);
            return;
        }

        foreach (ProjectMember member in members.Where(m =>
            m.Role is ProjectRole.Owner or ProjectRole.Manager && m.UserId != actorUserId))
        {
            await InsertBestEffortAsync(new NotificationInsert(
                member.UserId, projectId, contentId, NotificationType.ReviewSubmitted,
                $"Có nội dung chờ duyệt: {contentCode}",
                $"Nội dung \"{contentTitle}\" vừa được gửi duyệt (lần {reviewNo}).",
                NotificationKeys.Review(NotificationType.ReviewSubmitted, contentId, member.UserId, reviewNo)),
                sendEmail: true, token);
        }
    }

    /// <summary>Báo cho người gửi Review kết quả duyệt/từ chối kèm feedback khi bị từ chối.</summary>
    public async Task NotifyReviewDecidedAsync(
        long contentId, long projectId, string contentCode, string contentTitle, int reviewNo,
        long submitterUserId, bool approved, string? feedback, long actorUserId, CancellationToken token = default)
    {
        if (submitterUserId == actorUserId) return;

        NotificationType type = approved ? NotificationType.ReviewApproved : NotificationType.ReviewRejected;
        string message = approved
            ? $"Nội dung \"{contentTitle}\" (lần {reviewNo}) đã được duyệt."
            : $"Nội dung \"{contentTitle}\" (lần {reviewNo}) bị từ chối. Feedback: {feedback}";

        await InsertBestEffortAsync(new NotificationInsert(
            submitterUserId, projectId, contentId, type,
            approved ? $"Nội dung {contentCode} đã được duyệt" : $"Nội dung {contentCode} bị từ chối",
            message,
            NotificationKeys.Review(type, contentId, submitterUserId, reviewNo)), sendEmail: true, token);
    }

    /// <summary>
    /// Bảo đảm mỗi assignment đang tới hạn/trễ của người gọi có đúng một thông báo cho mỗi ngày (UTC).
    /// Khoá theo ngày nên thông báo trễ hạn sẽ được nhắc lại vào ngày khác, nhưng không trùng trong cùng ngày.
    /// </summary>
    private async Task EnsureDeadlineNotificationsAsync(long userId, CancellationToken token)
    {
        try
        {
            DateOnly today = Today;
            IReadOnlyList<DueAssignment> due =
                await repository.GetDueAssignmentsForUserAsync(userId, today.AddDays(NearDays), token);

            foreach (DueAssignment item in due)
            {
                bool overdue = item.Deadline < today;
                NotificationType type = overdue ? NotificationType.Overdue : NotificationType.DeadlineNear;
                string deadline = item.Deadline.ToString("dd/MM/yyyy");
                string message = overdue
                    ? $"Nội dung \"{item.ContentTitle}\" đã quá hạn {deadline}."
                    : $"Nội dung \"{item.ContentTitle}\" tới hạn {deadline}.";

                await InsertBestEffortAsync(new NotificationInsert(
                    userId, item.ProjectId, item.ContentId, type,
                    overdue ? $"Nội dung {item.ContentCode} đã trễ hạn" : $"Nội dung {item.ContentCode} sắp tới hạn",
                    message,
                    NotificationKeys.Deadline(type, item.ContentId, userId, today)), sendEmail: false, token);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Không sinh được thông báo deadline cho user {UserId}.", userId);
        }
    }

    /// <summary>
    /// Ghi thông báo rồi gửi email (nếu được yêu cầu và SMTP đã cấu hình).
    /// Chỉ gửi email khi bản ghi được tạo thật (dedupe) để không gửi trùng.
    /// </summary>
    private async Task InsertBestEffortAsync(NotificationInsert row, bool sendEmail, CancellationToken token)
    {
        try
        {
            if (!await repository.InsertAsync(row, token)) return;
            if (!sendEmail || !emailSender.IsConfigured) return;

            User? recipient = await users.FindByIdAsync(row.RecipientUserId, token);
            if (recipient is null) return;
            await emailSender.SendNotificationAsync(recipient.Email, row.Title, row.Message, token);
        }
        catch (Exception exception)
        {
            // Thông báo là hiệu ứng phụ: không được làm hỏng thao tác nghiệp vụ đã thành công.
            logger.LogError(exception,
                "Không ghi/gửi được thông báo {Type} cho user {UserId} (Content {ContentId}).",
                row.Type, row.RecipientUserId, row.ContentId);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static NotificationResponse ToResponse(NotificationRecord n) => new(
        n.NotificationId, n.Type, n.Title, n.Message, n.IsRead, n.CreatedAt, n.ReadAt,
        n.ProjectId, n.ContentId, n.ContentCode);

    private static AuthOperationResult<T> Unauthorized<T>() =>
        AuthOperationResult<T>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
}
