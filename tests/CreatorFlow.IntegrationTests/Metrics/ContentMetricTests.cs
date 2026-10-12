using CreatorFlow.Contracts.Metrics;
using CreatorFlow.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Metrics;

/// <summary>Logic thuần của hàng Metrics (parse ô nhập, nhãn thời gian, map từ response) — không cần UI.</summary>
[TestClass]
public sealed class ContentMetricTests
{
    [TestMethod]
    public void TryParseField_Empty_MeansKeep()
    {
        Assert.IsTrue(ContentMetric.TryParseField(null, out long? a) && a is null);
        Assert.IsTrue(ContentMetric.TryParseField("", out long? b) && b is null);
        Assert.IsTrue(ContentMetric.TryParseField("   ", out long? c) && c is null);
    }

    [TestMethod]
    public void TryParseField_NonNegativeInteger_Accepts()
    {
        Assert.IsTrue(ContentMetric.TryParseField("0", out long? a) && a == 0);
        Assert.IsTrue(ContentMetric.TryParseField(" 100 ", out long? b) && b == 100);
    }

    [TestMethod]
    [DataRow("-1")]
    [DataRow("abc")]
    [DataRow("1.5")]
    public void TryParseField_Invalid_Rejects(string text)
    {
        Assert.IsFalse(ContentMetric.TryParseField(text, out _));
    }

    [TestMethod]
    public void UpdatedLabel_NoData_ShowsPlaceholder()
    {
        var metric = new ContentMetric(100, 5, "TikTok", "PLANNED", false, null, null, null, null, null, null);

        Assert.AreEqual("Chưa có số liệu.", ContentMetric.UpdatedLabel(metric));
        Assert.IsFalse(metric.HasData);
    }

    [TestMethod]
    public void UpdatedLabel_WithData_ShowsLocalTimeAndName()
    {
        var at = new DateTime(2026, 10, 12, 7, 30, 0, DateTimeKind.Utc).ToLocalTime();
        var metric = new ContentMetric(100, 5, "TikTok", "PUBLISHED", true, 10, 1, 2, 3, at, "Manager A");

        string label = ContentMetric.UpdatedLabel(metric);

        Assert.IsTrue(label.Contains(at.ToString("dd/MM/yyyy HH:mm")));
        Assert.IsTrue(label.Contains("Manager A"));
    }

    [TestMethod]
    public void FromResponse_MapsLatestAndConvertsToLocal()
    {
        var recordedAt = new DateTimeOffset(2026, 10, 12, 7, 0, 0, TimeSpan.Zero);
        var platform = new PlatformMetricsResponse(100, 5, "TikTok", "PUBLISHED", true,
            new MetricSnapshotResponse(7, 100, 5, 2, 1, recordedAt, 3, "Manager A"), null);

        ContentMetric metric = ContentMetric.FromResponse(platform);

        Assert.AreEqual(100, metric.Views);
        Assert.AreEqual(recordedAt.LocalDateTime, metric.RecordedAtLocal);
        Assert.IsTrue(metric.HasData);
    }
}
