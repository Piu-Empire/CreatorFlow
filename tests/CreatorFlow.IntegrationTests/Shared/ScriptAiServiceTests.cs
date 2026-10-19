using CreatorFlow.AI;
using CreatorFlow.AI.Models;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-58: ScriptAiService nối cổng script với lõi AI client và map kết quả/lỗi đúng.</summary>
[TestClass]
public sealed class ScriptAiServiceTests
{
    [TestMethod]
    public async Task Request_NoClient_ReturnsNotConfigured()
    {
        var service = new ScriptAiService(null);

        var response = await service.RequestScriptAssistAsync(NewRequest());

        Assert.IsFalse(service.IsConfigured);
        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual(NotConfiguredAiService.Message, response.ErrorMessage);
    }

    [TestMethod]
    public async Task Request_ClientSucceeds_BuildsPromptAndReturnsText()
    {
        var client = new FakeAiClient(new AiResult { Success = true, Content = "  Nội dung AI  " });
        var service = new ScriptAiService(client);

        var response = await service.RequestScriptAssistAsync(NewRequest());

        Assert.IsTrue(service.IsConfigured);
        Assert.IsTrue(response.Succeeded);
        Assert.AreEqual("Nội dung AI", response.Text);
        Assert.IsNotNull(client.LastPrompt);
        StringAssert.Contains(client.LastPrompt, "Tiêu đề: Video test");
    }

    [TestMethod]
    public async Task Request_ClientFails_ReturnsFailure()
    {
        var client = new FakeAiClient(new AiResult { Success = false, ErrorMessage = "AI server đang gặp lỗi." });
        var service = new ScriptAiService(client);

        var response = await service.RequestScriptAssistAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual("AI server đang gặp lỗi.", response.ErrorMessage);
    }

    [TestMethod]
    public async Task Request_EmptyContent_ReturnsFailure()
    {
        var client = new FakeAiClient(new AiResult { Success = true, Content = "   " });
        var service = new ScriptAiService(client);

        var response = await service.RequestScriptAssistAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
    }

    [TestMethod]
    public async Task Request_ClientThrows_ReturnsFailureWithoutLeakingDetail()
    {
        var client = new FakeAiClient(new AiResult { Success = true, Content = "x" }, new HttpRequestException("secret-host"));
        var service = new ScriptAiService(client);

        var response = await service.RequestScriptAssistAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
        Assert.IsFalse(response.ErrorMessage!.Contains("secret-host"));
    }

    private static AiScriptRequest NewRequest() => new()
    {
        ContentId = 1,
        ProjectId = 1,
        RequestedByUserId = 2,
        RequestType = AiRequestType.Outline,
        Title = "Video test",
        Platforms = ["TikTok"],
    };

    private sealed class FakeAiClient(AiResult result, Exception? error = null) : IAIService
    {
        public string? LastPrompt { get; private set; }

        public Task<AiResult> AskAsync(string prompt, CancellationToken cancellationToken = default)
        {
            LastPrompt = prompt;
            if (error is not null)
                throw error;
            return Task.FromResult(result);
        }
    }
}
