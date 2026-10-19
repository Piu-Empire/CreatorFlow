namespace CreatorFlow.Api.Services.Metrics;

/// <summary>
/// Chỉ số dẫn xuất Engagement (SCRUM-52). Hàm thuần, kiểm thử offline.
/// Công thức chốt của dự án: <c>(Likes + Comments + Shares) / Views × 100</c>.
/// </summary>
public static class MetricEngagement
{
    /// <summary>
    /// Engagement phần trăm, làm tròn 2 chữ số. <paramref name="views"/> &lt;= 0 trả về 0 (không chia cho 0 — AC1).
    /// </summary>
    public static double Percent(long views, long likes, long comments, long shares)
    {
        if (views <= 0)
            return 0d;

        double interactions = likes + comments + shares;
        return Math.Round(interactions / views * 100d, 2, MidpointRounding.AwayFromZero);
    }
}
