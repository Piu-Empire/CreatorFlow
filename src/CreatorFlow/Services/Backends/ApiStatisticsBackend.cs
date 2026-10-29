using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Statistics;

namespace CreatorFlow.Services.Backends;

/// <summary>Thống kê theo Creator/Project chạy qua CreatorFlow.Api (quyền kiểm tra ở Backend).</summary>
public sealed class ApiStatisticsBackend(ApiClient api, UserSession session) : IStatisticsBackend
{
    public CreatorStatisticsResponse GetCreatorStatistics(long projectId, long? creatorId) =>
        ApiTaskCall.Run(() => api.GetCreatorStatisticsAsync(projectId, creatorId, ApiTaskCall.BearerOf(session)), session);
}
