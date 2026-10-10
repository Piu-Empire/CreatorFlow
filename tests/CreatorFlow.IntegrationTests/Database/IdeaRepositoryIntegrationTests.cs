using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Database;

/// <summary>
/// Idea Bank trên PostgreSQL thật: CRUD, tag (idea_tags), tìm kiếm/lọc trong SQL và tách dữ liệu theo Project.
/// Cần đã chạy 01_schema.sql + 02_seed.sql + 07_idea_bank.sql. Mỗi test chạy trong 1 transaction và Rollback ở cuối nên không để lại dữ liệu.
/// </summary>
[TestClass]
public sealed class IdeaRepositoryIntegrationTests
{
    private NpgsqlUnitOfWork _uow = null!;
    private IdeaRepository _ideas = null!;
    private long _projectId;
    private long _userId;

    [TestInitialize]
    public void Setup()
    {
        // Cùng quy ước với các test Database khác: chỉ chạy khi đặt biến môi trường trỏ tới DB test đã đồng ý.
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
    public void Create_StoresAllFieldsAndTags_AndGetByIdReadsThemBack()
    {
        long id = _ideas.Create(_projectId, FullDraft(), _userId);

        Idea idea = _ideas.GetById(id)!;

        Assert.AreEqual("Idea integration test", idea.Title);
        Assert.AreEqual("Mô tả tiếng Việt có dấu", idea.Description);
        Assert.AreEqual("Ghi chú nội bộ", idea.Note);
        Assert.AreEqual(IdeaStatus.Backlog, idea.Status);
        CollectionAssert.AreEqual(new[] { "ITest-Alpha", "ITest-Beta" }, idea.Tags);
        Assert.AreEqual(_projectId, idea.ProjectId);
        Assert.AreEqual(_userId, idea.CreatedByUserId);
        Assert.IsFalse(string.IsNullOrEmpty(idea.CreatedByName));
    }

    [TestMethod]
    public void Create_WithEmptyDescriptionAndNote_ReadsBackAsEmpty()
    {
        var draft = FullDraft();
        draft.Description = string.Empty;
        draft.Note = string.Empty;

        Idea idea = _ideas.GetById(_ideas.Create(_projectId, draft, _userId))!;

        Assert.AreEqual(string.Empty, idea.Description);
        Assert.AreEqual(string.Empty, idea.Note);
    }

    [TestMethod]
    public void Update_ChangesFields_AndSyncsTagsWithoutRecreatingKeptOnes()
    {
        long id = _ideas.Create(_projectId, FullDraft(), _userId);
        long keptTagId = ScalarLong($"SELECT tag_id FROM tags WHERE project_id = {_projectId} AND name = 'ITest-Alpha'");

        var edit = FullDraft();
        edit.Title = "Tiêu đề đã sửa";
        edit.Status = IdeaStatus.Archived;
        edit.Tags = new List<string> { "ITest-Alpha", "ITest-Gamma" }; // giữ Alpha, bỏ Beta, thêm Gamma
        _ideas.Update(id, edit);

        Idea idea = _ideas.GetById(id)!;
        Assert.AreEqual("Tiêu đề đã sửa", idea.Title);
        Assert.AreEqual(IdeaStatus.Archived, idea.Status);
        CollectionAssert.AreEqual(new[] { "ITest-Alpha", "ITest-Gamma" }, idea.Tags);
        Assert.AreEqual(keptTagId, ScalarLong($"SELECT tag_id FROM tags WHERE project_id = {_projectId} AND name = 'ITest-Alpha'"));
        Assert.AreEqual(_userId, idea.CreatedByUserId);
    }

    [TestMethod]
    public void Update_WithNoTags_RemovesAllIdeaTags()
    {
        long id = _ideas.Create(_projectId, FullDraft(), _userId);

        var edit = FullDraft();
        edit.Tags = new List<string>();
        _ideas.Update(id, edit);

        Assert.AreEqual(0, _ideas.GetById(id)!.Tags.Count);
    }

    [TestMethod]
    public void Delete_RemovesIdeaAndItsTagLinks()
    {
        long id = _ideas.Create(_projectId, FullDraft(), _userId);

        _ideas.Delete(id);

        Assert.IsNull(_ideas.GetById(id));
        Assert.AreEqual(0, ScalarLong($"SELECT COUNT(*) FROM idea_tags WHERE idea_id = {id}"));
    }

    [TestMethod]
    public void GetByProject_Search_MatchesTitleDescriptionNoteAndTag_CaseInsensitive()
    {
        long id = _ideas.Create(_projectId, FullDraft(), _userId);

        Assert.IsTrue(Search("INTEGRATION TEST").Any(i => i.IdeaId == id));      // tiêu đề
        Assert.IsTrue(Search("tiếng việt có dấu").Any(i => i.IdeaId == id));     // mô tả
        Assert.IsTrue(Search("nội bộ").Any(i => i.IdeaId == id));                // ghi chú
        Assert.IsTrue(Search("itest-beta").Any(i => i.IdeaId == id));            // tag
        Assert.IsFalse(Search("khong-co-tu-nay-xyz").Any(i => i.IdeaId == id));
    }

    [TestMethod]
    public void GetByProject_Search_TreatsPercentAndUnderscoreAsLiteralText()
    {
        var draft = FullDraft();
        draft.Title = "Giảm 50% phí_dịch vụ";
        long id = _ideas.Create(_projectId, draft, _userId);

        Assert.IsTrue(Search("50%").Any(i => i.IdeaId == id));
        Assert.IsTrue(Search("phí_dịch").Any(i => i.IdeaId == id));
        Assert.IsFalse(Search("50%x").Any(i => i.IdeaId == id));
        Assert.IsFalse(Search("phí-dịch").Any(i => i.IdeaId == id));
    }

    [TestMethod]
    public void GetByProject_FiltersByStatusAndTag()
    {
        long id = _ideas.Create(_projectId, FullDraft(), _userId);

        var byStatus = _ideas.GetByProject(_projectId, new IdeaFilter { Status = IdeaStatus.Backlog });
        Assert.IsTrue(byStatus.Any(i => i.IdeaId == id));
        Assert.IsTrue(byStatus.All(i => i.Status == IdeaStatus.Backlog));

        Assert.IsFalse(_ideas.GetByProject(_projectId, new IdeaFilter { Status = IdeaStatus.Archived }).Any(i => i.IdeaId == id));

        Assert.IsTrue(_ideas.GetByProject(_projectId, new IdeaFilter { Tag = "itest-alpha" }).Any(i => i.IdeaId == id));
        Assert.IsFalse(_ideas.GetByProject(_projectId, new IdeaFilter { Tag = "ITest-Gamma" }).Any(i => i.IdeaId == id));
    }

    [TestMethod]
    public void GetByProject_ReturnsOnlyIdeasOfThatProject()
    {
        long otherProjectId = ScalarLong(
            $"INSERT INTO projects (project_name, owner_id) VALUES ('ITest other project', {_userId}) RETURNING project_id");
        long mine = _ideas.Create(_projectId, FullDraft(), _userId);
        long theirs = _ideas.Create(otherProjectId, FullDraft(), _userId); // cùng tên tag ở Project khác

        Assert.IsTrue(_ideas.GetByProject(_projectId, new IdeaFilter()).Any(i => i.IdeaId == mine));
        Assert.IsFalse(_ideas.GetByProject(_projectId, new IdeaFilter()).Any(i => i.IdeaId == theirs));
        Assert.IsFalse(_ideas.GetByProject(otherProjectId, new IdeaFilter()).Any(i => i.IdeaId == mine));

        // Cùng tên tag nhưng là 2 dòng tags khác nhau, mỗi Idea chỉ gắn tag của Project mình.
        Assert.AreEqual(2, ScalarLong("SELECT COUNT(*) FROM tags WHERE name = 'ITest-Alpha'"));
        Assert.AreEqual(1, ScalarLong($"SELECT COUNT(*) FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id WHERE it.idea_id = {theirs} AND t.project_id = {otherProjectId} AND t.name = 'ITest-Alpha'"));
        Assert.AreEqual(0, ScalarLong($"SELECT COUNT(*) FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id JOIN ideas i ON i.idea_id = it.idea_id WHERE t.project_id <> i.project_id"));
    }

    [TestMethod]
    public void GetProjectTags_ReturnsTagsOfThatProject()
    {
        _ideas.Create(_projectId, FullDraft(), _userId);

        List<string> tags = _ideas.GetProjectTags(_projectId);

        Assert.IsTrue(tags.Contains("ITest-Alpha") && tags.Contains("ITest-Beta"));
    }

    [TestMethod]
    public void GetById_ForUnknownIdea_ReturnsNull()
    {
        Assert.IsNull(_ideas.GetById(long.MaxValue));
    }

    private List<Idea> Search(string text) =>
        _ideas.GetByProject(_projectId, new IdeaFilter { SearchText = text });

    private static IdeaDraft FullDraft() => new()
    {
        Title = "Idea integration test",
        Description = "Mô tả tiếng Việt có dấu",
        Note = "Ghi chú nội bộ",
        Status = IdeaStatus.Backlog,
        Tags = new List<string> { "ITest-Alpha", "ITest-Beta" },
    };

    private long ScalarLong(string sql)
    {
        using var cmd = _uow.CreateCommand(sql);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }
}
