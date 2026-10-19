using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-58: áp kết quả AI vào kịch bản — chèn thì nối, thay thế thì thay hẳn (không tự ghi đè ngoài ý muốn).</summary>
[TestClass]
public sealed class ScriptAiApplyTests
{
    [TestMethod]
    public void Append_ToEmptyScript_ReturnsAddition()
    {
        Assert.AreEqual("Nội dung AI", ScriptAiApply.Append(string.Empty, "Nội dung AI"));
    }

    [TestMethod]
    public void Append_ToExistingScript_SeparatesWithBlankLine()
    {
        string result = ScriptAiApply.Append("[Hook] A", "Nội dung AI");
        Assert.AreEqual("[Hook] A\n\nNội dung AI", result);
    }

    [TestMethod]
    public void Append_EmptyAddition_KeepsCurrent()
    {
        Assert.AreEqual("[Hook] A", ScriptAiApply.Append("[Hook] A", "  "));
    }

    [TestMethod]
    public void Replace_DiscardsExistingScript()
    {
        Assert.AreEqual("Nội dung AI", ScriptAiApply.Replace("Nội dung AI"));
    }

    [TestMethod]
    public void Append_NormalizesLineEndings()
    {
        string result = ScriptAiApply.Append(string.Empty, "A\r\nB");
        Assert.AreEqual("A\nB", result);
    }
}
