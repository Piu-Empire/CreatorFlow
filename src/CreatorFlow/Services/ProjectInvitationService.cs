using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Invitations;
using CreatorFlow.Services.Backends;

namespace CreatorFlow.Services;

/// <summary>
/// Mời thành viên vào Project qua CreatorFlow.Api (SCRUM-26).
/// Quyền (chỉ Owner), kiểm tra trùng thành viên / trùng lời mời do BACKEND quyết định; lớp này chỉ gọi API bằng token
/// của phiên đăng nhập và đổi lỗi API sang ngoại lệ nghiệp vụ mà giao diện đã xử lý:
/// ContentValidationException (dữ liệu sai, trùng, mất mạng...) / UnauthorizedWorkflowActionException (không đủ quyền, hết phiên).
/// </summary>
public sealed class ProjectInvitationService(ApiClient api, UserSession session) : IInvitationBackend
{
    /// <summary>Owner mời <paramref name="email"/> với vai trò "Manager" hoặc "Creator". Trả về lời mời vừa tạo (Pending).</summary>
    public InvitationResponse Invite(long projectId, string email, string role) =>
        ApiTaskCall.Run(() => api.InviteMemberAsync(
            projectId, new InviteMemberRequest(email, role), ApiTaskCall.BearerOf(session)));

    /// <summary>Các lời mời của Project, mới nhất trước (chỉ Owner xem được).</summary>
    public IReadOnlyList<InvitationResponse> GetInvitations(long projectId) =>
        ApiTaskCall.Run(() => api.GetInvitationsAsync(projectId, ApiTaskCall.BearerOf(session))).Invitations;
}
