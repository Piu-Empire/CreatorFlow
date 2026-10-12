namespace CreatorFlow.Contracts.Admin;

/// <summary>Tóm tắt một báo cáo vi phạm cho màn hình admin.</summary>
public sealed record AdminReportSummaryResponse(
    long ReportId,
    long ReporterUserId,
    string ReporterEmail,
    long ReportedUserId,
    string ReportedEmail,
    string Reason,
    string? Details,
    string Status,
    long? HandledBy,
    string? HandledNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? HandledAt);

/// <summary>Danh sách báo cáo kèm tổng số dòng khớp bộ lọc.</summary>
public sealed record AdminReportsListResponse(
    IReadOnlyList<AdminReportSummaryResponse> Items,
    int TotalCount);

/// <summary>User tố cáo user khác (reported lấy từ route/body id, không tin client về reporter).</summary>
public sealed record CreateReportRequest(long ReportedUserId, string? Reason, string? Details);

/// <summary>Kết quả tạo báo cáo.</summary>
public sealed record CreateReportResponse(long ReportId, string Status, string Message);

/// <summary>Admin xử lý báo cáo (chỉ IN_REVIEW / RESOLVED / REJECTED).</summary>
public sealed record HandleReportRequest(string? Status, string? Note);

/// <summary>Kết quả xử lý báo cáo.</summary>
public sealed record HandleReportResponse(long ReportId, string Status, string Message);
