using CreatorFlow.Api.Models.Admin;

namespace CreatorFlow.Api.Repositories.Admin;

/// <summary>Kết quả tạo báo cáo: trùng cặp đang mở hoặc id mới.</summary>
public sealed record AdminReportCreateResult(bool Duplicate, long ReportId);

/// <summary>Kết quả một lần xử lý báo cáo trong transaction.</summary>
public sealed record AdminReportHandleResult(ReportStatus PreviousStatus, string ReporterEmail, string ReportedEmail);

/// <summary>Truy vấn dữ liệu báo cáo vi phạm.</summary>
public interface IAdminReportRepository
{
    /// <summary>Tìm báo cáo theo từ khóa email và trạng thái, có phân trang.</summary>
    Task<IReadOnlyList<AdminReportRecord>> SearchAsync(string? search, string? status, int limit, int offset,
        CancellationToken cancellationToken = default);

    /// <summary>Đếm số báo cáo khớp bộ lọc (dùng chung điều kiện với SearchAsync).</summary>
    Task<int> CountAsync(string? search, string? status,
        CancellationToken cancellationToken = default);

    /// <summary>Tạo báo cáo mới trong transaction; null khi reported không tồn tại.</summary>
    Task<AdminReportCreateResult?> CreateAsync(long reporterUserId, long reportedUserId,
        string reason, string? details, CancellationToken cancellationToken = default);

    /// <summary>Đổi trạng thái xử lý kèm audit trong một transaction; null khi report không tồn tại.</summary>
    Task<AdminReportHandleResult?> HandleAsync(long reportId, ReportStatus status, string? note, long actorUserId,
        string traceId, CancellationToken cancellationToken = default);
}
