using CreatorFlow.Contracts.Metrics;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Đọc/ghi Metrics theo ContentPlatform khi chạy theo mô hình Client–Server:
/// quyền, Published và validation nằm ở CreatorFlow.Api, client chỉ gọi qua REST. Người thao tác luôn là người đang đăng nhập.
/// </summary>
public interface IMetricBackend
{
    ContentMetricsResponse GetContentMetrics(long contentId);

    /// <summary>Nhập một snapshot mới (field null = giữ giá trị cũ). Trả về platform kèm snapshot vừa lưu.</summary>
    PlatformMetricsResponse RecordMetric(long contentId, long platformId, long? views, long? likes, long? comments, long? shares);
}
