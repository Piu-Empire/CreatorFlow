using CreatorFlow.Api.Models.ContentDetail;
using CreatorFlow.Api.Repositories.Board;
using CreatorFlow.Api.Repositories.ContentDetail;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Fixture = CreatorFlow.Api.Tests.Database.ContentReferenceRepositoryIntegrationTests.Fixture;

namespace CreatorFlow.Api.Tests.Database;

// Thay coverage repository WinForms đã loại bỏ bằng repository Backend thực tế, trên DB test opt-in.
[TestClass]
public sealed class ContentPlanningPersistenceTests
{
    [TestMethod]
    public async Task PlanningAndScript_CreateUpdateClear_ReadBackWithoutChangingStatusOrCreator()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new ContentDetailRepository(fixture.Factory);
        var draft = new ContentUpdate("Planning integration test", "Hook mở đầu", "Long video", "HIGH",
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), []);
        long id = await repository.CreateAsync(new NewContent(fixture.ProjectId, "SCRIPT", draft, fixture.UserId));
        Assert.IsTrue(await repository.TryUpdateScriptAsync(id, null, "[Hook] Tiếng Việt có dấu\n[CTA] Theo dõi"));
        var detail = await repository.GetDetailAsync(id);
        Assert.IsNotNull(detail);
        Assert.AreEqual(fixture.ProjectId, detail.ProjectId);
        Assert.AreEqual(draft.Title, detail.Title);
        Assert.AreEqual(draft.Description, detail.Description);
        Assert.AreEqual(draft.ContentType, detail.ContentType);
        Assert.AreEqual("High", detail.Priority);
        Assert.AreEqual(draft.Deadline, detail.Deadline);
        Assert.AreEqual(draft.PlannedPublishAt, detail.PlannedPublishAt);
        Assert.AreEqual("[Hook] Tiếng Việt có dấu\n[CTA] Theo dõi", (await repository.GetScriptAsync(id))!.Script);

        var edited = draft with { Title = "Edited", ContentType = "Reel", PriorityDb = "LOW",
            Deadline = new DateOnly(2026, 12, 1), PlannedPublishAt = new DateOnly(2026, 12, 3) };
        await repository.UpdateAsync(id, edited);
        detail = await repository.GetDetailAsync(id);
        Assert.IsNotNull(detail);
        Assert.AreEqual("Edited", detail.Title);
        Assert.AreEqual("Reel", detail.ContentType);
        Assert.AreEqual("Low", detail.Priority);
        Assert.AreEqual(edited.Deadline, detail.Deadline);
        Assert.AreEqual(edited.PlannedPublishAt, detail.PlannedPublishAt);
        Assert.AreEqual("Script", detail.Status);
        Assert.AreEqual(fixture.UserId, detail.CreatedBy);
        Assert.IsFalse(await repository.TryUpdateScriptAsync(id, "stale version", "Must not overwrite"));
        Assert.IsTrue(await repository.TryUpdateScriptAsync(id, "[Hook] Tiếng Việt có dấu\n[CTA] Theo dõi", null));
        await repository.UpdateAsync(id, edited with { Description = "", PlannedPublishAt = null });
        Assert.IsNull((await repository.GetScriptAsync(id))!.Script);
        detail = await repository.GetDetailAsync(id);
        Assert.IsNotNull(detail);
        Assert.IsNull(detail.PlannedPublishAt);
        Assert.AreEqual("", detail.Description);
        Assert.AreEqual("Script", detail.Status);
        Assert.AreEqual(fixture.UserId, detail.CreatedBy);
        Assert.IsNull(await repository.GetDetailAsync(long.MaxValue));
    }

    [TestMethod]
    public async Task Platforms_UpdatePreservesKeptLinkAndMetrics_AndBoardReadsAllPlatforms()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new ContentDetailRepository(fixture.Factory);
        var active = await repository.GetActivePlatformNamesAsync();
        foreach (string name in new[] { "TikTok", "YouTube", "Facebook", "Instagram" })
            Assert.IsTrue(active.Contains(name), "Disposable test DB must contain the standard seeded platforms.");
        var draft = new ContentUpdate("Platform integration test", "", "Short video", "MEDIUM", null, null,
            ["TikTok", "YouTube"]);
        long id = await repository.CreateAsync(new NewContent(fixture.ProjectId, "EDITING", draft, fixture.UserId));
        var detail = await repository.GetDetailAsync(id);
        Assert.IsNotNull(detail);
        CollectionAssert.AreEquivalent(new[] { "TikTok", "YouTube" }, detail.Platforms.ToArray());
        var board = await new BoardCardRepository(fixture.Factory).GetCardsAsync(fixture.ProjectId);
        CollectionAssert.AreEquivalent(new[] { "TikTok", "YouTube" }, board.Single(c => c.ContentId == id).Platforms.ToArray());

        long linkId = await fixture.ScalarAsync<long>("""
            SELECT cp.content_platform_id FROM content_platforms cp JOIN platforms p USING(platform_id)
            WHERE cp.content_id=@id AND p.name='TikTok'
            """, ("id", id));
        await fixture.ScalarAsync<long>("""
            INSERT INTO content_metrics(content_platform_id,views) VALUES(@id,100) RETURNING metric_id
            """, ("id", linkId));
        await repository.UpdateAsync(id, draft with { Platforms = ["TikTok"] });
        detail = await repository.GetDetailAsync(id);
        Assert.IsNotNull(detail);
        CollectionAssert.AreEqual(new[] { "TikTok" }, detail.Platforms.ToArray());
        Assert.AreEqual(1L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM content_platforms WHERE content_platform_id=@id", ("id", linkId)));
        Assert.AreEqual(1L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM content_metrics WHERE content_platform_id=@id", ("id", linkId)));

        try
        {
            await fixture.ScalarAsync<long>("""
                INSERT INTO content_platforms(content_id,platform_id)
                SELECT @id,platform_id FROM platforms WHERE name='TikTok' RETURNING content_platform_id
                """, ("id", id));
            Assert.Fail("The unique constraint must reject duplicate platforms for a content.");
        }
        catch (PostgresException error) { Assert.AreEqual("23505", error.SqlState); }
    }
}
