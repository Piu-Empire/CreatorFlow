using CreatorFlow.Contracts.ContentDetail;
using CreatorFlow.Contracts.Workflow;

namespace CreatorFlow.ApiClients;

/// <summary>
/// Gọi các endpoint Content Detail (chi tiết, sửa thông tin, script, activity, danh sách platform)
/// và Workflow trạng thái (chuyển bước, gửi duyệt, duyệt/từ chối) của CreatorFlow.Api.
/// Người thao tác luôn là chủ của <c>bearer</c> (JWT); client không bao giờ gửi "tôi là ai" trong body.
/// </summary>
public sealed partial class ApiClient
{
    public Task<PlatformOptionsResponse> GetPlatformOptionsAsync(string bearer, CancellationToken token = default) =>
        SendTasksAsync<PlatformOptionsResponse>(HttpMethod.Get, "api/platforms", null, bearer, token);

    public Task<ContentDetailResponse> CreateContentAsync(long projectId, CreateContentRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentDetailResponse>(HttpMethod.Post, $"api/projects/{projectId}/contents", request, bearer, token);

    public Task<ContentDetailResponse> GetContentDetailAsync(long contentId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentDetailResponse>(HttpMethod.Get, $"api/contents/{contentId}", null, bearer, token);

    public Task<ContentDetailResponse> UpdateContentAsync(long contentId, UpdateContentRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentDetailResponse>(HttpMethod.Put, $"api/contents/{contentId}", request, bearer, token);

    public Task<ScriptResponse> GetScriptAsync(long contentId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ScriptResponse>(HttpMethod.Get, $"api/contents/{contentId}/script", null, bearer, token);

    public Task<ScriptResponse> SaveScriptAsync(long contentId, SaveScriptRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ScriptResponse>(HttpMethod.Put, $"api/contents/{contentId}/script", request, bearer, token);

    public Task<ActivityResponse> GetActivityAsync(long contentId, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ActivityResponse>(HttpMethod.Get, $"api/contents/{contentId}/activity", null, bearer, token);

    public Task<ContentStatusResponse> ChangeContentStatusAsync(long contentId, ChangeContentStatusRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentStatusResponse>(HttpMethod.Put, $"api/contents/{contentId}/status", request, bearer, token);

    public Task<ContentStatusResponse> SubmitReviewAsync(long contentId, SubmitReviewRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentStatusResponse>(HttpMethod.Post, $"api/contents/{contentId}/review", request, bearer, token);

    public Task<ContentStatusResponse> DecideReviewAsync(long contentId, ReviewDecisionRequest request, string bearer, CancellationToken token = default) =>
        SendTasksAsync<ContentStatusResponse>(HttpMethod.Put, $"api/contents/{contentId}/review/decision", request, bearer, token);
}
