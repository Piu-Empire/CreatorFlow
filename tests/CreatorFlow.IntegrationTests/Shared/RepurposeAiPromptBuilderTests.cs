using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-59: prompt Repurpose gửi đúng platform đích + thông tin Content gốc.</summary>
[TestClass]
public sealed class RepurposeAiPromptBuilderTests
{
    [TestMethod]
    public void Build_IncludesTargetPlatformAndSourceInfo()
    {
        RepurposeAiRequest request = NewRequest();

        string prompt = RepurposeAiPromptBuilder.Build(request);

        StringAssert.Contains(prompt, "Facebook");
        StringAssert.Contains(prompt, "TikTok");
        StringAssert.Contains(prompt, "Video test");
        StringAssert.Contains(prompt, "Short video");
        StringAssert.Contains(prompt, "[Hook 0-3s] Mở đầu");
        StringAssert.Contains(prompt, "ngắn hơn 30 giây");
        StringAssert.Contains(prompt, "== TIÊU ĐỀ ==");
        StringAssert.Contains(prompt, "== MÔ TẢ ==");
        StringAssert.Contains(prompt, "== KỊCH BẢN ==");
    }

    [TestMethod]
    public void Build_NoScript_DoesNotIncludeScriptSection()
    {
        RepurposeAiRequest request = NewRequest() with { Script = null };

        string prompt = RepurposeAiPromptBuilder.Build(request);

        Assert.IsFalse(prompt.Contains("Kịch bản gốc"));
    }

    [TestMethod]
    public void Build_TruncatedScript_AddsNote()
    {
        RepurposeAiRequest request = NewRequest() with { IsScriptTruncated = true };

        string prompt = RepurposeAiPromptBuilder.Build(request);

        StringAssert.Contains(prompt, "đã được rút gọn");
    }

    [TestMethod]
    public void Build_NoSourcePlatforms_MarksAsUnspecified()
    {
        RepurposeAiRequest request = NewRequest() with { SourcePlatforms = Array.Empty<string>() };

        string prompt = RepurposeAiPromptBuilder.Build(request);

        StringAssert.Contains(prompt, "(không chỉ định)");
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
        Instruction = "ngắn hơn 30 giây",
    };
}
