namespace CreatorFlow.Api.Models.Metrics;

public sealed record PlatformTarget(
    long ContentPlatformId, long ContentId, long ProjectId, long PlatformId,
    string PlatformName, string PublicationStatus, string ContentStatus);

public sealed record MetricSnapshot(
    long MetricId, long ContentPlatformId, long Views, long Likes, long Comments, long Shares,
    long? RecordedBy, string? RecordedByName, DateTimeOffset RecordedAt);

public sealed record NewMetric(long ContentPlatformId, long? Views, long? Likes, long? Comments, long? Shares, long RecordedBy);
