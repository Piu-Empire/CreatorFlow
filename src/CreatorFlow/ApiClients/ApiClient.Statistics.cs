using CreatorFlow.Contracts.Statistics;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi endpoint thống kê theo Creator và Project (SCRUM-55). Người xem luôn là chủ của <c>bearer</c> (JWT).
/// </summary>
public sealed partial class ApiClient
{
    /// <summary>Thống kê của đúng Project trong URL; <paramref name="creatorId"/> null = mọi Creator của Project.</summary>
    public Task<CreatorStatisticsResponse> GetCreatorStatisticsAsync(
        long projectId, long? creatorId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<CreatorStatisticsResponse>(HttpMethod.Get,
            $"api/projects/{projectId}/statistics/creators"
            + (creatorId is { } id ? $"?creatorId={id}" : string.Empty),
            null, bearer, token);
}
