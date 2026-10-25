using CreatorFlow.AI;
using CreatorFlow.AI.Models;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-59: RepurposeAiService nối cổng Repurpose với lõi AI client và map kết quả/lỗi đúng.</summary>
[TestClass]
public sealed class RepurposeAiServiceTests
{
    [TestMethod]
    public async Task Request_NoClient_ReturnsNotConfigured()
    {
        var service = new RepurposeAiService(null);

        RepurposeAiResponse response = await service.RequestRepurposeAsync(NewRequest());

        Assert.IsFalse(service.IsConfigured);
        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual(NotConfiguredAiService.Message, response.ErrorMessage);
    }

    [TestMethod]
    public async Task Request_ClientSucceeds_ParsesThreeParts()
    {
        var client = new FakeAiClient(new AiResult
        {
            Success = true,
            Content = """
                == TIÊU ĐỀ ==
                Tiêu đề Facebook
                == MÔ TẢ ==
                Mô tả Facebook
                == KỊCH BẢN ==
                [Hook 0-3s] Mở đầu
                """,
        });
        var service = new RepurposeAiService(client);

        RepurposeAiResponse response = await service.RequestRepurposeAsync(NewRequest());

        Assert.IsTrue(response.Succeeded);
        Assert.IsTrue(response.IsStructured);
        Assert.AreEqual("Tiêu đề Facebook", response.Title);
        Assert.AreEqual("Mô tả Facebook", response.Description);
        StringAssert.Contains(response.Script, "[Hook 0-3s] Mở đầu");
        Assert.IsNotNull(client.LastPrompt);
        StringAssert.Contains(client.LastPrompt, "Facebook");
    }

    [TestMethod]
    public async Task Request_UnstructuredContent_FallsBackToScript()
    {
        var client = new FakeAiClient(new AiResult { Success = true, Content = "Một đoạn văn tự do." });
        var service = new RepurposeAiService(client);

        RepurposeAiResponse response = await service.RequestRepurposeAsync(NewRequest());

        Assert.IsTrue(response.Succeeded);
        Assert.IsFalse(response.IsStructured);
        Assert.IsNull(response.Title);
        Assert.IsNull(response.Description);
        Assert.AreEqual("Một đoạn văn tự do.", response.Script);
    }

    [TestMethod]
    public async Task Request_ClientFails_ReturnsFailure()
    {
        var client = new FakeAiClient(new AiResult { Success = false, ErrorMessage = "AI đang gặp lỗi." });
        var service = new RepurposeAiService(client);

        RepurposeAiResponse response = await service.RequestRepurposeAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual("AI đang gặp lỗi.", response.ErrorMessage);
    }

    [TestMethod]
    public async Task Request_EmptyContent_ReturnsFailure()
    {
        var client = new FakeAiClient(new AiResult { Success = true, Content = "   " });
        var service = new RepurposeAiService(client);

        RepurposeAiResponse response = await service.RequestRepurposeAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
    }

    [TestMethod]
    public async Task Request_ClientThrows_ReturnsFailureWithoutLeakingDetail()
    {
        var client = new FakeAiClient(new AiResult { Success = true, Content = "x" }, new HttpRequestException("secret-host"));
        var service = new RepurposeAiService(client);

        RepurposeAiResponse response = await service.RequestRepurposeAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
        Assert.IsFalse(response.ErrorMessage!.Contains("secret-host"));
    }

    [TestMethod]
    public async Task Request_ClientCancels_PropagatesCancellation()
    {
        var client = new FakeAiClient(new AiResult(), new OperationCanceledException());
        var service = new RepurposeAiService(client);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => service.RequestRepurposeAsync(NewRequest()));
    }

    private static RepurposeAiRequest NewRequest() => new()
    {
        SourceContentId = 1,
        ProjectId = 1,
        RequestedByUserId = 2,
        Title = "Video test",
        Description = "Mô tả test",
        Script = "[Hook 0-3s] Mở đầu",
        ContentType = "Short video",
        Priority = Priority.High,
        SourcePlatforms = new[] { "TikTok" },
        TargetPlatform = "Facebook",
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
