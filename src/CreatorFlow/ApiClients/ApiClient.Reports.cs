using System.Net.Http;

using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.ApiClients;

/// <summary>HTTP client cho tố cáo và xử lý báo cáo vi phạm.</summary>
public sealed partial class ApiClient
{
    /// <summary>User đăng nhập tố cáo một user khác.</summary>
    public Task<CreateReportResponse> CreateReportAsync(long reportedUserId, string? reason, string? details,
        string bearer, CancellationToken token = default) =>
        SendJsonAsync<CreateReportResponse>(HttpMethod.Post, "api/reports",
            new CreateReportRequest(reportedUserId, reason, details), bearer, token);

    /// <summary>Admin lấy danh sách báo cáo kèm tổng số dòng.</summary>
    public Task<AdminReportsListResponse> GetAdminReportsAsync(string? search, string? status, int limit, int offset,
        string bearer, CancellationToken token = default)
    {
        string path = $"api/admin/reports?limit={limit}&offset={offset}";
        if (!string.IsNullOrWhiteSpace(search)) path += "&search=" + Uri.EscapeDataString(search.Trim());
        if (!string.IsNullOrWhiteSpace(status)) path += "&status=" + Uri.EscapeDataString(status.Trim());
        return SendJsonAsync<AdminReportsListResponse>(HttpMethod.Get, path, body: null, bearer, token);
    }

    /// <summary>Admin xử lý một báo cáo (kèm ghi chú).</summary>
    public Task<HandleReportResponse> HandleReportAsync(long reportId, string status, string? note,
        string bearer, CancellationToken token = default) =>
        SendJsonAsync<HandleReportResponse>(HttpMethod.Patch, $"api/admin/reports/{reportId}",
            new HandleReportRequest(status, note), bearer, token);
}
