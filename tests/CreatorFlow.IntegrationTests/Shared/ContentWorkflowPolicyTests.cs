using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

[TestClass]
public sealed class ContentWorkflowPolicyTests
{
    [TestMethod]
    [DataRow(ContentStatus.Idea, ContentStatus.Script)]
    [DataRow(ContentStatus.Script, ContentStatus.Production)]
    [DataRow(ContentStatus.Production, ContentStatus.Editing)]
    [DataRow(ContentStatus.Editing, ContentStatus.Review)]
    [DataRow(ContentStatus.Review, ContentStatus.Editing)]
    [DataRow(ContentStatus.Review, ContentStatus.Ready)]
    [DataRow(ContentStatus.Ready, ContentStatus.Published)]
    public void CanTransition_WithApprovedTransition_ReturnsTrue(
        ContentStatus from,
        ContentStatus to)
    {
        Assert.IsTrue(ContentWorkflowPolicy.CanTransition(from, to));
    }

    [TestMethod]
    [DataRow(ContentStatus.Idea, ContentStatus.Review)]
    [DataRow(ContentStatus.Script, ContentStatus.Ready)]
    [DataRow(ContentStatus.Published, ContentStatus.Idea)]
    [DataRow(ContentStatus.Archived, ContentStatus.Editing)]
    public void CanTransition_WithUnapprovedTransition_ReturnsFalse(
        ContentStatus from,
        ContentStatus to)
    {
        Assert.IsFalse(ContentWorkflowPolicy.CanTransition(from, to));
    }

    [TestMethod]
    [DataRow(ProjectRole.Owner)]
    [DataRow(ProjectRole.Manager)]
    public void CanArchive_ForOwnerOrManager_ReturnsTrue(ProjectRole role)
    {
        Assert.IsTrue(ContentWorkflowPolicy.CanArchive(role, ContentStatus.Editing));
    }

    [TestMethod]
    public void CanArchive_ForCreator_ReturnsFalse()
    {
        Assert.IsFalse(
            ContentWorkflowPolicy.CanArchive(ProjectRole.Creator, ContentStatus.Editing));
    }

    [TestMethod]
    public void CanArchive_WhenAlreadyArchived_ReturnsFalse()
    {
        Assert.IsFalse(
            ContentWorkflowPolicy.CanArchive(ProjectRole.Owner, ContentStatus.Archived));
    }

    [TestMethod]
    [DataRow(ProjectRole.Owner)]
    [DataRow(ProjectRole.Manager)]
    public void CanRestore_ArchivedContentForOwnerOrManager_ReturnsTrue(ProjectRole role)
    {
        Assert.IsTrue(ContentWorkflowPolicy.CanRestore(role, ContentStatus.Archived));
    }

    [TestMethod]
    public void CanRestore_ForCreator_ReturnsFalse()
    {
        Assert.IsFalse(
            ContentWorkflowPolicy.CanRestore(ProjectRole.Creator, ContentStatus.Archived));
    }

    [TestMethod]
    public void CanRestore_WhenContentIsNotArchived_ReturnsFalse()
    {
        Assert.IsFalse(
            ContentWorkflowPolicy.CanRestore(ProjectRole.Owner, ContentStatus.Review));
    }

    [TestMethod]
    public void GetRestoreStatus_WithMultipleArchives_ReturnsStatusBeforeLatestArchive()
    {
        var history = new[]
        {
            new ContentStatusHistory
            {
                HistoryId = 1,
                ContentId = 10,
                FromStatus = ContentStatus.Script,
                ToStatus = ContentStatus.Archived,
                ChangedAt = new DateTime(2026, 1, 1)
            },
            new ContentStatusHistory
            {
                HistoryId = 2,
                ContentId = 10,
                FromStatus = ContentStatus.Archived,
                ToStatus = ContentStatus.Script,
                ChangedAt = new DateTime(2026, 1, 2)
            },
            new ContentStatusHistory
            {
                HistoryId = 3,
                ContentId = 10,
                FromStatus = ContentStatus.Review,
                ToStatus = ContentStatus.Archived,
                ChangedAt = new DateTime(2026, 1, 3)
            }
        };

        ContentStatus result = ContentWorkflowPolicy.GetRestoreStatus(history);

        Assert.AreEqual(ContentStatus.Review, result);
    }

    [TestMethod]
    public void GetRestoreStatus_WithoutValidArchiveHistory_Throws()
    {
        var history = new[]
        {
            new ContentStatusHistory
            {
                HistoryId = 1,
                ContentId = 10,
                FromStatus = ContentStatus.Editing,
                ToStatus = ContentStatus.Review,
                ChangedAt = new DateTime(2026, 1, 1)
            }
        };

        Assert.ThrowsExactly<InvalidOperationException>(
            () => ContentWorkflowPolicy.GetRestoreStatus(history));
    }
}
