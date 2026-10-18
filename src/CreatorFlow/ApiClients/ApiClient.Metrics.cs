using CreatorFlow.Contracts.Metrics;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi các endpoint Metrics theo ContentPlatform.
/// Người thao tác luôn là chủ của <c>bearer</c> (JWT); client không bao giờ gửi "tôi là ai" trong body.
/// </summary>
public sealed partial class ApiClient
{
    public Task<ContentMetricsResponse> GetContentMetricsAsync(long contentId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentMetricsResponse>(HttpMethod.Get, $"api/contents/{contentId}/metrics", null, bearer, token);

    public Task<PlatformMetricsResponse> GetPlatformMetricsAsync(long contentId, long platformId, bool history, string bearer, CancellationToken token = default) =>
        SendTasksAsync<PlatformMetricsResponse>(HttpMethod.Get,
            $"api/contents/{contentId}/platforms/{platformId}/metrics{(history ? "?history=true" : string.Empty)}", null, bearer, token);

    public Task<PlatformMetricsResponse> RecordMetricAsync(long contentId, long platformId, RecordMetricRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<PlatformMetricsResponse>(HttpMethod.Post, $"api/contents/{contentId}/platforms/{platformId}/metrics", request, bearer, token);
}
