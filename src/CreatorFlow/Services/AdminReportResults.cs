using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Services;

/// <summary>Kết quả tải danh sách báo cáo cho màn hình admin.</summary>
public sealed record AdminReportsResult(
    bool Succeeded, string? ErrorMessage, AdminReportsListResponse? Response = null)
{
    public static AdminReportsResult Success(AdminReportsListResponse response) => new(true, null, response);

    public static AdminReportsResult Failure(string message) => new(false, message);
}

/// <summary>Kết quả xử lý một báo cáo từ màn hình admin.</summary>
public sealed record AdminReportHandleResult(
    bool Succeeded, string? ErrorMessage, HandleReportResponse? Response = null)
{
    public static AdminReportHandleResult Success(HandleReportResponse response) => new(true, null, response);

    public static AdminReportHandleResult Failure(string message) => new(false, message);
}
