using CreatorFlow.AI;
using CreatorFlow.AI.Models;
using CreatorFlow.Models;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-60: PerformanceAiService nối cổng phân tích với lõi AI client và map lỗi đúng.</summary>
[TestClass]
public sealed class PerformanceAiServiceTests
{
    [TestMethod]
    public async Task Analyze_NoClient_ReturnsNotConfigured()
    {
        var service = new PerformanceAiService(null);

        PerformanceAiResponse response = await service.AnalyzeAsync(NewRequest());

        Assert.IsFalse(service.IsConfigured);
        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual(NotConfiguredAiService.Message, response.ErrorMessage);
        Assert.AreEqual(0, response.Suggestions.Count);
    }

    [TestMethod]
    public async Task Analyze_Success_PassesPromptAndParses()
    {
        var client = new FakeAiClient(new AiResult
        {
            Success = true,
            Content = """
                == NHẬN XÉT ==
                Minh dẫn đầu về lượt xem.
                == GỢI Ý CẢI THIỆN ==
                - Chuyển bớt việc cho Huy.
                """,
        });
        var service = new PerformanceAiService(client);

        PerformanceAiResponse response = await service.AnalyzeAsync(NewRequest());

        Assert.IsTrue(response.Succeeded);
        Assert.IsTrue(response.IsStructured);
        StringAssert.Contains(response.Remarks, "Minh dẫn đầu");
        CollectionAssert.AreEqual(new[] { "Chuyển bớt việc cho Huy." }, response.Suggestions.ToArray());
        Assert.IsNotNull(client.LastPrompt);
        StringAssert.Contains(client.LastPrompt, "120,000");
        StringAssert.Contains(client.LastPrompt, "Tết 2026");
    }

    [TestMethod]
    public async Task Analyze_ClientFailure_ReturnsClientMessage()
    {
        var client = new FakeAiClient(new AiResult { Success = false, ErrorMessage = "AI đang bảo trì." });
        var service = new PerformanceAiService(client);

        PerformanceAiResponse response = await service.AnalyzeAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual("AI đang bảo trì.", response.ErrorMessage);
    }

    [TestMethod]
    public async Task Analyze_ClientThrows_GenericMessageWithoutDetails()
    {
        var client = new FakeAiClient(new AiResult(), new HttpRequestException("secret-host"));
        var service = new PerformanceAiService(client);

        PerformanceAiResponse response = await service.AnalyzeAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
        Assert.IsFalse(response.ErrorMessage!.Contains("secret-host"));
    }

    [TestMethod]
    public async Task Analyze_EmptyContent_ReturnsFailure()
    {
        var client = new FakeAiClient(new AiResult { Success = true, Content = "   " });
        var service = new PerformanceAiService(client);

        PerformanceAiResponse response = await service.AnalyzeAsync(NewRequest());

        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual("AI không trả về nội dung.", response.ErrorMessage);
    }

    [TestMethod]
    public async Task Analyze_ClientCancels_PropagatesCancellation()
    {
        var client = new FakeAiClient(new AiResult(), new OperationCanceledException());
        var service = new PerformanceAiService(client);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => service.AnalyzeAsync(NewRequest()));
    }

    private static PerformanceAiRequest NewRequest() => new()
    {
        ProjectId = 1,
        ProjectName = "Tết 2026",
        Scope = PerformanceScope.Metrics,
        ScopeLabel = "Theo Creator",
        GroupBy = "creator",
        LoadedAt = new DateTime(2026, 10, 29, 20, 15, 0),
        Metrics = new[]
        {
            new PerformanceMetricLine("Minh", 120_000, 5_000, 1_200, 500, 5.58),
        },
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
