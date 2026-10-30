using CreatorFlow.Models;
using CreatorFlow.Services.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-60: parser tách Nhận xét / Gợi ý và chịu lỗi khi AI trả sai định dạng.</summary>
[TestClass]
public sealed class PerformanceAiResponseParserTests
{
    [TestMethod]
    public void Parse_BothMarkers_ReturnsRemarksAndBullets()
    {
        string raw = """
            == NHẬN XÉT ==
            Minh dẫn đầu về lượt xem.
            == GỢI Ý CẢI THIỆN ==
            - Chuyển bớt việc cho Huy.
            - Xem lại 3 bài trễ hạn.
            """;

        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse(raw);

        Assert.IsTrue(parsed.IsStructured);
        StringAssert.Contains(parsed.Remarks, "Minh dẫn đầu");
        CollectionAssert.AreEqual(
            new[] { "Chuyển bớt việc cho Huy.", "Xem lại 3 bài trễ hạn." },
            parsed.Suggestions.ToArray());
    }

    [TestMethod]
    public void Parse_MissingSuggestions_IsUnstructured()
    {
        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse("Chỉ là một đoạn văn tự do.");

        Assert.IsFalse(parsed.IsStructured);
        Assert.AreEqual("Chỉ là một đoạn văn tự do.", parsed.Remarks);
        Assert.AreEqual(0, parsed.Suggestions.Count);
    }

    [TestMethod]
    public void Parse_MarkersOutOfOrder_IsUnstructured()
    {
        string raw = """
            == GỢI Ý CẢI THIỆN ==
            - A
            == NHẬN XÉT ==
            B
            """;

        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse(raw);

        Assert.IsFalse(parsed.IsStructured);
        Assert.AreEqual(0, parsed.Suggestions.Count);
    }

    [TestMethod]
    public void Parse_BulletsWithAsteriskAndNumbers()
    {
        string raw = """
            == NHẬN XÉT ==
            Ổn.
            == GỢI Ý CẢI THIỆN ==
            * Tăng tần suất đăng.
            1. Dời deadline bài trễ.
            """;

        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse(raw);

        Assert.IsTrue(parsed.IsStructured);
        CollectionAssert.AreEqual(
            new[] { "Tăng tần suất đăng.", "Dời deadline bài trễ." },
            parsed.Suggestions.ToArray());
    }

    [TestMethod]
    public void Parse_PlainLinesWithoutBullets_AreKept()
    {
        string raw = """
            == NHẬN XÉT ==
            Ổn.
            == GỢI Ý CẢI THIỆN ==
            Tăng tần suất đăng.
            Dời deadline bài trễ.
            """;

        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse(raw);

        Assert.IsTrue(parsed.IsStructured);
        Assert.AreEqual(2, parsed.Suggestions.Count);
    }

    [TestMethod]
    public void Parse_TooManySuggestions_KeepsMax()
    {
        string block = string.Join('\n', Enumerable.Range(1, 12).Select(i => $"- Gợi ý {i}"));
        string raw = "== NHẬN XÉT ==\nỔn.\n== GỢI Ý CẢI THIỆN ==\n" + block;

        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse(raw);

        Assert.AreEqual(PerformanceAiResponseParser.MaxSuggestions, parsed.Suggestions.Count);
    }

    [TestMethod]
    public void Parse_Empty_ReturnsUnstructured()
    {
        PerformanceParsedContent parsed = PerformanceAiResponseParser.Parse("   ");

        Assert.IsFalse(parsed.IsStructured);
        Assert.AreEqual(string.Empty, parsed.Remarks);
        Assert.AreEqual(0, parsed.Suggestions.Count);
    }
}
