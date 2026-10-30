using CreatorFlow.Contracts.Metrics;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Controls.Charts;

/// <summary>
/// SCRUM-54: gom dữ liệu thật của Project thành dữ liệu biểu đồ. Hàm thuần, không đọc DB/đồng hồ,
/// để unit test được và để control không phải tự đếm (tránh hard-code số liệu).
/// </summary>
public static class ChartDataBuilder
{
    /// <summary>Giới hạn số dòng Top Content hiển thị (giới hạn trình bày, không phải dữ liệu).</summary>
    public const int MaxBars = 8;

    /// <summary>
    /// Đếm Content theo trạng thái workflow theo đúng thứ tự enum; giữ cả trạng thái có 0 Content.
    /// Bỏ Archived vì nguồn Board đã loại Content Archived.
    /// </summary>
    public static StatusChartData BuildStatus(IEnumerable<ContentBoardCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var counts = new Dictionary<ContentStatus, int>();
        int total = 0;
        foreach (ContentBoardCard card in cards)
        {
            if (card.Status == ContentStatus.Archived) continue;
            total++;
            counts[card.Status] = counts.GetValueOrDefault(card.Status) + 1;
        }

        List<StatusBucket> buckets = Enum.GetValues<ContentStatus>()
            .Where(status => status != ContentStatus.Archived)
            .Select(status =>
            {
                int count = counts.GetValueOrDefault(status);
                double percent = total == 0 ? 0 : count * 100d / total;
                return new StatusBucket(status, count, percent);
            })
            .ToList();

        return new StatusChartData(buckets, total);
    }

    /// <summary>
    /// Đếm Content theo Platform. Tên Platform lấy từ dữ liệu (trim, gộp không phân biệt hoa thường);
    /// Content không gắn Platform nào vào nhóm Untagged.
    /// </summary>
    public static PlatformChartData BuildPlatforms(IEnumerable<ContentBoardCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int untagged = 0;
        int total = 0;

        foreach (ContentBoardCard card in cards)
        {
            if (card.Status == ContentStatus.Archived) continue;
            total++;

            List<string> platforms = card.Platforms
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (platforms.Count == 0)
            {
                untagged++;
                continue;
            }

            foreach (string platform in platforms)
            {
                names.TryAdd(platform, platform);
                counts[platform] = counts.GetValueOrDefault(platform) + 1;
            }
        }

        List<PlatformBucket> buckets = counts
            .Select(pair => new PlatformBucket(names[pair.Key], pair.Value))
            .OrderByDescending(bucket => bucket.Count)
            .ThenBy(bucket => bucket.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new PlatformChartData(buckets, untagged, total);
    }

    /// <summary>
    /// Gom Content theo tháng của mốc dự kiến publish (PlannedPublishAt, thiếu thì Deadline).
    /// Chỉ sinh tháng có dữ liệu; mốc tháng lấy từ chính dữ liệu, không dùng danh sách tháng cố định.
    /// </summary>
    public static TimelineChartData BuildTimeline(IEnumerable<ContentBoardCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var counts = new Dictionary<DateTime, int>();
        int noDate = 0;

        foreach (ContentBoardCard card in cards)
        {
            if (card.Status == ContentStatus.Archived) continue;

            DateTime? moment = card.PlannedPublishAt ?? card.Deadline;
            if (moment is null)
            {
                noDate++;
                continue;
            }

            var month = new DateTime(moment.Value.Year, moment.Value.Month, 1);
            counts[month] = counts.GetValueOrDefault(month) + 1;
        }

        List<MonthBucket> buckets = counts
            .Select(pair => new MonthBucket(pair.Key, pair.Value))
            .OrderBy(bucket => bucket.Month)
            .ToList();

        return new TimelineChartData(buckets, noDate);
    }

    /// <summary>Top Content theo Views (hoà thì theo tên) và toàn bộ Platform theo Views, từ Metrics đã lưu.</summary>
    public static PerformanceChartData BuildPerformance(
        MetricsSummaryResponse contentSummary,
        MetricsSummaryResponse platformSummary)
    {
        ArgumentNullException.ThrowIfNull(contentSummary);
        ArgumentNullException.ThrowIfNull(platformSummary);

        List<MetricsSummaryRow> topContents = contentSummary.Rows
            .OrderByDescending(row => row.Views)
            .ThenBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxBars)
            .ToList();

        List<MetricsSummaryRow> platforms = platformSummary.Rows
            .OrderByDescending(row => row.Views)
            .ThenBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new PerformanceChartData(topContents, contentSummary.Rows.Count, platforms);
    }

    /// <summary>Mốc tròn "đẹp" ≥ giá trị lớn nhất thực tế (1/2/5 × 10ⁿ) để thang đo không bị cố định.</summary>
    public static long NiceCeiling(long maxValue)
    {
        if (maxValue <= 1) return 1;

        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(maxValue)));
        double normalized = maxValue / magnitude;
        double nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return (long)(nice * magnitude);
    }
}
