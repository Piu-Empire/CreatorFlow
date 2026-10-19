using CreatorFlow.Api.Services.Overdue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Overdue;

/// <summary>
/// Quy tắc quá hạn (SCRUM-47): có deadline, đã qua NGÀY, và chưa hoàn thành (Published/Archived).
/// Hàm thuần nên test trực tiếp, không cần DB/HTTP.
/// </summary>
[TestClass]
public sealed class OverdueRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    [TestMethod]
    public void IsOverdue_UnfinishedPastDeadline_IsTrue() =>
        Assert.IsTrue(OverdueRules.IsOverdue(Today.AddDays(-1), "Script", Today));

    [TestMethod]
    public void IsOverdue_DeadlineToday_IsFalse() =>
        Assert.IsFalse(OverdueRules.IsOverdue(Today, "Script", Today));

    [TestMethod]
    public void IsOverdue_FutureOrMissingDeadline_IsFalse()
    {
        Assert.IsFalse(OverdueRules.IsOverdue(Today.AddDays(3), "Script", Today));
        Assert.IsFalse(OverdueRules.IsOverdue(null, "Script", Today));
    }

    [TestMethod]
    public void IsOverdue_FinishedContent_IsFalse()
    {
        Assert.IsFalse(OverdueRules.IsOverdue(Today.AddDays(-9), "Published", Today));
        Assert.IsFalse(OverdueRules.IsOverdue(Today.AddDays(-9), "Archived", Today));
    }

    [TestMethod]
    public void IsFinished_OnlyPublishedAndArchived()
    {
        Assert.IsTrue(OverdueRules.IsFinished("Published"));
        Assert.IsTrue(OverdueRules.IsFinished("Archived"));
        Assert.IsFalse(OverdueRules.IsFinished("Ready"));
        Assert.IsFalse(OverdueRules.IsFinished("Review"));
    }

    [TestMethod]
    public void DaysLate_CountsWholeDaysLate_AndZeroOtherwise()
    {
        Assert.AreEqual(9, OverdueRules.DaysLate(Today.AddDays(-9), Today));
        Assert.AreEqual(0, OverdueRules.DaysLate(Today, Today));
        Assert.AreEqual(0, OverdueRules.DaysLate(Today.AddDays(2), Today));
    }
}
