using CreatorFlow.Api.Models.Ideas;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Services.Ideas;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Ideas;

/// <summary>Quy tắc thuần của Idea Bank phía Backend (không DB): chuẩn hóa tag, giới hạn độ dài, đổi trạng thái, quyền sửa.</summary>
[TestClass]
public sealed class IdeaRulesTests
{
    private static Idea IdeaBy(long creator, IdeaStatus status = IdeaStatus.Draft) =>
        new(1, 1, "t", "", "", status, [], creator, "u", DateTime.UtcNow, DateTime.UtcNow, null);

    // ---------------------------------------------------------------- tag

    [TestMethod]
    [DataRow("#Funny", "Funny")]
    [DataRow("##x", "x")]
    [DataRow("  a    b  ", "a b")]
    [DataRow("# tag", "tag")]
    [DataRow("   ", "")]
    [DataRow("#", "")]
    public void NormalizeTag_StripsHashTrimsAndCollapsesSpaces(string input, string expected) =>
        Assert.AreEqual(expected, IdeaRules.NormalizeTag(input));

    [TestMethod]
    public void NormalizeTag_Null_ReturnsEmpty() => Assert.AreEqual(string.Empty, IdeaRules.NormalizeTag(null));

    [TestMethod]
    public void NormalizeTags_DropsEmptyAndDuplicates_CaseInsensitive_KeepingFirstSpelling()
    {
        string[] result = IdeaRules.NormalizeTags(["Vlog", "#vlog", " ", "Tech", "TECH", "#"]);

        CollectionAssert.AreEqual(new[] { "Vlog", "Tech" }, result);
        Assert.AreEqual(0, IdeaRules.NormalizeTags(null).Length);
    }

    // ---------------------------------------------------------------- độ dài

    [TestMethod]
    public void Validate_AcceptsExactLimits()
    {
        string[] tags = Enumerable.Range(0, 10).Select(i => new string('x', 80)).ToArray();

        Assert.IsNull(IdeaRules.Validate(new string('a', 250), new string('b', 2000), new string('c', 2000), tags));
    }

    [TestMethod]
    public void Validate_RejectsOneOverEachLimit_WithTheMatchingMessage()
    {
        StringAssert.Contains(IdeaRules.Validate("", "", "", [])!, "tiêu đề");
        StringAssert.Contains(IdeaRules.Validate(new string('a', 251), "", "", [])!, "250");
        StringAssert.Contains(IdeaRules.Validate("a", new string('b', 2001), "", [])!, "Mô tả");
        StringAssert.Contains(IdeaRules.Validate("a", "", new string('c', 2001), [])!, "Ghi chú");
        StringAssert.Contains(IdeaRules.Validate("a", "", "", Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray())!, "10 tag");
        StringAssert.Contains(IdeaRules.Validate("a", "", "", [new string('x', 81)])!, "80");
    }

    // ---------------------------------------------------------------- trạng thái

    [TestMethod]
    [DataRow("Draft", IdeaStatus.Draft)]
    [DataRow("backlog", IdeaStatus.Backlog)]
    [DataRow("ARCHIVED", IdeaStatus.Archived)]
    [DataRow("  Converted  ", IdeaStatus.Converted)]
    public void TryParseStatus_AcceptsStatusNames_IgnoringCaseAndSurroundingSpaces(string input, IdeaStatus expected)
    {
        Assert.IsTrue(IdeaRules.TryParseStatus(input, out IdeaStatus status));
        Assert.AreEqual(expected, status);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("Nope")]
    [DataRow("1")]
    [DataRow("-1")]
    [DataRow("99")]
    [DataRow("Draft,Backlog")]
    [DataRow("Draft Backlog")]
    [DataRow("Draft|Backlog")]
    public void TryParseStatus_RejectsNumbersListsAndUnknownNames(string? input) =>
        Assert.IsFalse(IdeaRules.TryParseStatus(input, out _));

    [TestMethod]
    [DataRow(null, IdeaStatus.Draft, true)]
    [DataRow(null, IdeaStatus.Backlog, true)]
    [DataRow(null, IdeaStatus.Archived, true)]
    [DataRow(null, IdeaStatus.Converted, false)]
    [DataRow(IdeaStatus.Draft, IdeaStatus.Converted, false)]
    [DataRow(IdeaStatus.Archived, IdeaStatus.Backlog, true)]
    [DataRow(IdeaStatus.Converted, IdeaStatus.Converted, true)]
    [DataRow(IdeaStatus.Converted, IdeaStatus.Draft, false)]
    [DataRow(IdeaStatus.Converted, IdeaStatus.Archived, false)]
    public void ValidateStatusChange_Matrix(IdeaStatus? current, IdeaStatus requested, bool allowed) =>
        Assert.AreEqual(allowed, IdeaRules.ValidateStatusChange(current, requested) is null);

    [TestMethod]
    [DataRow(IdeaStatus.Draft, true)]
    [DataRow(IdeaStatus.Backlog, true)]
    [DataRow(IdeaStatus.Archived, false)]
    [DataRow(IdeaStatus.Converted, false)]
    public void ValidateConvertible_OnlyDraftAndBacklog(IdeaStatus status, bool convertible) =>
        Assert.AreEqual(convertible, IdeaRules.ValidateConvertible(status) is null);

    // ---------------------------------------------------------------- quyền

    [TestMethod]
    public void CanEdit_OwnerAndManagerAnyIdea_CreatorOnlyOwn_OutsiderNever()
    {
        Idea byCreator7 = IdeaBy(7);

        Assert.IsTrue(IdeaRules.CanEdit(ProjectRole.Owner, byCreator7, 1));
        Assert.IsTrue(IdeaRules.CanEdit(ProjectRole.Manager, byCreator7, 1));
        Assert.IsTrue(IdeaRules.CanEdit(ProjectRole.Creator, byCreator7, 7));
        Assert.IsFalse(IdeaRules.CanEdit(ProjectRole.Creator, byCreator7, 8));
        Assert.IsFalse(IdeaRules.CanEdit(null, byCreator7, 7));
    }
}
