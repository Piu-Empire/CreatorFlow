namespace CreatorFlow.Api.Models.Metrics;

public sealed record PlatformTarget(
    long ContentPlatformId, long ContentId, long ProjectId, long PlatformId,
    string PlatformName, string PublicationStatus, string ContentStatus);

public sealed record MetricSnapshot(
    long MetricId, long ContentPlatformId, long Views, long Likes, long Comments, long Shares,
    long? RecordedBy, string? RecordedByName, DateTimeOffset RecordedAt);

public sealed record NewMetric(long ContentPlatformId, long? Views, long? Likes, long? Comments, long? Shares, long RecordedBy);

/// <summary>Chiều tổng hợp Metrics của một Project (SCRUM-52).</summary>
public enum MetricGroupBy
{
    Project,
    Content,
    Creator,
    Platform,
}

/// <summary>Một dòng tổng hợp thô từ SQL (chưa tính Engagement). Key/Name tùy chiều tổng hợp.</summary>
public sealed record MetricSummaryRecord(long Key, string Name, long Views, long Likes, long Comments, long Shares);
