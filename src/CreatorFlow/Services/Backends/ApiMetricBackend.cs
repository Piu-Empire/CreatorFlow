using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Metrics;

namespace CreatorFlow.Services.Backends;

/// <summary>Metrics theo ContentPlatform chạy qua CreatorFlow.Api (quyền, Published, validation kiểm tra ở Backend).</summary>
public sealed class ApiMetricBackend(ApiClient api, UserSession session) : IMetricBackend
{
    public ContentMetricsResponse GetContentMetrics(long contentId) =>
        ApiTaskCall.Run(() => api.GetContentMetricsAsync(contentId, ApiTaskCall.BearerOf(session)), session);

    public PlatformMetricsResponse RecordMetric(long contentId, long platformId, long? views, long? likes, long? comments, long? shares) =>
        ApiTaskCall.Run(() => api.RecordMetricAsync(contentId, platformId,
            new RecordMetricRequest(views, likes, comments, shares), ApiTaskCall.BearerOf(session)), session);
}
