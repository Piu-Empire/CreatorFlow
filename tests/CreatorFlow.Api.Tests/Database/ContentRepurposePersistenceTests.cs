using CreatorFlow.Api.Models.ContentDetail;
using CreatorFlow.Api.Repositories.ContentDetail;
using CreatorFlow.Contracts.ContentDetail;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Fixture = CreatorFlow.Api.Tests.Database.ContentReferenceRepositoryIntegrationTests.Fixture;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("ContentRepurposeDatabase")]
public sealed class ContentRepurposePersistenceTests
{
    [TestMethod]
    public async Task CreateRepurposedContent_PersistsScriptAndRelation_WithoutChangingSource()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new ContentDetailRepository(fixture.Factory);
        IReadOnlyList<string> platforms = await repository.GetActivePlatformNamesAsync();
        Assert.IsTrue(platforms.Contains("TikTok"), "Disposable database must contain the TikTok platform.");
        Assert.IsTrue(platforms.Contains("YouTube"), "Disposable database must contain the YouTube platform.");

        const string sourceTitle = "Repurpose source";
        const string sourceDescription = "Original description";
        const string sourceScript = "Original script";
        await fixture.ScalarAsync<long>("""
            UPDATE contents SET title=@title, description=@description, script=@script
            WHERE content_id=@id RETURNING content_id;
            """, ("title", sourceTitle), ("description", sourceDescription), ("script", sourceScript), ("id", fixture.ContentId));
        await fixture.ScalarAsync<long>("""
            INSERT INTO content_platforms(content_id, platform_id)
            SELECT @content_id, platform_id FROM platforms WHERE name='TikTok' AND is_active
            RETURNING content_platform_id;
            """, ("content_id", fixture.ContentId));

        ContentDetailRecord sourceBefore = (await repository.GetDetailAsync(fixture.ContentId))!;
        string? sourceScriptBefore = (await repository.GetScriptAsync(fixture.ContentId))!.Script;
        var details = new ContentUpdate("Repurposed title", "Repurposed description", "Reel", "MEDIUM",
            null, null, ["YouTube"], SprintCatalog.DefaultSprint);
        const string derivedScript = "[Hook] Adapted script\n[CTA] Follow";

        long derivedId = await repository.CreateAsync(new NewContent(fixture.ProjectId, "IDEA", details,
            fixture.UserId, fixture.ContentId, derivedScript));

        ContentDetailRecord derived = (await repository.GetDetailAsync(derivedId))!;
        Assert.AreEqual(fixture.ContentId, derived.SourceContentId);
        Assert.AreEqual(fixture.ProjectId, derived.ProjectId);
        Assert.AreEqual("Repurposed title", derived.Title);
        Assert.AreEqual("Idea", derived.Status);
        CollectionAssert.AreEqual(new[] { "YouTube" }, derived.Platforms.ToArray());
        Assert.AreEqual(derivedScript, (await repository.GetScriptAsync(derivedId))!.Script);
        Assert.AreEqual("REPURPOSED", await fixture.ScalarAsync<string>("""
            SELECT relation_type::text FROM content_relations
            WHERE source_content_id=@source AND derived_content_id=@derived;
            """, ("source", fixture.ContentId), ("derived", derivedId)));

        ContentDetailRecord sourceAfter = (await repository.GetDetailAsync(fixture.ContentId))!;
        Assert.AreEqual(sourceBefore.Title, sourceAfter.Title);
        Assert.AreEqual(sourceBefore.Description, sourceAfter.Description);
        Assert.AreEqual(sourceBefore.Status, sourceAfter.Status);
        Assert.AreEqual(sourceBefore.Priority, sourceAfter.Priority);
        Assert.AreEqual(sourceBefore.CreatedBy, sourceAfter.CreatedBy);
        Assert.IsNull(sourceAfter.SourceContentId);
        CollectionAssert.AreEqual(sourceBefore.Platforms.ToArray(), sourceAfter.Platforms.ToArray());
        Assert.AreEqual(sourceScriptBefore, (await repository.GetScriptAsync(fixture.ContentId))!.Script);
    }

    [TestMethod]
    public async Task CreateRepurposedContent_RollsBackContentWhenRelationInsertFails()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new ContentDetailRepository(fixture.Factory);
        string title = "Failed repurpose " + Guid.NewGuid().ToString("N");
        var details = new ContentUpdate(title, "", "Short video", "MEDIUM", null, null, [], SprintCatalog.DefaultSprint);

        try
        {
            await repository.CreateAsync(new NewContent(fixture.ProjectId, "IDEA", details,
                fixture.UserId, long.MaxValue, "Script that must roll back"));
            Assert.Fail("An unknown source must violate the content_relations foreign key.");
        }
        catch (PostgresException error)
        {
            Assert.AreEqual("23503", error.SqlState);
        }

        Assert.AreEqual(0L, await fixture.ScalarAsync<long>("""
            SELECT COUNT(*) FROM contents WHERE project_id=@project AND title=@title;
            """, ("project", fixture.ProjectId), ("title", title)));
    }
}
