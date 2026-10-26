using CreatorFlow.Api.Models.Ideas;
using CreatorFlow.Api.Repositories.Ideas;
using CreatorFlow.Api.Repositories.ContentDetail;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fixture = CreatorFlow.Api.Tests.Database.ContentReferenceRepositoryIntegrationTests.Fixture;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
public sealed class IdeaPersistenceTests
{
    private static IdeaDraft Draft() => new("Idea integration test", "Mô tả tiếng Việt có dấu", "Ghi chú nội bộ",
        IdeaStatus.Backlog, ["itest-alpha", "itest-beta"]);

    [TestMethod]
    public async Task Crud_PreservesFieldsAndKeptTags_AndRemovesTagLinksOnDelete()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new IdeaRepository(fixture.Factory);
        var draft = Draft();
        long id = await repository.CreateAsync(fixture.ProjectId, draft, fixture.UserId);
        var idea = await repository.GetByIdAsync(id);
        Assert.IsNotNull(idea);
        Assert.AreEqual(fixture.ProjectId, idea.ProjectId);
        Assert.AreEqual(fixture.UserId, idea.CreatedByUserId);
        Assert.AreEqual(draft.Title, idea.Title);
        Assert.AreEqual(draft.Description, idea.Description);
        Assert.AreEqual(draft.Note, idea.Note);
        Assert.AreEqual(draft.Status, idea.Status);
        CollectionAssert.AreEquivalent(draft.Tags.ToArray(), idea.Tags.ToArray());
        long keptTag = await fixture.ScalarAsync<long>("SELECT tag_id FROM tags WHERE project_id=@p AND name='itest-alpha'", ("p", fixture.ProjectId));
        await repository.UpdateAsync(id, draft with { Title = "Edited", Description = "", Note = "", Status = IdeaStatus.Draft,
            Tags = ["itest-alpha", "itest-gamma"] });
        idea = await repository.GetByIdAsync(id);
        Assert.IsNotNull(idea);
        Assert.AreEqual("Edited", idea.Title);
        Assert.AreEqual("", idea.Description);
        Assert.AreEqual("", idea.Note);
        Assert.AreEqual(IdeaStatus.Draft, idea.Status);
        CollectionAssert.AreEquivalent(new[] { "itest-alpha", "itest-gamma" }, idea.Tags.ToArray());
        Assert.AreEqual(keptTag, await fixture.ScalarAsync<long>("SELECT tag_id FROM tags WHERE project_id=@p AND name='itest-alpha'", ("p", fixture.ProjectId)));
        await repository.UpdateAsync(id, draft with { Tags = [] });
        Assert.AreEqual(0, (await repository.GetByIdAsync(id))!.Tags.Count);
        await repository.DeleteAsync(id);
        Assert.IsNull(await repository.GetByIdAsync(id));
        Assert.AreEqual(0L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM idea_tags WHERE idea_id=@id", ("id", id)));
        Assert.IsNull(await repository.GetByIdAsync(long.MaxValue));
        long emptyId = await repository.CreateAsync(fixture.ProjectId, draft with { Description = "", Note = "" }, fixture.UserId);
        Assert.AreEqual("", (await repository.GetByIdAsync(emptyId))!.Description);
        Assert.AreEqual("", (await repository.GetByIdAsync(emptyId))!.Note);
    }

