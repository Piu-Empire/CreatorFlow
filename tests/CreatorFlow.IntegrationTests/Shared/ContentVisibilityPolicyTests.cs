using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

[TestClass]
public sealed class ContentVisibilityPolicyTests
{
    [TestMethod]
    public void IsVisibleOnBoard_WithArchivedContent_ReturnsFalse()
    {
        Content content = CreateContent(ContentStatus.Archived);

        Assert.IsFalse(ContentVisibilityPolicy.IsVisibleOnBoard(content));
    }

    [TestMethod]
    public void IsVisibleOnBoard_WithActiveContent_ReturnsTrue()
    {
        Content content = CreateContent(ContentStatus.Editing);

        Assert.IsTrue(ContentVisibilityPolicy.IsVisibleOnBoard(content));
    }

    [TestMethod]
    public void IsVisibleInLibrary_WithDefaultFilter_HidesArchivedContent()
    {
        Content content = CreateContent(ContentStatus.Archived);

        Assert.IsFalse(
            ContentVisibilityPolicy.IsVisibleInLibrary(content, new ContentFilter()));
    }

    [TestMethod]
    public void IsVisibleInLibrary_WithArchivedFilter_ShowsArchivedContent()
    {
        Content content = CreateContent(ContentStatus.Archived);
        var filter = new ContentFilter { Status = ContentStatus.Archived };

        Assert.IsTrue(ContentVisibilityPolicy.IsVisibleInLibrary(content, filter));
    }

    [TestMethod]
    public void IsVisibleInLibrary_WithStatusFilter_OnlyShowsMatchingStatus()
    {
        Content content = CreateContent(ContentStatus.Ready);
        var filter = new ContentFilter { Status = ContentStatus.Review };

        Assert.IsFalse(ContentVisibilityPolicy.IsVisibleInLibrary(content, filter));
    }

    private static Content CreateContent(ContentStatus status)
    {
        return new Content
        {
            Title = "Test content",
            Status = status
        };
    }
}
