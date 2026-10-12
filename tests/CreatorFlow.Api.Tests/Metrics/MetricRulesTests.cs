using CreatorFlow.Api.Models.Metrics;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Services.Metrics;
using CreatorFlow.Contracts.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Metrics;

/// <summary>Quy tắc Metrics thuần túy, không cần DB.</summary>
[TestClass]
public sealed class MetricRulesTests
{
    [TestMethod]
    public void Validate_AllNull_ReturnsError()
    {
        Assert.IsNotNull(MetricRules.Validate(new RecordMetricRequest(null, null, null, null)));
    }

    [TestMethod]
    [DataRow(-1, 0, 0, 0)]
    [DataRow(0, 0, -5, 0)]
    public void Validate_Negative_ReturnsError(long views, long likes, long comments, long shares)
    {
        Assert.IsNotNull(MetricRules.Validate(new RecordMetricRequest(views, likes, comments, shares)));
    }

    [TestMethod]
    public void Validate_ZeroAndPartial_ReturnsNull()
    {
        Assert.IsNull(MetricRules.Validate(new RecordMetricRequest(0, 0, 0, 0)));
        Assert.IsNull(MetricRules.Validate(new RecordMetricRequest(100, null, null, null)));
    }

    [TestMethod]
    public void CanRecord_OwnerAndManager_True_CreatorAndNull_False()
    {
        Assert.IsTrue(MetricRules.CanRecord(ProjectRole.Owner));
        Assert.IsTrue(MetricRules.CanRecord(ProjectRole.Manager));
        Assert.IsFalse(MetricRules.CanRecord(ProjectRole.Creator));
        Assert.IsFalse(MetricRules.CanRecord(null));
    }

    [TestMethod]
    public void IsEligible_PlatformPublished_True()
    {
        var target = new PlatformTarget(1, 10, 1, 5, "TikTok", "PUBLISHED", "READY");
        Assert.IsTrue(MetricRules.IsEligible(target));
    }

    [TestMethod]
    public void IsEligible_ContentPublished_True()
    {
        var target = new PlatformTarget(1, 10, 1, 5, "TikTok", "PLANNED", "PUBLISHED");
        Assert.IsTrue(MetricRules.IsEligible(target));
    }

    [TestMethod]
    public void IsEligible_PlannedAndDraft_False()
    {
        var target = new PlatformTarget(1, 10, 1, 5, "TikTok", "PLANNED", "READY");
        Assert.IsFalse(MetricRules.IsEligible(target));
    }
}
