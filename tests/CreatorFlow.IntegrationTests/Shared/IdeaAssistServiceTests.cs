using CreatorFlow.AI;
using CreatorFlow.AI.Models;
using CreatorFlow.Models;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-57 — Nghiệp vụ gọi AI: thành công, lỗi, và chặn input thiếu.</summary>
[TestClass]
public sealed class IdeaAssistServiceTests
{
    private static IdeaSuggestRequest ValidRequest() => new()
    {
        Topic = "mẹo học tiếng Anh",
        Goal = "tăng lượt theo dõi",
    };

    [TestMethod]
    public async Task SuggestAsync_ReturnsParsedSuggestions()
    {
        var ai = new FakeAiService(new AiResult
        {
            Success = true,
            Content = "Ý tưởng 1 | Mô tả 1\nÝ tưởng 2 | Mô tả 2",
        });
        var service = new IdeaAssistService(ai);

        var result = await service.SuggestAsync(ValidRequest());

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(2, result.Suggestions.Count);
        Assert.AreEqual("Ý tưởng 1", result.Suggestions[0].Title);
        Assert.AreEqual(1, ai.Calls);
    }

    [TestMethod]
    public async Task SuggestAsync_AiFailure_ReturnsError()
    {
        var ai = new FakeAiService(new AiResult { Success = false, ErrorMessage = "API key không hợp lệ." });
        var service = new IdeaAssistService(ai);

        var result = await service.SuggestAsync(ValidRequest());

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("API key không hợp lệ.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SuggestAsync_EmptyTopic_DoesNotCallAi()
    {
        var ai = new FakeAiService(new AiResult { Success = true, Content = "x | y" });
        var service = new IdeaAssistService(ai);

        var result = await service.SuggestAsync(new IdeaSuggestRequest { Topic = "", Goal = "y" });

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(0, ai.Calls);
    }

    [TestMethod]
    public async Task SuggestAsync_NoAiConfigured_ReturnsError()
    {
        var service = new IdeaAssistService(null);

        var result = await service.SuggestAsync(ValidRequest());

        Assert.IsFalse(result.Succeeded);
    }

    private sealed class FakeAiService : IAIService
    {
        private readonly AiResult _result;

        public FakeAiService(AiResult result) => _result = result;

        public int Calls { get; private set; }

        public Task<AiResult> AskAsync(string prompt, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_result);
        }
    }
}
