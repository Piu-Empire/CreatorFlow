using CreatorFlow.Models;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>Quy tắc platform của ContentService, chạy trên repository InMemory (không cần PostgreSQL).</summary>
[TestClass]
public sealed class ContentServicePlatformTests
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
            new InMemoryUnitOfWork(), new InMemoryMyTaskRepository());
    }

    [TestMethod]
    public void Create_WithTwoPlatforms_ReadsBackBoth()
    {
        long id = _service.Create(ProjectId, ContentStatus.Editing, Draft("TikTok", "YouTube"), OwnerId);

        Assert.AreEqual("TikTok,YouTube", PlatformNames(id));
    }

    [TestMethod]
    public void Update_DroppingOnePlatform_KeepsTheOtherOne()
    {
        long id = _service.Create(ProjectId, ContentStatus.Editing, Draft("TikTok", "YouTube"), OwnerId);

        _service.Update(id, Draft("TikTok"), OwnerId);

        Assert.AreEqual("TikTok", PlatformNames(id));
    }

    [TestMethod]
    public void Create_WithDuplicatePlatformNames_StoresEachPlatformOnce()
    {
        long id = _service.Create(ProjectId, ContentStatus.Editing, Draft("YouTube", "youtube", "YOUTUBE"), OwnerId);

        Assert.AreEqual("YouTube", PlatformNames(id));
    }

    [TestMethod]
    public void Create_WithUnknownPlatform_Throws()
    {
        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Editing, Draft("MySpace"), OwnerId));
    }

    [TestMethod]
    public void Update_RemovingPublishedPlatform_ThrowsAndKeepsPlatforms()
    {
        long id = _service.Create(ProjectId, ContentStatus.Editing, Draft("TikTok", "YouTube"), OwnerId);
        InMemoryDataStore.Contents.First(c => c.Id == id).PublishedPlatforms.Add("TikTok");

        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Update(id, Draft("YouTube"), OwnerId));
        Assert.AreEqual("TikTok,YouTube", PlatformNames(id));

        // Bỏ platform chưa đăng thì vẫn được.
        _service.Update(id, Draft("TikTok"), OwnerId);
        Assert.AreEqual("TikTok", PlatformNames(id));
    }

    [TestMethod]
    public void GetAvailablePlatforms_ComesFromPlatformRepository()
    {
        string expected = string.Join(",", _platforms.GetActive().Select(p => p.Name));

        Assert.AreEqual(expected, string.Join(",", _service.GetAvailablePlatforms()));
    }

    private static ContentDraft Draft(params string[] platforms) =>
        new() { Title = "Platform test", Platforms = platforms.ToList() };

    private string PlatformNames(long contentId) =>
        string.Join(",", _platforms.GetByContentId(contentId).Select(cp => cp.PlatformName).OrderBy(n => n));
}
