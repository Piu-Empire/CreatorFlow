using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// SCRUM-49 — Lọc lịch theo Project / Creator / Platform / Status.
/// Logic thuần client-side, không cần DB/UI.
/// </summary>
[TestClass]
public sealed class CalendarFilterTests
{
    private static ContentBoardCard Card(
        long contentId, long projectId, ContentStatus status, string platform, params long[] assigneeIds)
    {
        var card = new ContentBoardCard
        {
            ContentId = contentId,
            ProjectId = projectId,
            Code = $"CNT-{contentId:000}",
            Title = $"Content {contentId}",
            Status = status,
            Platforms = [platform],
        };
        foreach (long id in assigneeIds)
            card.Assignees.Add(new CardAssignee(id, $"Creator {id}", AssignmentStatus.InProgress, 0, null));
        return card;
    }

    private static readonly List<ContentBoardCard> Cards =
    [
        Card(1, 1, ContentStatus.Idea, "TikTok", 10),
        Card(2, 1, ContentStatus.Script, "YouTube", 11),
        Card(3, 1, ContentStatus.Idea, "YouTube", 10, 11),
        Card(4, 2, ContentStatus.Idea, "TikTok", 12),
    ];

    [TestMethod]
    public void ApplyFilters_AllNull_ReturnsEverything()
    {
        var result = CalendarDateLogic.ApplyFilters(Cards, null, null, null, null);
        Assert.AreEqual(4, result.Count);
    }

    [TestMethod]
    public void ApplyFilters_Platform_IsCaseInsensitive()
    {
        var result = CalendarDateLogic.ApplyFilters(Cards, "youtube", null, null, null);
        CollectionAssert.AreEquivalent(new long[] { 2, 3 }, result.Select(c => c.ContentId).ToArray());
    }

    [TestMethod]
    public void ApplyFilters_Status_ReturnsMatchingOnly()
    {
        var result = CalendarDateLogic.ApplyFilters(Cards, null, ContentStatus.Script, null, null);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(2, result[0].ContentId);
    }

    [TestMethod]
    public void ApplyFilters_Creator_ReturnsContentWithThatAssignee()
    {
        var result = CalendarDateLogic.ApplyFilters(Cards, null, null, 11, null);
        CollectionAssert.AreEquivalent(new long[] { 2, 3 }, result.Select(c => c.ContentId).ToArray());
    }

    [TestMethod]
    public void ApplyFilters_Project_ReturnsThatProjectOnly()
    {
        var result = CalendarDateLogic.ApplyFilters(Cards, null, null, null, 2);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(4, result[0].ContentId);
    }

    [TestMethod]
    public void ApplyFilters_Combined_ReturnsIntersection()
    {
        var result = CalendarDateLogic.ApplyFilters(Cards, "YouTube", ContentStatus.Idea, 11, null);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(3, result[0].ContentId);
    }

    [TestMethod]
    public void ApplyFilters_NoMatch_ReturnsEmpty()
    {
        var result = CalendarDateLogic.ApplyFilters(Cards, "Facebook", null, null, null);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ApplyFilters_Search_MatchesCodeOrTitle()
    {
        var byCode = CalendarDateLogic.ApplyFilters(Cards, null, null, null, null, "CNT-002");
        Assert.AreEqual(1, byCode.Count);
        Assert.AreEqual(2, byCode[0].ContentId);

        var byTitle = CalendarDateLogic.ApplyFilters(Cards, null, null, null, null, "content 4");
        Assert.AreEqual(1, byTitle.Count);
        Assert.AreEqual(4, byTitle[0].ContentId);
    }
}
