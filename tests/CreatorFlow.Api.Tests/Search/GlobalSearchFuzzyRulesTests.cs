using CreatorFlow.Api.Repositories.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace CreatorFlow.Api.Tests.Search;

[TestClass]
[TestCategory("GlobalSearchApiUnit")]
public sealed class GlobalSearchFuzzyRulesTests
{
    [TestMethod]
    public void NormalizeSearchText_IgnoresVietnameseAccentsAndUnicodeComposition()
    {
        string composed = GlobalSearchRepository.NormalizeSearchText("BÁN NGƯỜI SANG CAM");
        string decomposed = GlobalSearchRepository.NormalizeSearchText("BÁN NGƯỜI SANG CAM".Normalize(NormalizationForm.FormD));

        Assert.AreEqual("ban nguoi sang cam", composed);
        Assert.AreEqual(composed, decomposed);
    }

    [TestMethod]
    [DataRow("ban nguoi sang cam")]
    [DataRow("ban nguo sang cam")]
    [DataRow("ban nguio sang cam")]
    [DataRow("ban ngwio sang cam")]
    public void FuzzyBigrams_KeepContentCandidateForSmallTypingErrors(string query)
    {
        string normalizedQuery = GlobalSearchRepository.NormalizeSearchText(query);
        string normalizedTitle = GlobalSearchRepository.NormalizeSearchText("bán người sang cam");
        string[] grams = GlobalSearchRepository.BuildFuzzyGrams(normalizedQuery);
        int matchedGrams = grams.Count(normalizedTitle.Contains);

        Assert.IsTrue(grams.Length > 0);
        Assert.IsTrue(matchedGrams >= GlobalSearchRepository.MinimumFuzzyGramMatches(grams.Length),
            $"Expected '{query}' to remain a fuzzy candidate for the existing title.");
    }
}
