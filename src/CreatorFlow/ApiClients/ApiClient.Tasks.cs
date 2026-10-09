using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreatorFlow.Contracts.Tasks;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi các endpoint My Tasks / tiến độ / deadline / giao việc của CreatorFlow.Api.
/// Người thao tác luôn là chủ của <c>bearer</c> (JWT); client không bao giờ gửi "tôi là ai" trong body.
/// </summary>
public sealed partial class ApiClient
{
    // Danh sách task/assignment có thể dài hơn giới hạn 64 KiB dùng cho các phản hồi Auth.
    private const int TaskResponseLimit = 512 * 1024;

    public Task<MyTasksResponse> GetMyTasksAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<MyTasksResponse>(HttpMethod.Get, $"api/projects/{projectId}/my-tasks", null, bearer, token);

    public Task<ProjectRoleResponse> GetMyProjectRoleAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ProjectRoleResponse>(HttpMethod.Get, $"api/projects/{projectId}/my-role", null, bearer, token);

    public Task<AssignableMembersResponse> GetAssignableMembersAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<AssignableMembersResponse>(HttpMethod.Get, $"api/projects/{projectId}/assignable-members", null, bearer, token);

    public Task<BoardAssigneesResponse> GetBoardAssigneesAsync(long projectId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<BoardAssigneesResponse>(HttpMethod.Get, $"api/projects/{projectId}/board/assignees", null, bearer, token);

    public Task<AssignmentsResponse> GetAssignmentsAsync(long contentId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<AssignmentsResponse>(HttpMethod.Get, $"api/contents/{contentId}/assignments", null, bearer, token);

    public Task<AssignmentsResponse> AssignContentAsync(long contentId, AssignContentRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<AssignmentsResponse>(HttpMethod.Put, $"api/contents/{contentId}/assignments", request, bearer, token);

    public Task<MyTaskResponse> UpdateProgressAsync(long contentId, UpdateProgressRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<MyTaskResponse>(HttpMethod.Put, $"api/contents/{contentId}/progress", request, bearer, token);

    public Task<MyTaskResponse> ChangeDeadlineAsync(long contentId, long assigneeUserId, ChangeDeadlineRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<MyTaskResponse>(HttpMethod.Put, $"api/contents/{contentId}/assignments/{assigneeUserId}/deadline", request, bearer, token);

    // Mã lỗi nghiệp vụ của nhóm endpoint này; thông điệp (title) do Backend đặt nên được giữ lại để hiển thị cho người dùng.
    private static readonly string[] TaskErrorCodes = ["validation", "unauthorized", "forbidden", "not_found", "content_locked"];

    private static ApiBusinessException CreateTaskError(HttpStatusCode status, byte[] body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("code", out var codeValue) && codeValue.ValueKind == JsonValueKind.String
                && codeValue.GetString() is { } code && TaskErrorCodes.Contains(code)
                && root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                && title.GetString() is { Length: > 0 and <= 300 } message && !message.Contains('\n'))
            {
                return new ApiBusinessException(status, code, message);
            }
        }
        catch (JsonException)
        {
            // rơi xuống cách xử lý chung bên dưới
        }
        return CreateBusinessError(status, body);
    }

    private async Task<T> SendTasksAsync<T>(HttpMethod method, string path, object? body, string bearer, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(httpClient.Timeout);
        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            byte[] data = await ReadBoundedAsync(response.Content, TaskResponseLimit, deadline.Token);
            if (!response.IsSuccessStatusCode) throw CreateTaskError(response.StatusCode, data);
            T? result = JsonSerializer.Deserialize<T>(data, JsonOptions);
            return result ?? throw new ApiException(ApiErrorKind.InvalidResponse, "API trả dữ liệu không hợp lệ.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new ApiException(ApiErrorKind.Timeout, "API phản hồi quá thời gian chờ."); }
        catch (HttpRequestException) { throw new ApiException(ApiErrorKind.Network, "Không kết nối được API."); }
        catch (JsonException) { throw new ApiException(ApiErrorKind.InvalidResponse, "API trả dữ liệu không hợp lệ."); }
    }
}
