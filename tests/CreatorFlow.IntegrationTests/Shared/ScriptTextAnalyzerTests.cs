using CreatorFlow.Models;
using CreatorFlow.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

[TestClass]
public sealed class ScriptTextAnalyzerTests
{
    [TestMethod]
    public void Normalize_UnifiesNewLines_RemovesNulAndBom_Trims()
    {
        string result = ScriptTextAnalyzer.Normalize("\uFEFF  Dòng 1\r\nDòng 2\rDòng\03  \n");

        Assert.AreEqual("Dòng 1\nDòng 2\nDòng3", result);
    }

    [TestMethod]
    public void Normalize_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, ScriptTextAnalyzer.Normalize(null));
        Assert.AreEqual(string.Empty, ScriptTextAnalyzer.Normalize(" \r\n\t "));
    }

    [TestMethod]
    public void Analyze_CountsWordsLinesAndEstimatesDuration()
    {
        var stats = ScriptTextAnalyzer.Analyze("Xin chào các bạn\nHôm nay");

        Assert.AreEqual(6, stats.Words);
        Assert.AreEqual(2, stats.Lines);
        Assert.AreEqual(3, stats.EstimatedSpeakingSeconds); // ceil(6 * 60 / 150)
    }

    [TestMethod]
    public void Analyze_OneMinuteOfWords_IsSixtySeconds()
    {
        string text = string.Join(' ', Enumerable.Repeat("từ", ScriptTextAnalyzer.WordsPerMinute));

        Assert.AreEqual(60, ScriptTextAnalyzer.Analyze(text).EstimatedSpeakingSeconds);
    }

    [TestMethod]
    public void Analyze_Empty_ReturnsEmptyStatistics()
    {
        Assert.AreEqual(ScriptStatistics.Empty, ScriptTextAnalyzer.Analyze("  "));
    }

    [TestMethod]
    public void SplitSections_SeparatesByLeadingMarkers()
    {
        var sections = ScriptTextAnalyzer.SplitSections(
            "Lời dẫn\n[Hook 0-3s]: Mở đầu mạnh\n[Body]\nÝ 1\nÝ 2\n[CTA] Theo dõi");

        Assert.AreEqual(4, sections.Count);
        Assert.AreEqual(ScriptTextAnalyzer.DefaultSectionName, sections[0].Name);
        Assert.AreEqual("Lời dẫn", sections[0].Text);
        Assert.AreEqual("Hook 0-3s", sections[1].Name);
        Assert.AreEqual("Mở đầu mạnh", sections[1].Text);
        Assert.AreEqual("Body", sections[2].Name);
        Assert.AreEqual("Ý 1\nÝ 2", sections[2].Text);
        Assert.AreEqual("CTA", sections[3].Name);
        Assert.AreEqual("Theo dõi", sections[3].Text);
    }

    [TestMethod]
    public void SplitSections_NoMarkers_IsOneGeneralSection()
    {
        var sections = ScriptTextAnalyzer.SplitSections("Chỉ là một đoạn văn.");

        Assert.AreEqual(1, sections.Count);
        Assert.AreEqual(ScriptTextAnalyzer.DefaultSectionName, sections[0].Name);
    }

    [TestMethod]
    public void Truncate_ShortText_IsUntouched()
    {
        string result = ScriptTextAnalyzer.Truncate("abc", 10, out bool truncated);

        Assert.AreEqual("abc", result);
        Assert.IsFalse(truncated);
    }

    [TestMethod]
    public void Truncate_LongText_CutsAtLineBoundary()
    {
        string text = "aaaa\nbbbb\ncccc";

        string result = ScriptTextAnalyzer.Truncate(text, 12, out bool truncated);

        Assert.IsTrue(truncated);
        Assert.AreEqual("aaaa\nbbbb", result);
    }

    [TestMethod]
    public void Truncate_DoesNotSplitSurrogatePair()
    {
        string text = new string('a', 9) + "😀" + "tail"; // emoji chiếm index 9-10

        string result = ScriptTextAnalyzer.Truncate(text, 10, out bool truncated);

        Assert.IsTrue(truncated);
        Assert.AreEqual(new string('a', 9), result);
    }

    [TestMethod]
    public void ComputeVersion_NullAndEmptyMatch_DifferentTextDiffers()
    {
        Assert.AreEqual(ScriptTextAnalyzer.ComputeVersion(null), ScriptTextAnalyzer.ComputeVersion(string.Empty));
        Assert.AreNotEqual(ScriptTextAnalyzer.ComputeVersion("a"), ScriptTextAnalyzer.ComputeVersion("b"));
    }
}
