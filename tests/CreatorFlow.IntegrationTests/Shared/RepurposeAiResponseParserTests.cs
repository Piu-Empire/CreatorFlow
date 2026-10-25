using CreatorFlow.Models;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-59: tách output AI Repurpose thành 3 phần, chịu lỗi khi thiếu mốc.</summary>
[TestClass]
public sealed class RepurposeAiResponseParserTests
{
    [TestMethod]
    public void Parse_AllMarkers_ReturnsThreeParts()
    {
        const string raw = """
            == TIÊU ĐỀ ==
            Tiêu đề mới
            == MÔ TẢ ==
            Mô tả mới
            == KỊCH BẢN ==
            [Hook 0-3s] Mở đầu
            [Body] Nội dung
            """;

        RepurposeParsedContent parsed = RepurposeAiResponseParser.Parse(raw);

        Assert.IsTrue(parsed.IsStructured);
        Assert.AreEqual("Tiêu đề mới", parsed.Title);
        Assert.AreEqual("Mô tả mới", parsed.Description);
        StringAssert.Contains(parsed.Script, "[Hook 0-3s] Mở đầu");
        StringAssert.Contains(parsed.Script, "[Body] Nội dung");
    }

    [TestMethod]
    public void Parse_AccentlessMarkers_StillWorks()
    {
        const string raw = """
            == TIEU DE ==
            Tiêu đề
            == MO TA ==
            Mô tả
            == KICH BAN ==
            Kịch bản
            """;

        RepurposeParsedContent parsed = RepurposeAiResponseParser.Parse(raw);

        Assert.IsTrue(parsed.IsStructured);
        Assert.AreEqual("Tiêu đề", parsed.Title);
        Assert.AreEqual("Kịch bản", parsed.Script);
    }

    [TestMethod]
    public void Parse_MissingMarkers_FallsBackToScript()
    {
        const string raw = "AI trả về một đoạn văn không có mốc.";

        RepurposeParsedContent parsed = RepurposeAiResponseParser.Parse(raw);

        Assert.IsFalse(parsed.IsStructured);
        Assert.IsNull(parsed.Title);
        Assert.IsNull(parsed.Description);
        Assert.AreEqual(raw, parsed.Script);
    }

    [TestMethod]
    public void Parse_Empty_ReturnsNotStructured()
    {
        RepurposeParsedContent parsed = RepurposeAiResponseParser.Parse("   ");

        Assert.IsFalse(parsed.IsStructured);
        Assert.IsNull(parsed.Script);
    }
}
