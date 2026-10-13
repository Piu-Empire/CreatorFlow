namespace CreatorFlow.Contracts.Metrics;

/// <summary>Một lần nhập số liệu. RecordedAt là timestamp cập nhật (UTC) — AC4.</summary>
public sealed record MetricSnapshotResponse(
    long MetricId, long Views, long Likes, long Comments, long Shares,
    DateTimeOffset RecordedAt, long? RecordedBy, string? RecordedByName);

/// <summary>Latest = null nghĩa là platform này chưa có số liệu (hợp lệ). History chỉ có khi ?history=true.</summary>
public sealed record PlatformMetricsResponse(
    long ContentPlatformId, long PlatformId, string PlatformName, string PublicationStatus,
    bool CanRecord, MetricSnapshotResponse? Latest, IReadOnlyList<MetricSnapshotResponse>? History);

public sealed record ContentMetricsResponse(long ContentId, IReadOnlyList<PlatformMetricsResponse> Platforms);
