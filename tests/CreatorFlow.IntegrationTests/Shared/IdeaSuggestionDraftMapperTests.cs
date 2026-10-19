using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-57 — Chuyển gợi ý (đã chỉnh) thành IdeaDraft để lưu.</summary>
[TestClass]
public sealed class IdeaSuggestionDraftMapperTests
{
    private static IdeaSuggestRequest Request() => new()
    {
        Topic = "mẹo học tiếng Anh",
        Goal = "tăng lượt theo dõi",
        Platforms = { "YouTube", "TikTok", "YouTube" },
    };

    [TestMethod]
    public void ToDraft_TrimsAndDefaultsStatusBacklog()
    {
        var draft = IdeaSuggestionDraftMapper.ToDraft(Request(), "  Tiêu đề  ", "  Mô tả  ");

        Assert.AreEqual("Tiêu đề", draft.Title);
        Assert.AreEqual("Mô tả", draft.Description);
        Assert.AreEqual(IdeaStatus.Backlog, draft.Status);
    }

    [TestMethod]
    public void ToDraft_NoteContainsTopicGoalPlatforms()
    {
        var draft = IdeaSuggestionDraftMapper.ToDraft(Request(), "T", "D");

        StringAssert.Contains(draft.Note, "mẹo học tiếng Anh");
        StringAssert.Contains(draft.Note, "tăng lượt theo dõi");
        StringAssert.Contains(draft.Note, "YouTube");
        StringAssert.Contains(draft.Note, "Trợ lý AI");
    }

    [TestMethod]
    public void ToDraft_TagsDefaultToDistinctPlatforms()
    {
        var draft = IdeaSuggestionDraftMapper.ToDraft(Request(), "T", "D");

        CollectionAssert.AreEquivalent(new[] { "YouTube", "TikTok" }, draft.Tags);
    }

    [TestMethod]
    public void ToDraft_ExplicitTagsOverridePlatforms()
    {
        var draft = IdeaSuggestionDraftMapper.ToDraft(Request(), "T", "D", new[] { "a", "b" });

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, draft.Tags);
    }
}
