namespace CreatorFlow.Contracts.Metrics;

/// <summary>Nhập một snapshot metrics. Field null = giữ giá trị của snapshot trước (chưa có thì 0). Phải có ít nhất một field.</summary>
public sealed record RecordMetricRequest(long? Views, long? Likes, long? Comments, long? Shares);
