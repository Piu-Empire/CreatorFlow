using CreatorFlow.Contracts.Invitations;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi các endpoint "lời mời của tôi" của CreatorFlow.Api: xem lời mời gửi tới mình, chấp nhận, từ chối.
/// Người thao tác luôn là chủ của <c>bearer</c> (JWT); client không bao giờ gửi "tôi là ai" trong body.
/// Dùng chung bộ gửi/đọc lỗi với nhóm Tasks (SendTasksAsync) để không lặp code HTTP.
/// </summary>
public sealed partial class ApiClient
{
    public Task<MyInvitationsResponse> GetMyInvitationsAsync(string bearer, CancellationToken token = default) =>
        SendTasksAsync<MyInvitationsResponse>(HttpMethod.Get, "api/invitations/mine", null, bearer, token);

    public Task<MyInvitationResponse> AcceptInvitationAsync(long invitationId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<MyInvitationResponse>(HttpMethod.Post, $"api/invitations/{invitationId}/accept", null, bearer, token);

    public Task<MyInvitationResponse> RejectInvitationAsync(long invitationId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<MyInvitationResponse>(HttpMethod.Post, $"api/invitations/{invitationId}/reject", null, bearer, token);
}
