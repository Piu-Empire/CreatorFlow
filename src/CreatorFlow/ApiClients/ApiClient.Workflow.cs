using CreatorFlow.Contracts.Workflow;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi các endpoint Workflow của CreatorFlow.Api.
/// Người thao tác luôn là chủ của <c>bearer</c> (JWT); client không bao giờ gửi "tôi là ai" trong body.
/// </summary>
public sealed partial class ApiClient
{
    /// <summary>Gửi duyệt: EDITING → REVIEW (Backend tạo Review mới, ghi người gửi + thời gian).</summary>
    public Task<ContentStatusResponse> SubmitReviewAsync(long contentId, SubmitReviewRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentStatusResponse>(HttpMethod.Post, $"api/contents/{contentId}/review", request, bearer, token);

    /// <summary>Duyệt (REVIEW → READY) hoặc từ chối (REVIEW → EDITING, bắt buộc Feedback). Chỉ Owner/Manager.</summary>
    public Task<ContentStatusResponse> DecideReviewAsync(long contentId, ReviewDecisionRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentStatusResponse>(HttpMethod.Put, $"api/contents/{contentId}/review/decision", request, bearer, token);

    /// <summary>Lịch sử đổi trạng thái của Content (mới nhất trước), gồm feedback khi từ chối.</summary>
    public Task<StatusHistoryResponse> GetStatusHistoryAsync(long contentId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<StatusHistoryResponse>(HttpMethod.Get, $"api/contents/{contentId}/status-history", null, bearer, token);
}
