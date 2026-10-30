using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-59: tạo Content kèm liên kết nguồn → phái sinh trên repository InMemory.</summary>
[TestClass]
public sealed class ContentRepurposeRelationTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;

    private IPlatformRepository _platforms = null!;
    private ContentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _platforms = new InMemoryPlatformRepository();
        _service = new ContentService(
            new InMemoryContentRepository(),
            new InMemoryContentDetailsRepository(),
            new InMemoryContentStatusHistoryRepository(),
            new InMemoryProjectMemberRepository(),
            _platforms,
            new InMemoryUnitOfWork(),
            new InMemoryMyTaskRepository());
    }

    [TestMethod]
    public void Create_WithSource_StoresRepurposedRelationAndSourceId()
    {
        long sourceId = _service.Create(ProjectId, ContentStatus.Published, Draft("TikTok"), OwnerId);

        long derivedId = _service.Create(ProjectId, ContentStatus.Idea, Draft("Facebook", sourceId), OwnerId);

        InMemoryContentRelationRecord relation = InMemoryDataStore.ContentRelations
            .Last(r => r.DerivedContentId == derivedId);
        Assert.AreEqual(sourceId, relation.SourceContentId);
        Assert.AreEqual("REPURPOSED", relation.RelationType);
        Assert.AreEqual(OwnerId, relation.CreatedBy);

        Content detail = _service.GetDetail(derivedId, OwnerId);
        Assert.AreEqual(sourceId, detail.SourceContentId);
    }

    [TestMethod]
    public void Create_WithoutSource_DoesNotCreateRelation()
    {
        int before = InMemoryDataStore.ContentRelations.Count;

        long derivedId = _service.Create(ProjectId, ContentStatus.Idea, Draft("Facebook"), OwnerId);

        Assert.AreEqual(before, InMemoryDataStore.ContentRelations.Count);
        Assert.AreEqual(0, InMemoryDataStore.ContentRelations.Count(r => r.DerivedContentId == derivedId));
        Assert.IsNull(_service.GetDetail(derivedId, OwnerId).SourceContentId);
    }

    [TestMethod]
    public void Create_WithSourceFromAnotherProject_Throws()
    {
        long foreignId = InMemoryDataStore.Contents.Count == 0
            ? 1000
            : InMemoryDataStore.Contents.Max(c => c.Id) + 1;
        InMemoryDataStore.Contents.Add(new InMemoryContentRecord
        {
            Id = foreignId,
            ProjectId = 2,
            Title = "Content Project khác",
            CreatedByUserId = OwnerId,
        });

        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Idea, Draft("Facebook", foreignId), OwnerId));
    }

    private static ContentDraft Draft(string platform, long? sourceContentId = null) => new()
    {
        Title = "Nội dung test",
        Description = "Mô tả test",
        ContentType = ContentService.DefaultContentType,
        Platforms = new List<string> { platform },
        SourceContentId = sourceContentId,
    };
}
