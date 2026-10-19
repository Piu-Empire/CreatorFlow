using CreatorFlow.Contracts.Search;

namespace CreatorFlow.ApiClients;

public sealed partial class ApiClient
{
    public Task<GlobalSearchResponse> SearchGlobalAsync(
        string query, int limit, string bearer, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        string path = $"api/search?q={Uri.EscapeDataString(query.Trim())}&limit={Math.Clamp(limit, 1, 50)}";
        return SendTasksAsync<GlobalSearchResponse>(HttpMethod.Get, path, null, bearer, token);
    }
}