    [TestMethod]
    public async Task SearchAndFilters_RespectFieldsLiteralWildcardsAndProjectBoundary()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var other = await Fixture.CreateAsync();
        var repository = new IdeaRepository(fixture.Factory);
        long id = await repository.CreateAsync(fixture.ProjectId, Draft(), fixture.UserId);
        long otherId = await repository.CreateAsync(other.ProjectId, Draft() with { Tags = ["other-only"] }, other.UserId);
        foreach (string term in new[] { "INTEGRATION TEST", "tiếng Việt có dấu", "nội bộ", "itest-beta" })
            Assert.IsTrue((await repository.GetByProjectAsync(fixture.ProjectId, new(term, null, null))).Any(i => i.IdeaId == id), term);
        Assert.AreEqual(0, (await repository.GetByProjectAsync(fixture.ProjectId, new("no-match-xyz", null, null))).Count);
        var filtered = await repository.GetByProjectAsync(fixture.ProjectId, new("", IdeaStatus.Backlog, "itest-alpha"));
        Assert.AreEqual(id, filtered.Single().IdeaId);
        Assert.AreEqual(0, (await repository.GetByProjectAsync(fixture.ProjectId, new("", IdeaStatus.Archived, null))).Count);
        Assert.AreEqual(0, (await repository.GetByProjectAsync(fixture.ProjectId, new("", null, "missing-tag"))).Count);
        Assert.IsFalse((await repository.GetByProjectAsync(fixture.ProjectId, new("", null, null))).Any(i => i.IdeaId == otherId));
        Assert.IsFalse((await repository.GetByProjectAsync(other.ProjectId, new("", null, null))).Any(i => i.IdeaId == id));
        CollectionAssert.AreEquivalent(new[] { "itest-alpha", "itest-beta" }, (await repository.GetProjectTagsAsync(fixture.ProjectId)).ToArray());
        CollectionAssert.AreEquivalent(new[] { "other-only" }, (await repository.GetProjectTagsAsync(other.ProjectId)).ToArray());
        long literalId = await repository.CreateAsync(fixture.ProjectId, Draft() with { Title = "Giảm 50% phí_dịch vụ" }, fixture.UserId);
        foreach (string term in new[] { "50%", "phí_dịch" })
            Assert.IsTrue((await repository.GetByProjectAsync(fixture.ProjectId, new(term, null, null))).Any(i => i.IdeaId == literalId));
        foreach (string term in new[] { "50%x", "phí-dịch" })
            Assert.IsFalse((await repository.GetByProjectAsync(fixture.ProjectId, new(term, null, null))).Any(i => i.IdeaId == literalId));
    }

    [TestMethod]
    [DataRow(IdeaStatus.Draft)]
    [DataRow(IdeaStatus.Backlog)]
    public async Task Conversion_PreservesSourceTagsAndHistory_IsNotDuplicated_AndContentSurvivesIdeaDelete(IdeaStatus status)
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new IdeaRepository(fixture.Factory);
        long id = await repository.CreateAsync(fixture.ProjectId, Draft() with { Status = status }, fixture.UserId);
        long? contentId = await repository.ConvertToContentAsync(id, "Short video", fixture.UserId, "Conversion integration test");
        Assert.IsNotNull(contentId);
        var content = await new ContentDetailRepository(fixture.Factory).GetDetailAsync(contentId.Value);
        Assert.IsNotNull(content);
        Assert.AreEqual(fixture.ProjectId, content.ProjectId);
        Assert.AreEqual(fixture.UserId, content.CreatedBy);
        Assert.AreEqual(Draft().Title, content.Title);
        Assert.AreEqual(Draft().Description, content.Description);
        Assert.AreEqual("Short video", content.ContentType);
        Assert.AreEqual("Idea", content.Status);
        Assert.AreEqual("Medium", content.Priority);
        Assert.AreEqual(id, await fixture.ScalarAsync<long>("SELECT source_idea_id FROM contents WHERE content_id=@id", ("id", contentId.Value)));
        Assert.AreEqual(2L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM content_tags WHERE content_id=@id", ("id", contentId.Value)));
        Assert.AreEqual(0L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM content_tags ct JOIN tags t USING(tag_id) WHERE ct.content_id=@id AND t.project_id<>@p", ("id", contentId.Value), ("p", fixture.ProjectId)));
        Assert.AreEqual(1L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM content_status_history WHERE content_id=@id", ("id", contentId.Value)));
        var idea = await repository.GetByIdAsync(id);
        Assert.IsNotNull(idea);
        Assert.AreEqual(IdeaStatus.Converted, idea.Status);
        Assert.AreEqual(contentId, idea.ConvertedContentId);
        Assert.AreEqual(Draft().Note, idea.Note);
        CollectionAssert.AreEquivalent(Draft().Tags.ToArray(), idea.Tags.ToArray());
        Assert.IsNull(await repository.ConvertToContentAsync(id, "Short video", fixture.UserId, "Must not duplicate"));
        Assert.AreEqual(1L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM contents WHERE source_idea_id=@id", ("id", id)));
        await repository.DeleteAsync(id);
        Assert.IsNotNull(await new ContentDetailRepository(fixture.Factory).GetDetailAsync(contentId.Value));
        Assert.AreEqual(0L, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM contents WHERE content_id=@id AND source_idea_id IS NOT NULL", ("id", contentId.Value)));
    }

    [TestMethod]
    public async Task Conversion_ArchivedOrUnknownIdea_WritesNothing()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new IdeaRepository(fixture.Factory);
        long id = await repository.CreateAsync(fixture.ProjectId, Draft() with { Status = IdeaStatus.Archived }, fixture.UserId);
        long before = await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM contents WHERE project_id=@p", ("p", fixture.ProjectId));
        Assert.IsNull(await repository.ConvertToContentAsync(id, "Short video", fixture.UserId, "Must not convert"));
        Assert.IsNull(await repository.ConvertToContentAsync(long.MaxValue, "Short video", fixture.UserId, "Must not convert"));
        Assert.AreEqual(before, await fixture.ScalarAsync<long>("SELECT COUNT(*) FROM contents WHERE project_id=@p", ("p", fixture.ProjectId)));
    }
}
