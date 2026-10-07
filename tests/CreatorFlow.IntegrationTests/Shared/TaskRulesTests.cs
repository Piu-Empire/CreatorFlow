using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

[TestClass]
public sealed class TaskRulesTests
{
    private static readonly DateTime Today = new(2026, 10, 4);

    // ---------- Validation progress ----------

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(50)]
    [DataRow(100)]
    public void ValidateProgress_InRange_DoesNotThrow(int percent) => TaskRules.ValidateProgress(percent);

    [TestMethod]
    [DataRow(-1)]
    [DataRow(101)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void ValidateProgress_OutOfRange_Throws(int percent) =>
        Assert.ThrowsExactly<ContentValidationException>(() => TaskRules.ValidateProgress(percent));

    [TestMethod]
    [DataRow(0, AssignmentStatus.Assigned)]
    [DataRow(1, AssignmentStatus.InProgress)]
    [DataRow(99, AssignmentStatus.InProgress)]
    [DataRow(100, AssignmentStatus.Completed)]
    public void StatusForProgress_MapsPercentToStatus(int percent, AssignmentStatus expected) =>
        Assert.AreEqual(expected, TaskRules.StatusForProgress(percent));

    // ---------- Phân quyền deadline ----------

    [TestMethod]
    public void CanChangeDeadline_OnlyOwnerAndManager()
    {
        Assert.IsTrue(TaskRules.CanChangeDeadline(ProjectRole.Owner));
        Assert.IsTrue(TaskRules.CanChangeDeadline(ProjectRole.Manager));
        Assert.IsFalse(TaskRules.CanChangeDeadline(ProjectRole.Creator));
        Assert.IsFalse(TaskRules.CanChangeDeadline(null));
    }

    // ---------- Validation deadline ----------

    [TestMethod]
    public void ValidateNewDeadline_PastDate_Throws() =>
        Assert.ThrowsExactly<ContentValidationException>(
            () => TaskRules.ValidateNewDeadline(Today.AddDays(-1), Today.AddDays(3), Today));

    [TestMethod]
    public void ValidateNewDeadline_TodayOrFuture_IsValid()
    {
        TaskRules.ValidateNewDeadline(Today, null, Today);
        TaskRules.ValidateNewDeadline(Today.AddDays(30), null, Today);
    }

    [TestMethod]
    public void ValidateNewDeadline_UnchangedPastDeadline_IsValid() =>
        TaskRules.ValidateNewDeadline(Today.AddDays(-5).AddHours(9), Today.AddDays(-5), Today);

    [TestMethod]
    public void ValidateNewDeadline_Null_IsValid() =>
        TaskRules.ValidateNewDeadline(null, Today.AddDays(2), Today);

    // ---------- Content hoàn thành khi TẤT CẢ Creator hoàn thành ----------

    [TestMethod]
    public void IsContentCompleted_AllCompleted_IsTrue() =>
        Assert.IsTrue(TaskRules.IsContentCompleted(new[] { AssignmentStatus.Completed, AssignmentStatus.Completed }));

    [TestMethod]
    public void IsContentCompleted_OneStillWorking_IsFalse() =>
        Assert.IsFalse(TaskRules.IsContentCompleted(new[] { AssignmentStatus.Completed, AssignmentStatus.InProgress }));

    [TestMethod]
    public void IsContentCompleted_NobodyAssigned_IsFalse() =>
        Assert.IsFalse(TaskRules.IsContentCompleted(Array.Empty<AssignmentStatus>()));

    [TestMethod]
    public void IsContentCompleted_SingleCompletedCreator_IsTrue() =>
        Assert.IsTrue(TaskRules.IsContentCompleted(new[] { AssignmentStatus.Completed }));

    // ---------- Phát hiện overdue ----------

    [TestMethod]
    public void IsOverdue_DeadlineYesterdayAndUnfinished_IsTrue() =>
        Assert.IsTrue(TaskRules.IsOverdue(Today.AddDays(-1), isFinished: false, Today));

    [TestMethod]
    public void IsOverdue_DeadlineToday_IsFalse() =>
        Assert.IsFalse(TaskRules.IsOverdue(Today.AddHours(8), isFinished: false, Today.AddHours(20)));

    [TestMethod]
    public void IsOverdue_FinishedWork_IsFalse() =>
        Assert.IsFalse(TaskRules.IsOverdue(Today.AddDays(-10), isFinished: true, Today));

    [TestMethod]
    public void IsOverdue_NoDeadline_IsFalse() =>
        Assert.IsFalse(TaskRules.IsOverdue(null, isFinished: false, Today));

    [TestMethod]
    public void DaysLate_CountsWholeDays()
    {
        Assert.AreEqual(3, TaskRules.DaysLate(Today.AddDays(-3), Today));
        Assert.AreEqual(0, TaskRules.DaysLate(Today, Today));
        Assert.AreEqual(0, TaskRules.DaysLate(Today.AddDays(2), Today));
        Assert.AreEqual(0, TaskRules.DaysLate(null, Today));
    }
}
