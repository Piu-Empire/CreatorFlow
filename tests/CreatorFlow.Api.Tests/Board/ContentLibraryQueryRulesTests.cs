using CreatorFlow.Api.Services.Board;
using CreatorFlow.Contracts.Board;
using CreatorFlow.Contracts.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Board;

[TestClass]
[TestCategory("ContentLibraryQuery")]
public sealed class ContentLibraryQueryRulesTests
{
    private static readonly BoardCardResponse[] Cards =
    [
        Card(1, "Bravo", "Ready", "Low", new DateOnly(2026, 11, 20), ["TikTok"], ["news"], (3, "Zoë")),
        Card(2, "Alpha", "Idea", "Urgent", new DateOnly(2026, 11, 10), ["YouTube"], ["weekly"], (2, "Alice")),
        Card(3, "Charlie", "Script", "Medium", null, ["Instagram"], ["education"], (1, "Mia")),
    ];

    [TestMethod]
    public void Apply_CombinesEveryFilterWithAnd()
    {
        var query = new ContentLibraryQuery
        {
            Title = "alp",
            Platform = "youtube",
            Status = "idea",
            CreatorId = 2,
            Priority = "urgent",
            HasDeadline = true,
            DeadlineFrom = new DateOnly(2026, 11, 1),
            DeadlineTo = new DateOnly(2026, 11, 30),
            Tag = "WEEKLY",
        };

        IReadOnlyList<BoardCardResponse> result = ContentLibraryQueryRules.Apply(Cards, query);

        CollectionAssert.AreEqual(new long[] { 2 }, result.Select(card => card.ContentId).ToArray());
    }

    [TestMethod]
    public void Apply_SortsAllSupportedFieldsAndKeepsMissingDeadlinesLast()
    {
        AssertSort("title", [2, 1, 3]);
        AssertSort("platform", [3, 1, 2]);
        AssertSort("status", [2, 1, 3]);
        AssertSort("creator", [2, 3, 1]);
        AssertSort("priority", [1, 3, 2]);
        AssertSort("deadline", [2, 1, 3]);
        AssertSort("tag", [3, 1, 2]);
        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, ContentLibraryQueryRules.Apply(Cards,
            new ContentLibraryQuery { SortBy = "deadline", SortDescending = true }).Select(card => card.ContentId).ToArray());
    }

    [TestMethod]
    public void TryValidate_RejectsInvalidSortDeadlineRangeAndCreator()
    {
        Assert.IsFalse(ContentLibraryQueryRules.TryValidate(new ContentLibraryQuery { SortBy = "sql" }, out string? sortField, out _));
        Assert.AreEqual(nameof(ContentLibraryQuery.SortBy), sortField);
        Assert.IsFalse(ContentLibraryQueryRules.TryValidate(new ContentLibraryQuery
        {
            DeadlineFrom = new DateOnly(2026, 11, 30),
            DeadlineTo = new DateOnly(2026, 11, 1),
        }, out string? deadlineField, out _));
        Assert.AreEqual(nameof(ContentLibraryQuery.DeadlineTo), deadlineField);
        Assert.IsFalse(ContentLibraryQueryRules.TryValidate(new ContentLibraryQuery { CreatorId = 0 }, out string? creatorField, out _));
        Assert.AreEqual(nameof(ContentLibraryQuery.CreatorId), creatorField);
        Assert.IsFalse(ContentLibraryQueryRules.TryValidate(new ContentLibraryQuery
        {
            HasDeadline = false,
            DeadlineFrom = new DateOnly(2026, 11, 1),
        }, out string? modeField, out _));
        Assert.AreEqual(nameof(ContentLibraryQuery.HasDeadline), modeField);
    }

    private static void AssertSort(string field, long[] expected) =>
        CollectionAssert.AreEqual(expected, ContentLibraryQueryRules.Apply(Cards,
            new ContentLibraryQuery { SortBy = field }).Select(card => card.ContentId).ToArray(), field);

    private static BoardCardResponse Card(
        long id,
        string title,
        string status,
        string priority,
        DateOnly? deadline,
        IReadOnlyList<string> platforms,
        IReadOnlyList<string> tags,
        (long Id, string Name) creator) =>
        new(id, 81, $"CNT-{id:000}", title, string.Empty, status, priority, deadline, null,
            platforms, tags, false,
            [new BoardAssigneeResponse(id, creator.Id, creator.Name, "InProgress", 0, null)]);
}
