using CreatorFlow.Contracts.Invitations;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nguồn dữ liệu cho màn "Mời thành viên" (SCRUM-26). Cài đặt thật là ProjectInvitationService (gọi CreatorFlow.Api).
/// Giao diện chỉ phụ thuộc interface này, giống cách IdeaBank dùng IIdeaBackend.
/// Lỗi được đổi sang ContentValidationException / UnauthorizedWorkflowActionException / InvalidOperationException (404).
/// </summary>
public interface IInvitationBackend
{
    /// <summary>Owner mời <paramref name="email"/> với vai trò "Manager" hoặc "Creator". Trả về lời mời vừa tạo (Pending).</summary>
    InvitationResponse Invite(long projectId, string email, string role);

    /// <summary>Các lời mời của Project, mới nhất trước (chỉ Owner xem được).</summary>
    IReadOnlyList<InvitationResponse> GetInvitations(long projectId);
}
