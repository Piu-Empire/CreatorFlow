using CreatorFlow.Contracts.Metrics;
using CreatorFlow.Controls.Charts;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-54: dữ liệu biểu đồ phải tính từ card/Metrics thật, không có nhãn hay số liệu cứng.</summary>
[TestClass]
public sealed class ChartDataBuilderTests
{
    [TestMethod]
    public void Status_CountsAndPercents_MatchInput()
    {
        var cards = new List<ContentBoardCard>
        {
            Card(ContentStatus.Idea),
            Card(ContentStatus.Production),
            Card(ContentStatus.Production),
        };

        StatusChartData data = ChartDataBuilder.BuildStatus(cards);

        Assert.AreEqual(3, data.Total);
        StatusBucket production = data.Buckets.Single(b => b.Status == ContentStatus.Production);
        Assert.AreEqual(2, production.Count);
        Assert.AreEqual(66.67, production.Percent, 0.01);
        Assert.AreEqual(33.33, data.Buckets.Single(b => b.Status == ContentStatus.Idea).Percent, 0.01);
    }

    [TestMethod]
    public void Status_KeepsWorkflowOrder_AndZeroRows()
    {
        StatusChartData data = ChartDataBuilder.BuildStatus(new[] { Card(ContentStatus.Review) });

        List<ContentStatus> order = data.Buckets.Select(b => b.Status).ToList();
        CollectionAssert.AreEqual(
            new[]
            {
                ContentStatus.Idea, ContentStatus.Script, ContentStatus.Production, ContentStatus.Editing,
                ContentStatus.Review, ContentStatus.Ready, ContentStatus.Published,
            },
            order);
        Assert.AreEqual(0, data.Buckets.Single(b => b.Status == ContentStatus.Idea).Count);
        Assert.AreEqual(1, data.Buckets.Single(b => b.Status == ContentStatus.Review).Count);
    }

    [TestMethod]
    public void Status_Empty_ReturnsZeroTotal()
    {
        StatusChartData data = ChartDataBuilder.BuildStatus(Array.Empty<ContentBoardCard>());

        Assert.AreEqual(0, data.Total);
        Assert.IsTrue(data.Buckets.All(b => b.Count == 0 && b.Percent == 0));
    }

    [TestMethod]
    public void Platforms_GroupsCaseInsensitive_AndCountsUntagged()
    {
        var cards = new List<ContentBoardCard>
        {
            Card(ContentStatus.Published, platforms: "TikTok"),
            Card(ContentStatus.Published, platforms: new[] { "tiktok ", "YouTube" }),
            Card(ContentStatus.Published),
        };

        PlatformChartData data = ChartDataBuilder.BuildPlatforms(cards);

        Assert.AreEqual(3, data.Total);
        Assert.AreEqual(1, data.Untagged);
        Assert.AreEqual(2, data.Buckets.Count);
        Assert.AreEqual(2, data.Buckets.Single(b => b.Name.Equals("tiktok", StringComparison.OrdinalIgnoreCase)).Count);
        Assert.AreEqual(1, data.Buckets.Single(b => b.Name == "YouTube").Count);
    }

    [TestMethod]
    public void Platforms_SortsByCountThenName()
    {
        var cards = new List<ContentBoardCard>
        {
            Card(ContentStatus.Published, platforms: "YouTube"),
            Card(ContentStatus.Published, platforms: "TikTok"),
            Card(ContentStatus.Published, platforms: "TikTok"),
            Card(ContentStatus.Published, platforms: "Facebook"),
        };

        PlatformChartData data = ChartDataBuilder.BuildPlatforms(cards);

        CollectionAssert.AreEqual(new[] { "TikTok", "Facebook", "YouTube" },
            data.Buckets.Select(b => b.Name).ToList());
    }

