using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.IntegrationTests.Database;

/// <summary>
/// Kiểm tra content_platforms trên PostgreSQL thật (cần đã chạy 01_schema.sql + 02_seed.sql).
/// Mỗi test chạy trong 1 transaction và Rollback ở cuối nên không để lại dữ liệu.
/// </summary>
[TestClass]
public sealed class ContentPlatformIntegrationTests
{
    private NpgsqlUnitOfWork _uow = null!;
    private ContentDetailsRepository _details = null!;
    private PlatformRepository _platforms = null!;
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
        _platforms = new PlatformRepository(_uow);

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
    public void GetActive_ReturnsTheFourSeededPlatforms()
    {
        string names = string.Join(",", _platforms.GetActive().Select(p => p.Name).OrderBy(n => n));

        Assert.AreEqual("Facebook,Instagram,TikTok,YouTube", names);
    }

    [TestMethod]
    public void Create_WithTwoPlatforms_ReadsBackBoth()
    {
        long id = _details.Create(_projectId, ContentStatus.Editing, Draft("TikTok", "YouTube"), _userId);

        Assert.AreEqual("TikTok,YouTube", PlatformNames(id));
    }

    [TestMethod]
    public void Update_DroppingOnePlatform_KeepsRemainingPlatformAndItsMetrics()
    {
        long id = _details.Create(_projectId, ContentStatus.Editing, Draft("TikTok", "YouTube"), _userId);
        var before = _platforms.GetByContentId(id);
        long tikTokRowId = before.Single(cp => cp.PlatformName == "TikTok").ContentPlatformId;
        foreach (var cp in before) InsertMetric(cp.ContentPlatformId);

        _details.Update(id, Draft("TikTok"));

        var after = _platforms.GetByContentId(id);
        Assert.AreEqual("TikTok", PlatformNames(id));
        Assert.AreEqual(tikTokRowId, after.Single().ContentPlatformId); // dòng được giữ nguyên, không xóa-chèn lại
        Assert.AreEqual(1L, ScalarLong($"SELECT COUNT(*) FROM content_metrics WHERE content_platform_id = {tikTokRowId}"));
    }

    [TestMethod]
    public void Insert_DuplicatePlatformForSameContent_IsRejectedByUniqueConstraint()
    {
        long id = _details.Create(_projectId, ContentStatus.Editing, Draft("TikTok"), _userId);

        var ex = Assert.ThrowsExactly<PostgresException>(() =>
        {
            using var cmd = _uow.CreateCommand(
                "INSERT INTO content_platforms (content_id, platform_id) " +
                "SELECT @id, platform_id FROM platforms WHERE name = 'TikTok'");
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        });
        Assert.AreEqual("23505", ex.SqlState);
    }

    [TestMethod]
    public void GetBoardCards_ShowsAllPlatformsOfAContent()
    {
        long id = _details.Create(_projectId, ContentStatus.Editing, Draft("TikTok", "YouTube"), _userId);

        var card = new BoardRepository(_uow).GetBoardCards(_projectId).Single(c => c.ContentId == id);

        Assert.AreEqual("TikTok,YouTube", string.Join(",", card.Platforms));
    }

    private static ContentDraft Draft(params string[] platforms) =>
        new() { Title = "Platform integration test", Platforms = platforms.ToList() };

    private string PlatformNames(long contentId) =>
        string.Join(",", _platforms.GetByContentId(contentId).Select(cp => cp.PlatformName).OrderBy(n => n));

    private void InsertMetric(long contentPlatformId)
    {
        using var cmd = _uow.CreateCommand("INSERT INTO content_metrics (content_platform_id, views) VALUES (@id, 100)");
        cmd.Parameters.AddWithValue("id", contentPlatformId);
        cmd.ExecuteNonQuery();
    }

    private long ScalarLong(string sql)
    {
        using var cmd = _uow.CreateCommand(sql);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }
}