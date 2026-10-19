using CreatorFlow.Api.Services.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Metrics;

/// <summary>Chỉ số dẫn xuất Engagement (SCRUM-52) — hàm thuần, kiểm thử offline (AC1: Views = 0).</summary>
[TestClass]
public sealed class MetricEngagementTests
{
    [TestMethod]
    public void Percent_ZeroViews_ReturnsZero_NoDivideByZero()
    {
        Assert.AreEqual(0d, MetricEngagement.Percent(0, 10, 20, 30));
    }

    [TestMethod]
    public void Percent_NegativeViews_ReturnsZero()
    {
        Assert.AreEqual(0d, MetricEngagement.Percent(-5, 1, 1, 1));
    }

    [TestMethod]
    public void Percent_NormalValues_UsesFormula()
    {
        // (Likes + Comments + Shares) / Views × 100 = (20 + 10 + 5) / 1000 × 100 = 3.5
        Assert.AreEqual(3.5d, MetricEngagement.Percent(1000, 20, 10, 5), 0.0001);
    }

    [TestMethod]
    public void Percent_NoInteractions_WithViews_ReturnsZero()
    {
        Assert.AreEqual(0d, MetricEngagement.Percent(1000, 0, 0, 0));
    }

    [TestMethod]
    public void Percent_RoundsToTwoDecimals()
    {
        // (1) / 3 × 100 = 33.3333... → 33.33
        Assert.AreEqual(33.33d, MetricEngagement.Percent(3, 1, 0, 0), 0.0001);
    }
}
