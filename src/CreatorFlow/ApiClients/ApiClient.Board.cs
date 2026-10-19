using CreatorFlow.Contracts.Board;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi endpoint Board của CreatorFlow.Api. Người thao tác luôn là chủ của <c>bearer</c> (JWT).
/// Dùng chung bộ gửi/đọc lỗi với nhóm Tasks (SendTasksAsync).
/// </summary>
public sealed partial class ApiClient
{
    public Task<BoardResponse> GetBoardAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<BoardResponse>(HttpMethod.Get, $"api/projects/{projectId}/board", null, bearer, token);

    public Task<BoardResponse> GetProjectContentLibraryAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<BoardResponse>(HttpMethod.Get, $"api/projects/{projectId}/contents", null, bearer, token);

    public Task<BoardResponse> GetProjectContentLibraryAsync(long projectId, string bearer, ContentLibraryQuery query, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var values = new List<KeyValuePair<string, string>>();
        Add("title", query.Title);
        Add("platform", query.Platform);
        Add("status", query.Status);
        Add("creatorId", query.CreatorId?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add("priority", query.Priority);
        Add("hasDeadline", query.HasDeadline?.ToString().ToLowerInvariant());
        Add("deadlineFrom", query.DeadlineFrom?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Add("deadlineTo", query.DeadlineTo?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Add("tag", query.Tag);
        Add("sortBy", query.SortBy);
        Add("sortDescending", query.SortDescending?.ToString().ToLowerInvariant());

        string path = $"api/projects/{projectId}/contents";
        if (values.Count > 0)
            path += "?" + string.Join("&", values.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return SendTasksAsync<BoardResponse>(HttpMethod.Get, path, null, bearer, token);

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) values.Add(new(key, value.Trim()));
        }
    }
}
