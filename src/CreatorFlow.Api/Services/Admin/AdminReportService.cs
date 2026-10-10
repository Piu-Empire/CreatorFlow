using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Repositories.Admin;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Api.Services.Admin;

/// <summary>Nghiệp vụ tố cáo và xử lý báo cáo vi phạm.</summary>
public sealed class AdminReportService(
    IAdminReportRepository repository,
    CurrentAuthenticatedUser currentUser,
    ILogger<AdminReportService> logger)
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;
    private const int MaxOffset = 10_000;
    private const int MaxSearchLength = 100;
    private const int MaxReasonLength = 500;
    private const int MaxDetailsLength = 2000;
    private const int MaxNoteLength = 1000;

    /// <summary>User đăng nhập tố cáo một user khác.</summary>
    public async Task<AuthOperationResult<CreateReportResponse>> CreateReportAsync(CreateReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.User is null)
            return AuthOperationResult<CreateReportResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
        long reporterId = currentUser.User.UserId;
        if (request.ReportedUserId == reporterId)
            return AuthOperationResult<CreateReportResponse>.Fail("self_report", "Không thể tố cáo chính mình.", status: 400);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > MaxReasonLength)
            return AuthOperationResult<CreateReportResponse>.Fail("validation", "Lý do tố cáo bắt buộc, tối đa 500 ký tự.", "Reason");
        if (request.Details is not null && request.Details.Trim().Length > MaxDetailsLength)
            return AuthOperationResult<CreateReportResponse>.Fail("validation", "Chi tiết tối đa 2000 ký tự.", "Details");

        try
        {
            AdminReportCreateResult? created = await repository.CreateAsync(reporterId, request.ReportedUserId,
                request.Reason.Trim(), string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(), cancellationToken);
            if (created is null)
                return AuthOperationResult<CreateReportResponse>.Fail("not_found", "Không tìm thấy tài khoản bị tố cáo.", status: 404);
            if (created.Duplicate)
                return AuthOperationResult<CreateReportResponse>.Fail("duplicate_report", "Báo cáo về tài khoản này đang chờ xử lý.", status: 409);
            return new(new CreateReportResponse(created.ReportId, "OPEN", "Đã gửi báo cáo. Quản trị viên sẽ xem xét."));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<CreateReportResponse>.Fail("service_unavailable", "Không thể gửi báo cáo lúc này.", status: 503);
        }
    }

    /// <summary>Admin xem danh sách báo cáo, chỉ dành cho System Admin.</summary>
    public async Task<AuthOperationResult<AdminReportsListResponse>> GetReportsAsync(string? search, string? status,
        int? limit, int? offset, CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminReportsListResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);
        if (search is not null && search.Trim().Length > MaxSearchLength)
            return AuthOperationResult<AdminReportsListResponse>.Fail("validation", "Từ khóa tìm kiếm không được vượt quá 100 ký tự.", "Search");
        string? normalized = string.IsNullOrWhiteSpace(status) || string.Equals(status!.Trim(), "ALL", StringComparison.OrdinalIgnoreCase)
            ? null : status.Trim().ToUpperInvariant();
        if (normalized is not (null or "OPEN" or "IN_REVIEW" or "RESOLVED" or "REJECTED"))
            return AuthOperationResult<AdminReportsListResponse>.Fail("validation", "Trạng thái lọc không hợp lệ.", "Status");
        int take = limit ?? DefaultLimit;
        if (limit is < 1)
            return AuthOperationResult<AdminReportsListResponse>.Fail("validation", "Limit phải lớn hơn 0.", "Limit");
        if (take > MaxLimit)
            take = MaxLimit;
        int skip = offset ?? 0;
        if (skip is < 0 or > MaxOffset)
            return AuthOperationResult<AdminReportsListResponse>.Fail("validation", "Offset không hợp lệ.", "Offset");

        try
        {
            string? keyword = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            IReadOnlyList<AdminReportRecord> items =
                await repository.SearchAsync(keyword, normalized, take, skip, cancellationToken);
            int total = await repository.CountAsync(keyword, normalized, cancellationToken);
            return new(new AdminReportsListResponse([.. items.Select(ToSummary)], total));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminReportsListResponse>.Fail("service_unavailable", "Không thể tải danh sách báo cáo lúc này.", status: 503);
        }
    }

    /// <summary>Admin xử lý báo cáo, chỉ dành cho System Admin.</summary>
    public async Task<AuthOperationResult<HandleReportResponse>> HandleReportAsync(long reportId,
        HandleReportRequest request, string traceId, CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<HandleReportResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);
        ReportStatus? newStatus = request.Status?.Trim().ToUpperInvariant() switch
        {
            "IN_REVIEW" => ReportStatus.InReview,
            "RESOLVED" => ReportStatus.Resolved,
            "REJECTED" => ReportStatus.Rejected,
            _ => null,
        };
        if (newStatus is null)
            return AuthOperationResult<HandleReportResponse>.Fail("validation", "Trạng thái không hợp lệ. Chỉ nhận IN_REVIEW, RESOLVED hoặc REJECTED.", "Status");
        if (request.Note is not null && request.Note.Trim().Length > MaxNoteLength)
            return AuthOperationResult<HandleReportResponse>.Fail("validation", "Ghi chú xử lý tối đa 1000 ký tự.", "Note");

        try
        {
            AdminReportHandleResult? handled = await repository.HandleAsync(reportId, newStatus.Value,
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(), currentUser.User.UserId,
                traceId, cancellationToken);
            if (handled is null)
                return AuthOperationResult<HandleReportResponse>.Fail("not_found", "Không tìm thấy báo cáo.", status: 404);
            logger.LogInformation("Report handled. TraceId: {TraceId}; ReportId: {ReportId}; Status: {Status}",
                traceId, reportId, AdminReportRepository.ToDbText(newStatus.Value));
            string message = newStatus switch
            {
                ReportStatus.InReview => "Đã tiếp nhận báo cáo để xem xét.",
                ReportStatus.Resolved => "Đã xử lý xong báo cáo.",
                _ => "Đã bác bỏ báo cáo.",
            };
            return new(new HandleReportResponse(reportId, AdminReportRepository.ToDbText(newStatus.Value), message));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<HandleReportResponse>.Fail("service_unavailable", "Không thể xử lý báo cáo lúc này.", status: 503);
        }
    }

    private static AdminReportSummaryResponse ToSummary(AdminReportRecord record) => new(
        record.ReportId,
        record.ReporterUserId,
        record.ReporterEmail,
        record.ReportedUserId,
        record.ReportedEmail,
        record.Reason,
        record.Details,
        AdminReportRepository.ToDbText(record.Status),
        record.HandledBy,
        record.HandledNote,
        record.CreatedAt,
        record.HandledAt);
}
