using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-58: prompt gửi AI cho Outline/Script dựng đúng từ thông tin Content.</summary>
[TestClass]
public sealed class ScriptAiPromptBuilderTests
{
    [TestMethod]
    public void Build_Outline_AsksForOutlineAndIncludesContentInfo()
    {
        var request = NewRequest(AiRequestType.Outline, script: string.Empty);

        string prompt = ScriptAiPromptBuilder.Build(request);

        StringAssert.Contains(prompt, "DÀN Ý");
        StringAssert.Contains(prompt, "Tiêu đề: Video test");
        StringAssert.Contains(prompt, "Loại nội dung: Short video");
        StringAssert.Contains(prompt, "Nền tảng: TikTok");
        StringAssert.Contains(prompt, ScriptAiPromptBuilder.SectionGuide);
        Assert.IsFalse(prompt.Contains("Kịch bản hiện có"), "Script trống thì không có khối kịch bản hiện có.");
    }

    [TestMethod]
    public void Build_Script_AsksForScriptAndIncludesExistingScript()
    {
        var request = NewRequest(AiRequestType.Script, script: "[Hook 0-3s] Mở đầu");

        string prompt = ScriptAiPromptBuilder.Build(request);

        StringAssert.Contains(prompt, "KỊCH BẢN");
        StringAssert.Contains(prompt, "[Hook 0-3s] Mở đầu");
        StringAssert.Contains(prompt, "Yêu cầu thêm của người dùng");
    }

    [TestMethod]
    public void Build_NoPlatforms_MarksAsUnspecified()
    {
        var request = new AiScriptRequest
        {
            ContentId = 1,
            ProjectId = 1,
            RequestedByUserId = 1,
            RequestType = AiRequestType.Outline,
            Title = "Video test",
        };

        string prompt = ScriptAiPromptBuilder.Build(request);

        StringAssert.Contains(prompt, "Nền tảng: (không chỉ định)");
    }

    private static AiScriptRequest NewRequest(AiRequestType type, string? script) => new()
    {
        ContentId = 1,
        ProjectId = 1,
        RequestedByUserId = 2,
        RequestType = type,
        Title = "Video test",
        Description = "Concept hook",
        ContentType = "Short video",
        Priority = Priority.High,
        Platforms = ["TikTok"],
        Script = script ?? string.Empty,
        Instruction = "rút gọn 30 giây",
    };
}
