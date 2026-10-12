using CreatorFlow.Api.Models.Invitations;

namespace CreatorFlow.Api.Repositories.Invitations;

/// <summary>Truy cập project_invitations / project_members từ phía NGƯỜI ĐƯỢC MỜI (chỉ Backend được chạm database).</summary>
public interface IMyInvitationRepository
{
    /// <summary>Mọi lời mời gửi tới email này (không phân biệt hoa/thường), mới nhất trước.</summary>
    Task<IReadOnlyList<MyInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trong MỘT transaction, khoá dòng lời mời (SELECT ... FOR UPDATE) rồi:
    /// không có lời mời gửi tới email này → NotFound; không còn Pending → NotPending; Pending quá hạn → đánh dấu Expired;
    /// chấp nhận vào Project đã lưu trữ → ProjectArchived (không ghi gì, lời mời vẫn Pending); ngược lại chuyển sang <paramref name="decision"/> (Accepted hoặc Rejected). Chỉ khi chấp nhận mới tạo (hoặc kích hoạt lại) project_members.
    /// Khoá dòng khiến hai lần chấp nhận đồng thời chỉ có một lần thành công, nên membership chỉ được tạo đúng một lần.
    /// </summary>
    Task<InvitationDecisionResult> DecideAsync(
        long invitationId, long userId, string email, InvitationStatus decision, DateTime nowUtc, CancellationToken cancellationToken = default);
}
