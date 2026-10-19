using CreatorFlow.Models;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-57 — Dựng prompt gửi AI từ chủ đề/nền tảng/mục tiêu.</summary>
[TestClass]
public sealed class IdeaAssistPromptBuilderTests
{
    [TestMethod]
    public void Build_IncludesTopicGoalPlatformsAndCount()
    {
        var request = new IdeaSuggestRequest
        {
            Topic = "mẹo học tiếng Anh",
            Goal = "tăng lượt theo dõi",
            Platforms = { "YouTube", "TikTok" },
        };

        string prompt = IdeaAssistPromptBuilder.Build(request);

        StringAssert.Contains(prompt, "mẹo học tiếng Anh");
        StringAssert.Contains(prompt, "tăng lượt theo dõi");
        StringAssert.Contains(prompt, "YouTube, TikTok");
        StringAssert.Contains(prompt, IdeaAssistPromptBuilder.SuggestionCount.ToString());
    }

    [TestMethod]
    public void Build_WithoutPlatforms_UsesPlaceholder()
    {
        string prompt = IdeaAssistPromptBuilder.Build(new IdeaSuggestRequest { Topic = "x", Goal = "y" });

        StringAssert.Contains(prompt, "(không chỉ định)");
    }
}