    [TestMethod]
    public void Timeline_UsesPlannedThenDeadline_AndSortsMonths()
    {
        var cards = new List<ContentBoardCard>
        {
            Card(ContentStatus.Ready, planned: new DateTime(2026, 3, 15)),
            Card(ContentStatus.Ready, deadline: new DateTime(2026, 1, 10)),
            Card(ContentStatus.Ready, planned: new DateTime(2026, 1, 2), deadline: new DateTime(2026, 5, 1)),
            Card(ContentStatus.Ready, planned: new DateTime(2026, 2, 8)),
        };

        TimelineChartData data = ChartDataBuilder.BuildTimeline(cards);

        CollectionAssert.AreEqual(
            new[] { new DateTime(2026, 1, 1), new DateTime(2026, 2, 1), new DateTime(2026, 3, 1) },
            data.Buckets.Select(b => b.Month).ToList());
        Assert.AreEqual(2, data.Buckets[0].Count);
        Assert.AreEqual(1, data.Buckets[1].Count);
        Assert.AreEqual(1, data.Buckets[2].Count);
        Assert.AreEqual(0, data.NoDate);
    }

    [TestMethod]
    public void Timeline_CountsNoDate()
    {
        var cards = new List<ContentBoardCard>
        {
            Card(ContentStatus.Idea),
            Card(ContentStatus.Idea, planned: new DateTime(2026, 4, 1)),
        };

        TimelineChartData data = ChartDataBuilder.BuildTimeline(cards);

        Assert.AreEqual(1, data.NoDate);
        Assert.AreEqual(1, data.Buckets.Single().Count);
    }

    [TestMethod]
    public void Performance_SortsByViews_AndCapsTopEight()
    {
        var contents = Enumerable.Range(1, 10)
            .Select(i => Row(i, $"Content {i}", i * 100))
            .ToList();

        PerformanceChartData data = ChartDataBuilder.BuildPerformance(
            Summary("content", contents),
            Summary("platform", new List<MetricsSummaryRow> { Row(1, "TikTok", 500) }));

        Assert.AreEqual(10, data.ContentTotal);
        Assert.AreEqual(ChartDataBuilder.MaxBars, data.TopContents.Count);
        Assert.AreEqual("Content 10", data.TopContents[0].Name);
        Assert.AreEqual("Content 3", data.TopContents[^1].Name);
        Assert.AreEqual("TikTok", data.Platforms.Single().Name);
    }

    [TestMethod]
    public void Performance_Empty_ReturnsEmptyLists()
    {
        PerformanceChartData data = ChartDataBuilder.BuildPerformance(
            Summary("content", new List<MetricsSummaryRow>()),
            Summary("platform", new List<MetricsSummaryRow>()));

        Assert.AreEqual(0, data.ContentTotal);
        Assert.IsEmpty(data.TopContents);
        Assert.IsEmpty(data.Platforms);
    }

    [TestMethod]
    [DataRow(0L, 1L)]
    [DataRow(1L, 1L)]
    [DataRow(87L, 100L)]
    [DataRow(1200L, 2000L)]
    [DataRow(4500L, 5000L)]
    [DataRow(120_000L, 200_000L)]
    public void NiceCeiling_RoundsTo125(long input, long expected)
    {
        Assert.AreEqual(expected, ChartDataBuilder.NiceCeiling(input));
    }

    [TestMethod]
    public void Builder_NullInput_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ChartDataBuilder.BuildStatus(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ChartDataBuilder.BuildPlatforms(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ChartDataBuilder.BuildTimeline(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            ChartDataBuilder.BuildPerformance(null!, Summary("platform", new List<MetricsSummaryRow>())));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            ChartDataBuilder.BuildPerformance(Summary("content", new List<MetricsSummaryRow>()), null!));
    }

    private static ContentBoardCard Card(
        ContentStatus status, DateTime? planned = null, DateTime? deadline = null, params string[] platforms) =>
        new()
        {
            Status = status,
            Platforms = platforms.ToList(),
            PlannedPublishAt = planned,
            Deadline = deadline,
        };

    private static MetricsSummaryRow Row(long key, string name, long views) =>
        new(key, name, views, 10, 5, 2, 1.5);

    private static MetricsSummaryResponse Summary(string groupBy, IReadOnlyList<MetricsSummaryRow> rows) =>
        new(1, groupBy, rows);
}
