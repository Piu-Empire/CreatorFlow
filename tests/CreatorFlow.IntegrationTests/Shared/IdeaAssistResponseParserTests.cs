using CreatorFlow.Models;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-57 — Phân tích nội dung AI trả về thành danh sách gợi ý, chịu nhiều định dạng.</summary>
[TestClass]
public sealed class IdeaAssistResponseParserTests
{
    [TestMethod]
    public void Parse_PipeLines_SplitsTitleAndDescription()
    {
        string text = "Tiêu đề 1 | Mô tả 1\nTiêu đề 2 | Mô tả 2";

        var result = IdeaAssistResponseParser.Parse(text, 5);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Tiêu đề 1", result[0].Title);
        Assert.AreEqual("Mô tả 1", result[0].Description);
    }

    [TestMethod]
    public void Parse_NumberedAndBulletedLines_StripsMarkers()
    {
        string text = "1. Tiêu đề A | Mô tả A\n- Tiêu đề B | Mô tả B\n• **Tiêu đề C** | Mô tả C";

        var result = IdeaAssistResponseParser.Parse(text, 5);

        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("Tiêu đề A", result[0].Title);
        Assert.AreEqual("Tiêu đề B", result[1].Title);
        Assert.AreEqual("Tiêu đề C", result[2].Title);
    }

    [TestMethod]
    public void Parse_KeepsLeadingNumberInPlainTitle()
    {
        var result = IdeaAssistResponseParser.Parse("5 mẹo học tiếng Anh | Gợi ý nhanh", 5);

        Assert.AreEqual("5 mẹo học tiếng Anh", result[0].Title);
    }

    [TestMethod]
    public void Parse_JsonArray_WithFences()
    {
        string text = "```json\n[{\"title\":\"JSON 1\",\"description\":\"D1\"},{\"title\":\"JSON 2\",\"description\":\"D2\"}]\n```";

        var result = IdeaAssistResponseParser.Parse(text, 5);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("JSON 1", result[0].Title);
        Assert.AreEqual("D2", result[1].Description);
    }

    [TestMethod]
    public void Parse_RespectsMax()
    {
        var result = IdeaAssistResponseParser.Parse("A | 1\nB | 2\nC | 3", 2);

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void Parse_EmptyOrNull_ReturnsEmpty()
    {
        Assert.AreEqual(0, IdeaAssistResponseParser.Parse(null, 5).Count);
        Assert.AreEqual(0, IdeaAssistResponseParser.Parse("   ", 5).Count);
        Assert.AreEqual(0, IdeaAssistResponseParser.Parse("A | 1", 0).Count);
    }
}
