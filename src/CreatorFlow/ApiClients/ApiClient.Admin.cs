using System.Net.Http;

using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.ApiClients;

/// <summary>HTTP client cho endpoint quản trị hệ thống (chỉ System Admin gọi được).</summary>
public sealed partial class ApiClient
{
    /// <summary>Lấy danh sách người dùng kèm tổng số dòng cho màn hình admin.</summary>
    public Task<AdminUsersListResponse> GetAdminUsersAsync(string? search, string? status, int limit, int offset,
        string bearer, CancellationToken token = default)
    {
        string path = $"api/admin/users?limit={limit}&offset={offset}";
        if (!string.IsNullOrWhiteSpace(search)) path += "&search=" + Uri.EscapeDataString(search.Trim());
        if (!string.IsNullOrWhiteSpace(status)) path += "&status=" + Uri.EscapeDataString(status.Trim());
        return SendJsonAsync<AdminUsersListResponse>(HttpMethod.Get, path, body: null, bearer, token);
    }

    /// <summary>Khóa (LOCKED) hoặc mở khóa (ACTIVE) một tài khoản.</summary>
    public Task<UpdateUserStatusResponse> UpdateUserStatusAsync(long userId, string status, string bearer,
        CancellationToken token = default) =>
        SendJsonAsync<UpdateUserStatusResponse>(HttpMethod.Patch, $"api/admin/users/{userId}/status",
            new UpdateUserStatusRequest(status), bearer, token);

    // SCRUM-23: gói dịch vụ, đăng ký gói và thống kê hệ thống.

    public Task<AdminPlansListResponse> GetAdminPlansAsync(string bearer, CancellationToken token = default) =>
        SendJsonAsync<AdminPlansListResponse>(HttpMethod.Get, "api/admin/plans", body: null, bearer, token);

    public Task<AdminPlanResponse> CreateAdminPlanAsync(CreatePlanRequest request, string bearer,
        CancellationToken token = default) =>
        SendJsonAsync<AdminPlanResponse>(HttpMethod.Post, "api/admin/plans", request, bearer, token);

    public Task<AdminPlanResponse> UpdateAdminPlanAsync(long planId, UpdatePlanRequest request, string bearer,
        CancellationToken token = default) =>
        SendJsonAsync<AdminPlanResponse>(HttpMethod.Patch, $"api/admin/plans/{planId}", request, bearer, token);

    public Task<AdminSubscriptionsListResponse> GetAdminSubscriptionsAsync(string? status, long? planId,
        string? search, int limit, int offset, string bearer, CancellationToken token = default)
    {
        string path = $"api/admin/subscriptions?limit={limit}&offset={offset}";
        if (!string.IsNullOrWhiteSpace(status)) path += "&status=" + Uri.EscapeDataString(status.Trim());
        if (planId is > 0) path += "&planId=" + planId;
        if (!string.IsNullOrWhiteSpace(search)) path += "&search=" + Uri.EscapeDataString(search.Trim());
        return SendJsonAsync<AdminSubscriptionsListResponse>(HttpMethod.Get, path, body: null, bearer, token);
    }

    public Task<AdminSubscriptionResponse> CreateAdminSubscriptionAsync(CreateSubscriptionRequest request,
        string bearer, CancellationToken token = default) =>
        SendJsonAsync<AdminSubscriptionResponse>(HttpMethod.Post, "api/admin/subscriptions", request, bearer, token);

    public Task<AdminSubscriptionResponse> UpdateAdminSubscriptionAsync(long subscriptionId,
        UpdateSubscriptionRequest request, string bearer, CancellationToken token = default) =>
        SendJsonAsync<AdminSubscriptionResponse>(HttpMethod.Patch, $"api/admin/subscriptions/{subscriptionId}",
            request, bearer, token);

    public Task<AdminStatisticsResponse> GetAdminStatisticsAsync(string bearer,
        CancellationToken token = default) =>
        SendJsonAsync<AdminStatisticsResponse>(HttpMethod.Get, "api/admin/statistics", body: null, bearer, token);
}
