using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// Quá hạn trên Board phải khớp quy tắc SCRUM-47: Content chưa hoàn thành (Published/Archived) và deadline đã qua ngày.
/// </summary>
[TestClass]
public sealed class ContentBoardCardTests
{
    private static ContentBoardCard Card(ContentStatus status, DateTime? deadline) => new()
    {
        ContentId = 1,
        Code = "CNT-001",
        Title = "Content",
        Status = status,
        Deadline = deadline,
    };

    [TestMethod]
    public void IsOverdue_UnfinishedPastDeadline_IsTrue() =>
        Assert.IsTrue(Card(ContentStatus.Script, DateTime.Today.AddDays(-1)).IsOverdue);

    [TestMethod]
    public void IsOverdue_FinishedContent_IsFalse()
    {
        Assert.IsFalse(Card(ContentStatus.Published, DateTime.Today.AddDays(-5)).IsOverdue);
        Assert.IsFalse(Card(ContentStatus.Archived, DateTime.Today.AddDays(-5)).IsOverdue);
    }

    [TestMethod]
    public void IsOverdue_DeadlineTodayOrMissing_IsFalse()
    {
        Assert.IsFalse(Card(ContentStatus.Ready, DateTime.Today).IsOverdue);
        Assert.IsFalse(Card(ContentStatus.Script, null).IsOverdue);
    }
}
