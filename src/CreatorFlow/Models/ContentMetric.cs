using CreatorFlow.Contracts.Metrics;

namespace CreatorFlow.Models;

/// <summary>
/// Một hàng Metrics của một platform trong tab Metrics của Content Detail.
/// Không lưu trong model Content (AC3): đọc/ghi qua <c>IMetricBackend</c>, timestamp lấy từ snapshot mới nhất.
/// </summary>
public sealed record ContentMetric(
    long ContentPlatformId,
    long PlatformId,
    string PlatformName,
    string PublicationStatus,
    bool CanRecord,
    long? Views,
    long? Likes,
    long? Comments,
    long? Shares,
    DateTime? RecordedAtLocal,
    string? RecordedByName)
{
    /// <summary>Platform chưa có số liệu (latest = null) vẫn hợp lệ: các ô để trống, nhãn "Chưa có số liệu".</summary>
    public bool HasData => Views.HasValue;

    public static ContentMetric FromResponse(PlatformMetricsResponse platform) => new(
        platform.ContentPlatformId, platform.PlatformId, platform.PlatformName, platform.PublicationStatus,
        platform.CanRecord, platform.Latest?.Views, platform.Latest?.Likes, platform.Latest?.Comments, platform.Latest?.Shares,
        platform.Latest?.RecordedAt.LocalDateTime, platform.Latest?.RecordedByName);

    public static string UpdatedLabel(ContentMetric metric)
    {
        if (!metric.HasData || metric.RecordedAtLocal is not { } at)
            return "Chưa có số liệu.";
        string time = at.ToString("dd/MM/yyyy HH:mm");
        return string.IsNullOrWhiteSpace(metric.RecordedByName)
            ? $"Cập nhật lần cuối: {time}."
            : $"Cập nhật lần cuối: {time} bởi {metric.RecordedByName}.";
    }

    /// <summary>Ô nhập: trống = giữ nguyên (null); số nguyên ≥ 0 mới hợp lệ.</summary>
    public static bool TryParseField(string? text, out long? value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }
        if (long.TryParse(text.Trim(), out long parsed) && parsed >= 0)
        {
            value = parsed;
            return true;
        }
        value = null;
        return false;
    }
}
