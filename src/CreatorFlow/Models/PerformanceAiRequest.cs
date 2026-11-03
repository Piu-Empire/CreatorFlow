namespace CreatorFlow.Models;

/// <summary>Phạm vi số liệu được đưa cho AI phân tích (SCRUM-60).</summary>
public enum PerformanceScope
{
    /// <summary>View "Hiệu suất": metrics tổng hợp theo một chiều (Content/Creator/Platform/Project).</summary>
    Metrics,

    /// <summary>View "Creator &amp; Project": thống kê số Content và hiệu suất theo Creator.</summary>
    Creator,
}

/// <summary>Một dòng metrics đã tổng hợp, chuẩn hoá để dựng prompt (SCRUM-60).</summary>
public sealed record PerformanceMetricLine(
    string Name, long Views, long Likes, long Comments, long Shares, double EngagementPercent);

/// <summary>Một dòng thống kê Creator, chuẩn hoá để dựng prompt (SCRUM-60).</summary>
public sealed record PerformanceCreatorLine(
    string Name, int Total, int InProgress, int Published, int Overdue,
    long Views, long Likes, long Comments, long Shares, double EngagementPercent);

/// <summary>
/// Dữ liệu gửi AI phân tích hiệu suất (SCRUM-60): chỉ chứa số liệu hệ thống đã lưu,
/// không chứa nội dung Content. Prompt được dựng từ request, request không chứa prompt dựng sẵn.
/// </summary>
public sealed record PerformanceAiRequest
{
    public long ProjectId { get; init; }

    public required string ProjectName { get; init; }

    public PerformanceScope Scope { get; init; }

    /// <summary>Mô tả phạm vi hiển thị cho người dùng, ví dụ "Theo Creator" hoặc "Creator: Minh".</summary>
    public required string ScopeLabel { get; init; }

    /// <summary>Chiều tổng hợp khi <see cref="Scope"/> = Metrics (content/creator/platform/project).</summary>
    public string? GroupBy { get; init; }

    /// <summary>Thời điểm số liệu được tải từ hệ thống; dùng để AI biết dữ liệu không phải realtime.</summary>
    public DateTime LoadedAt { get; init; }

    public IReadOnlyList<PerformanceMetricLine> Metrics { get; init; } = Array.Empty<PerformanceMetricLine>();

    public IReadOnlyList<PerformanceCreatorLine> Creators { get; init; } = Array.Empty<PerformanceCreatorLine>();

    /// <summary>Dòng tổng của Project khi phân tích theo Creator (mốc so sánh); null khi không có.</summary>
    public PerformanceCreatorLine? ProjectTotals { get; init; }

    /// <summary>true nếu danh sách đã bị cắt bớt trước khi gửi AI.</summary>
    public bool IsTruncated { get; init; }
}
