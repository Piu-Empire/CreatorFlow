using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Invitations;
using CreatorFlow.Services.Backends;

namespace CreatorFlow.Services;

/// <summary>
/// Hộp thư lời mời của NGƯỜI ĐƯỢC MỜI qua CreatorFlow.Api: xem lời mời gửi tới mình, chấp nhận, từ chối.
/// Quyền, hạn dùng, trạng thái và việc tạo thành viên Project do BACKEND quyết định; lớp này chỉ gọi API bằng token
/// của phiên đăng nhập và đổi lỗi API sang ngoại lệ nghiệp vụ mà giao diện đã xử lý:
/// ContentValidationException (lời mời hết hạn / đã được trả lời, mất mạng...) / UnauthorizedWorkflowActionException (hết phiên) /
/// InvalidOperationException (API trả 404: không có lời mời này).
/// </summary>
public sealed class InvitationInboxService(ApiClient api, UserSession session)
{
    /// <summary>Các lời mời gửi tới tài khoản đang đăng nhập (mọi trạng thái), mới nhất trước.</summary>
    public IReadOnlyList<MyInvitationResponse> GetMine() =>
        ApiTaskCall.Run(() => api.GetMyInvitationsAsync(ApiTaskCall.BearerOf(session))).Invitations;

    /// <summary>Chấp nhận lời mời: người dùng trở thành thành viên Project. Trả về lời mời đã ở trạng thái Accepted.</summary>
    public MyInvitationResponse Accept(long invitationId) =>
        ApiTaskCall.Run(() => api.AcceptInvitationAsync(invitationId, ApiTaskCall.BearerOf(session)));

    /// <summary>Từ chối lời mời: không tạo thành viên. Trả về lời mời đã ở trạng thái Rejected.</summary>
    public MyInvitationResponse Reject(long invitationId) =>
        ApiTaskCall.Run(() => api.RejectInvitationAsync(invitationId, ApiTaskCall.BearerOf(session)));
}
