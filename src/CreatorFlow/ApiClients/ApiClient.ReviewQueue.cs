using CreatorFlow.Contracts.ReviewQueue;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi endpoint Review Queue của CreatorFlow.Api.
/// Người xem luôn là chủ của <c>bearer</c> (JWT); quyền Owner/Manager do Backend kiểm tra.
/// </summary>
public sealed partial class ApiClient
{
    public Task<ReviewQueueResponse> GetPendingReviewsAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ReviewQueueResponse>(HttpMethod.Get, $"api/projects/{projectId}/reviews/pending", null, bearer, token);
}
