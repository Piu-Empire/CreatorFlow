using CreatorFlow.Contracts.Ideas;

namespace CreatorFlow.ApiClients;

/// <summary>Gọi các endpoint Idea Bank của CreatorFlow.Api. Người thao tác luôn là chủ của <c>bearer</c> (JWT).</summary>
public sealed partial class ApiClient
{
    public Task<IdeasResponse> GetIdeasAsync(long projectId, string? search, string? status, string? tag, string bearer, CancellationToken token = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search));
        if (!string.IsNullOrWhiteSpace(status)) query.Add("status=" + Uri.EscapeDataString(status));
        if (!string.IsNullOrWhiteSpace(tag)) query.Add("tag=" + Uri.EscapeDataString(tag));
        string path = $"api/projects/{projectId}/ideas" + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));
        return SendTasksAsync<IdeasResponse>(HttpMethod.Get, path, null, bearer, token);
    }

    public Task<IdeaTagsResponse> GetIdeaTagsAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<IdeaTagsResponse>(HttpMethod.Get, $"api/projects/{projectId}/idea-tags", null, bearer, token);

    public Task<IdeaResponse> CreateIdeaAsync(long projectId, IdeaRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<IdeaResponse>(HttpMethod.Post, $"api/projects/{projectId}/ideas", request, bearer, token);

    public Task<IdeaResponse> UpdateIdeaAsync(long ideaId, IdeaRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<IdeaResponse>(HttpMethod.Put, $"api/ideas/{ideaId}", request, bearer, token);

    public Task<ConvertIdeaResponse> ConvertIdeaAsync(long ideaId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ConvertIdeaResponse>(HttpMethod.Post, $"api/ideas/{ideaId}/convert", null, bearer, token);

    public Task<DeleteIdeaResponse> DeleteIdeaAsync(long ideaId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<DeleteIdeaResponse>(HttpMethod.Delete, $"api/ideas/{ideaId}", null, bearer, token);
}
