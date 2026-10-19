namespace CreatorFlow.Contracts.Metrics;

/// <summary>
/// Một dòng tổng hợp Metrics theo một chiều (Content/Creator/Platform/Project) — SCRUM-52.
/// <see cref="EngagementPercent"/> = (Likes + Comments + Shares) / Views × 100, làm tròn 2 chữ số.
/// </summary>
public sealed record MetricsSummaryRow(
    long Key, string Name, long Views, long Likes, long Comments, long Shares, double EngagementPercent);

/// <summary>Tổng hợp Metrics của một Project theo chiều <see cref="GroupBy"/> (content/creator/platform/project).</summary>
public sealed record MetricsSummaryResponse(long ProjectId, string GroupBy, IReadOnlyList<MetricsSummaryRow> Rows);
