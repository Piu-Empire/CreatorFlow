using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Database;

/// <summary>
/// SCRUM-31 — Chuyển Idea thành Content trên PostgreSQL thật: contents.source_idea_id, content_tags, trạng thái Converted.
/// Cần đã chạy 01_schema.sql + 02_seed.sql + 07_idea_bank.sql. Mỗi test chạy trong 1 transaction và Rollback ở cuối nên không để lại dữ liệu.
/// </summary>
[TestClass]
public sealed class IdeaConvertToContentIntegrationTests
{
    private NpgsqlUnitOfWork _uow = null!;
    private IdeaRepository _ideas = null!;
    private long _projectId;
    private long _userId;

    [TestInitialize]
    public void Setup()
    {
        string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive("Set CREATORFLOW_TEST_CONNECTION_STRING for an explicitly approved test database.");
        }

        _uow = new NpgsqlUnitOfWork(connectionString);
        _ideas = new IdeaRepository(_uow);

        _projectId = ScalarLong("SELECT project_id FROM projects ORDER BY project_id LIMIT 1");
        _userId = ScalarLong($"SELECT user_id FROM project_members WHERE project_id = {_projectId} ORDER BY user_id LIMIT 1");

        _uow.Begin();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _uow?.Rollback();
        _uow?.Dispose();
    }

    [TestMethod]
    public void ConvertToContent_CreatesContentWithSourceLink_Info_AndTags()
    {
        long ideaId = _ideas.Create(_projectId, FullDraft(IdeaStatus.Backlog), _userId);

        long? contentId = _ideas.ConvertToContent(ideaId, "Short video", _userId);

        Assert.IsNotNull(contentId);
        Assert.AreEqual(ideaId, ScalarLong($"SELECT source_idea_id FROM contents WHERE content_id = {contentId}"));
        Assert.AreEqual(_projectId, ScalarLong($"SELECT project_id FROM contents WHERE content_id = {contentId}"));
        Assert.AreEqual(_userId, ScalarLong($"SELECT created_by FROM contents WHERE content_id = {contentId}"));
        Assert.AreEqual(1, ScalarLong($"SELECT COUNT(*) FROM contents WHERE content_id = {contentId} AND title = 'Idea convert test' AND description = 'Mô tả tiếng Việt có dấu' AND content_type = 'Short video' AND status = 'IDEA' AND priority = 'MEDIUM'"));
        Assert.AreEqual(2, ScalarLong($"SELECT COUNT(*) FROM content_tags WHERE content_id = {contentId}"));
        Assert.AreEqual(0, ScalarLong($"SELECT COUNT(*) FROM content_tags ct JOIN tags t ON t.tag_id = ct.tag_id WHERE ct.content_id = {contentId} AND t.project_id <> {_projectId}"));
    }

    [TestMethod]
    public void ConvertToContent_KeepsIdea_SetsConverted_AndReadsBackContentId()
    {
        long ideaId = _ideas.Create(_projectId, FullDraft(IdeaStatus.Draft), _userId);

        long? contentId = _ideas.ConvertToContent(ideaId, "Short video", _userId);

        Idea idea = _ideas.GetById(ideaId)!;
        Assert.AreEqual(IdeaStatus.Converted, idea.Status);
        Assert.AreEqual(contentId, idea.ConvertedContentId);
        Assert.AreEqual("Idea convert test", idea.Title);
        Assert.AreEqual("Ghi chú nội bộ", idea.Note);
        CollectionAssert.AreEqual(new[] { "IConvert-Alpha", "IConvert-Beta" }, idea.Tags);
    }

    [TestMethod]
    public void ConvertToContent_SecondCall_ReturnsNull_AndCreatesNoSecondContent()
    {
        long ideaId = _ideas.Create(_projectId, FullDraft(IdeaStatus.Backlog), _userId);
        Assert.IsNotNull(_ideas.ConvertToContent(ideaId, "Short video", _userId));

        Assert.IsNull(_ideas.ConvertToContent(ideaId, "Short video", _userId));
        Assert.AreEqual(1, ScalarLong($"SELECT COUNT(*) FROM contents WHERE source_idea_id = {ideaId}"));
    }

    [TestMethod]
    public void ConvertToContent_ArchivedOrUnknownIdea_ReturnsNull_AndWritesNothing()
    {
        long archived = _ideas.Create(_projectId, FullDraft(IdeaStatus.Archived), _userId);

        Assert.IsNull(_ideas.ConvertToContent(archived, "Short video", _userId));
        Assert.IsNull(_ideas.ConvertToContent(long.MaxValue, "Short video", _userId));
        Assert.AreEqual(0, ScalarLong($"SELECT COUNT(*) FROM contents WHERE source_idea_id = {archived}"));
        Assert.AreEqual(IdeaStatus.Archived, _ideas.GetById(archived)!.Status);
    }

    [TestMethod]
    public void ConvertToContent_ThenDeleteIdea_KeepsContent_WithNullSource()
    {
        long ideaId = _ideas.Create(_projectId, FullDraft(IdeaStatus.Backlog), _userId);
        long contentId = _ideas.ConvertToContent(ideaId, "Short video", _userId)!.Value;

        _ideas.Delete(ideaId);

        Assert.AreEqual(1, ScalarLong($"SELECT COUNT(*) FROM contents WHERE content_id = {contentId} AND source_idea_id IS NULL"));
    }

    private static IdeaDraft FullDraft(IdeaStatus status) => new()
    {
        Title = "Idea convert test",
        Description = "Mô tả tiếng Việt có dấu",
        Note = "Ghi chú nội bộ",
        Status = status,
        Tags = new List<string> { "IConvert-Alpha", "IConvert-Beta" },
    };

    private long ScalarLong(string sql)
    {
        using var cmd = _uow.CreateCommand(sql);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }
}
