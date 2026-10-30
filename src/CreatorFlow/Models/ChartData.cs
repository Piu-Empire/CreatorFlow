using CreatorFlow.Contracts.Metrics;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Models;

/// <summary>Số Content theo một trạng thái workflow; Percent tính trên tổng Content của Project.</summary>
public sealed record StatusBucket(ContentStatus Status, int Count, double Percent);

/// <summary>Dữ liệu biểu đồ "Phân bố trạng thái Content" (SCRUM-54).</summary>
public sealed record StatusChartData(IReadOnlyList<StatusBucket> Buckets, int Total);

/// <summary>Số Content gắn một Platform; một Content có thể nằm trong nhiều Platform.</summary>
public sealed record PlatformBucket(string Name, int Count);

/// <summary>Dữ liệu biểu đồ "Content theo Platform"; Untagged = số Content chưa gắn Platform nào.</summary>
public sealed record PlatformChartData(IReadOnlyList<PlatformBucket> Buckets, int Untagged, int Total);

/// <summary>Số Content dự kiến publish trong một tháng (từ PlannedPublishAt, thiếu thì Deadline).</summary>
public sealed record MonthBucket(DateTime Month, int Count);

/// <summary>Dữ liệu biểu đồ "Kế hoạch publish theo tháng"; NoDate = số Content không có mốc thời gian.</summary>
public sealed record TimelineChartData(IReadOnlyList<MonthBucket> Buckets, int NoDate);

/// <summary>
/// Dữ liệu biểu đồ "Hiệu suất Content &amp; Platform" (SCRUM-54):
/// TopContents giới hạn hiển thị, ContentTotal để UI ghi rõ "Top N / tổng".
/// </summary>
public sealed record PerformanceChartData(
    IReadOnlyList<MetricsSummaryRow> TopContents,
    int ContentTotal,
    IReadOnlyList<MetricsSummaryRow> Platforms);
