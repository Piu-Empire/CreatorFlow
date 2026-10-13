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
}
