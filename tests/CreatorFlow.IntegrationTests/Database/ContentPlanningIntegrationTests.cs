using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Database;

/// <summary>
/// SCRUM-32: các cột script, content_type, planned_publish_at của bảng contents được ghi và đọc lại đúng trên PostgreSQL thật
/// (cần đã chạy 01_schema.sql + 02_seed.sql). Mỗi test chạy trong 1 transaction và Rollback ở cuối nên không để lại dữ liệu.
/// </summary>
[TestClass]
public sealed class ContentPlanningIntegrationTests
{
    private NpgsqlUnitOfWork _uow = null!;
    private ContentDetailsRepository _details = null!;
    private long _projectId;
    private long _userId;

    [TestInitialize]
    public void Setup()
    {
        // Cùng quy ước với PlanRepositoryIntegrationTests: chỉ chạy khi đặt biến môi trường trỏ tới DB test đã đồng ý.
        string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive("Set CREATORFLOW_TEST_CONNECTION_STRING for an explicitly approved test database.");
        }

        _uow = new NpgsqlUnitOfWork(connectionString);
        _details = new ContentDetailsRepository(_uow);

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
    public void Create_StoresAllPlanningColumns_AndGetDetailReadsThemBack()
    {
        long id = _details.Create(_projectId, ContentStatus.Script, FullDraft(), _userId);

        Content? detail = _details.GetDetail(id);

        Assert.IsNotNull(detail);
        Assert.AreEqual(id, detail.ContentId);
        Assert.AreEqual(_projectId, detail.ProjectId);
        Assert.AreEqual("Planning integration test", detail.Title);
        Assert.AreEqual("Hook mở đầu", detail.Description);
        Assert.AreEqual("[Hook] Mở hộp\n[Body] Thử âm thanh\n[CTA] Theo dõi — tiếng Việt có dấu", detail.Script);
        Assert.AreEqual("Long video", detail.ContentType);
        Assert.AreEqual(Priority.High, detail.Priority);
        Assert.AreEqual(ContentStatus.Script, detail.Status);
        Assert.AreEqual(new DateTime(2026, 11, 1), detail.Deadline!.Value.Date);
        Assert.AreEqual(new DateTime(2026, 11, 5), detail.PlannedPublishAt!.Value.Date);
        Assert.AreEqual(_userId, detail.CreatedByUserId);
    }

    [TestMethod]
    public void Update_ChangesPlanningColumns()
    {
        long id = _details.Create(_projectId, ContentStatus.Script, FullDraft(), _userId);

        var edited = FullDraft();
        edited.Title = "Planning integration test (edited)";
        edited.Script = "Kịch bản đã sửa";
        edited.ContentType = "Reel";
        edited.Priority = Priority.Low;
        edited.Deadline = new DateTime(2026, 12, 1);
        edited.PlannedPublishAt = new DateTime(2026, 12, 3);
        _details.Update(id, edited);

        Content detail = _details.GetDetail(id)!;
        Assert.AreEqual("Planning integration test (edited)", detail.Title);
        Assert.AreEqual("Kịch bản đã sửa", detail.Script);
        Assert.AreEqual("Reel", detail.ContentType);
        Assert.AreEqual(Priority.Low, detail.Priority);
        Assert.AreEqual(new DateTime(2026, 12, 1), detail.Deadline!.Value.Date);
        Assert.AreEqual(new DateTime(2026, 12, 3), detail.PlannedPublishAt!.Value.Date);
    }

    [TestMethod]
    public void Update_ClearingScriptAndPlannedDate_StoresNull()
    {
        long id = _details.Create(_projectId, ContentStatus.Script, FullDraft(), _userId);

        var edited = FullDraft();
        edited.Script = string.Empty;
        edited.PlannedPublishAt = null;
        _details.Update(id, edited);

        Assert.AreEqual(1L, ScalarLong($"SELECT COUNT(*) FROM contents WHERE content_id = {id} AND script IS NULL AND planned_publish_at IS NULL"));
        Content detail = _details.GetDetail(id)!;
        Assert.IsNull(detail.Script);
        Assert.IsNull(detail.PlannedPublishAt);
    }

    [TestMethod]
    public void Update_DoesNotTouchStatusOrCreator()
    {
        long id = _details.Create(_projectId, ContentStatus.Editing, FullDraft(), _userId);

        _details.Update(id, FullDraft());

        Content detail = _details.GetDetail(id)!;
        Assert.AreEqual(ContentStatus.Editing, detail.Status);
        Assert.AreEqual(_userId, detail.CreatedByUserId);
    }

    [TestMethod]
    public void GetDetail_ForUnknownContent_ReturnsNull()
    {
        Assert.IsNull(_details.GetDetail(long.MaxValue));
    }

    private static ContentDraft FullDraft() => new()
    {
        Title = "Planning integration test",
        Description = "Hook mở đầu",
        Script = "[Hook] Mở hộp\n[Body] Thử âm thanh\n[CTA] Theo dõi — tiếng Việt có dấu",
        ContentType = "Long video",
        Priority = Priority.High,
        Deadline = new DateTime(2026, 11, 1),
        PlannedPublishAt = new DateTime(2026, 11, 5),
        Platforms = new List<string> { "YouTube" },
    };

    private long ScalarLong(string sql)
    {
        using var cmd = _uow.CreateCommand(sql);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }
}
