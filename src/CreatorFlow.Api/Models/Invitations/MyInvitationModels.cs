using CreatorFlow.Api.Models.Tasks;

namespace CreatorFlow.Api.Models.Invitations;

/// <summary>Lời mời nhìn từ phía người được mời: kèm tên Project để họ biết mình đang được mời vào đâu. Token không nằm trong model.</summary>
public sealed record MyInvitation(
    long InvitationId,
    long ProjectId,
    string ProjectName,
    ProjectRole Role,
    InvitationStatus Status,
    string InvitedByName,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt);

/// <summary>Kết quả của việc chấp nhận / từ chối một lời mời.</summary>
public enum InvitationDecisionOutcome
{
    /// <summary>Đã ghi nhận quyết định (và tạo membership nếu là chấp nhận).</summary>
    Decided,

    /// <summary>Không có lời mời này gửi tới email của người thao tác (kể cả khi lời mời tồn tại nhưng dành cho người khác).</summary>
    NotFound,

    /// <summary>Lời mời đã được trả lời / huỷ / hết hạn từ trước, không còn ở trạng thái Pending.</summary>
    NotPending,

    /// <summary>Lời mời còn ghi Pending nhưng đã quá hạn; đã được chuyển sang Expired.</summary>
    Expired,
}

/// <param name="Invitation">Lời mời sau khi cập nhật; chỉ có khi Outcome = Decided.</param>
/// <param name="CurrentStatus">Trạng thái hiện tại của lời mời; chỉ có khi Outcome = NotPending.</param>
public sealed record InvitationDecisionResult(
    InvitationDecisionOutcome Outcome,
    MyInvitation? Invitation = null,
    InvitationStatus? CurrentStatus = null);
