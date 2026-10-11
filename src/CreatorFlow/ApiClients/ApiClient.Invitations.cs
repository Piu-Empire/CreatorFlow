using CreatorFlow.Contracts.Invitations;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi các endpoint mời thành viên của CreatorFlow.Api.
/// Người thao tác luôn là chủ của <c>bearer</c> (JWT); client không bao giờ gửi "tôi là ai" trong body.
/// Dùng chung bộ gửi/đọc lỗi với nhóm Tasks (SendTasksAsync) để không lặp code HTTP.
/// </summary>
public sealed partial class ApiClient
{
    public Task<InvitationResponse> InviteMemberAsync(long projectId, InviteMemberRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<InvitationResponse>(HttpMethod.Post, $"api/projects/{projectId}/invitations", request, bearer, token);

    public Task<InvitationsResponse> GetInvitationsAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<InvitationsResponse>(HttpMethod.Get, $"api/projects/{projectId}/invitations", null, bearer, token);
}
