namespace CreatorFlow.Api.Models.Admin;

/// <summary>Trạng thái báo cáo vi phạm (khớp enum report_status trong DB).</summary>
public enum ReportStatus
{
    Open,
    InReview,
    Resolved,
    Rejected,
}

/// <summary>Một dòng báo cáo vi phạm (nội bộ API).</summary>
public sealed class AdminReportRecord
{
    public long ReportId { get; init; }

    public long ReporterUserId { get; init; }

    public required string ReporterEmail { get; init; }

    public long ReportedUserId { get; init; }

    public required string ReportedEmail { get; init; }

    public required string Reason { get; init; }

    public string? Details { get; init; }

    public ReportStatus Status { get; init; }

    public long? HandledBy { get; init; }

    public string? HandledNote { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? HandledAt { get; init; }
}
