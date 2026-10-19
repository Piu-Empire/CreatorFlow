using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// SCRUM-53 — Đếm 6 chỉ số Dashboard (Total / In Progress / Waiting Review / Ready / Published / Overdue).
/// Logic thuần client-side, không cần DB/UI.
/// </summary>
[TestClass]
public sealed class DashboardMetricsTests
{
    private static readonly DateTime Today = new(2026, 6, 15);

    private static ContentBoardCard Card(long contentId, ContentStatus status, DateTime? deadline = null) =>
        new()
        {
            ContentId = contentId,
            ProjectId = 1,
            Code = $"CNT-{contentId:000}",
            Title = $"Content {contentId}",
            Status = status,
            Deadline = deadline,
        };

    [TestMethod]
    public void From_CountsEachBucket()
    {
        var cards = new List<ContentBoardCard>
        {
            Card(1, ContentStatus.Production, Today.AddDays(-1)), // InProgress + Overdue
            Card(2, ContentStatus.Editing, Today),                // InProgress, đúng hạn
            Card(3, ContentStatus.Review),                        // WaitingReview
            Card(4, ContentStatus.Ready, Today.AddDays(5)),       // Ready
            Card(5, ContentStatus.Published, Today.AddDays(-9)),  // Published, không tính overdue
            Card(6, ContentStatus.Idea, Today.AddDays(-2)),       // chỉ Overdue
            Card(7, ContentStatus.Script),                        // không vào bucket nào
        };

        DashboardCounts counts = DashboardMetrics.From(cards, Today);

        Assert.AreEqual(7, counts.Total);
        Assert.AreEqual(2, counts.InProgress);
        Assert.AreEqual(1, counts.WaitingReview);
        Assert.AreEqual(1, counts.Ready);
        Assert.AreEqual(1, counts.Published);
        Assert.AreEqual(2, counts.Overdue); // thẻ 1 và 6
    }

    [TestMethod]
    public void From_EmptyProject_AllZero()
    {
        DashboardCounts counts = DashboardMetrics.From([], Today);

        Assert.AreEqual(0, counts.Total);
        Assert.AreEqual(0, counts.InProgress);
        Assert.AreEqual(0, counts.WaitingReview);
        Assert.AreEqual(0, counts.Ready);
        Assert.AreEqual(0, counts.Published);
        Assert.AreEqual(0, counts.Overdue);
    }

    [TestMethod]
    public void From_OverdueIgnoresPublishedAndTodayDeadline()
    {
        var cards = new List<ContentBoardCard>
        {
            Card(1, ContentStatus.Published, Today.AddDays(-3)), // đã đăng ⇒ không overdue
            Card(2, ContentStatus.Ready, Today),                 // đúng hôm nay ⇒ chưa overdue
            Card(3, ContentStatus.Ready, Today.AddDays(-1)),     // quá hạn ⇒ overdue
            Card(4, ContentStatus.Ready),                        // không deadline ⇒ không overdue
        };

        DashboardCounts counts = DashboardMetrics.From(cards, Today);

        Assert.AreEqual(4, counts.Total);
        Assert.AreEqual(1, counts.Overdue);
    }

    [TestMethod]
    public void From_ProjectSwitch_ReturnsIndependentNumbers()
    {
        // Cùng logic nhưng phải phản ánh đúng danh sách card của từng Project (AC1: đổi theo Current Project).
        var projectA = new List<ContentBoardCard> { Card(1, ContentStatus.Production), Card(2, ContentStatus.Review) };
        var projectB = new List<ContentBoardCard> { Card(3, ContentStatus.Published), Card(4, ContentStatus.Ready), Card(5, ContentStatus.Ready) };

        DashboardCounts a = DashboardMetrics.From(projectA, Today);
        DashboardCounts b = DashboardMetrics.From(projectB, Today);

        Assert.AreEqual(2, a.Total);
        Assert.AreEqual(1, a.InProgress);
        Assert.AreEqual(1, a.WaitingReview);
        Assert.AreEqual(3, b.Total);
        Assert.AreEqual(2, b.Ready);
        Assert.AreEqual(1, b.Published);
    }
}
