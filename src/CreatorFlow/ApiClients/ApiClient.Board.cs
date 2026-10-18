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
}
