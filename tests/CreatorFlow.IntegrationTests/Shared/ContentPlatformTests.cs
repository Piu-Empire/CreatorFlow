using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>Content ↔ nhiều Platform qua ContentPlatforms, chạy trên repository InMemory (không cần PostgreSQL).</summary>
[TestClass]
public sealed class ContentPlatformTests
{
    private const long ProjectId = 1;
    private const long OwnerUserId = 1;

    private InMemoryPlatformRepository _platformRepo = null!;
    private ContentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _platformRepo = new InMemoryPlatformRepository();
        _service = new ContentService(
            new InMemoryContentRepository(),
            new InMemoryContentDetailsRepository(),
            new InMemoryContentStatusHistoryRepository(),
            new InMemoryProjectMemberRepository(),
            _platformRepo,
            new InMemoryUnitOfWork(), new InMemoryMyTaskRepository());
    }

    private static ContentDraft Draft(params string[] platforms) => new()
    {
        Title = "Content đa nền tảng " + Guid.NewGuid(),
        Platforms = platforms.ToList(),
    };

    private string[] PlatformNames(long contentId) =>
        _platformRepo.GetByContentId(contentId).Select(p => p.PlatformName).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    [TestMethod]
    public void Create_WithMultiplePlatforms_StoresAllPlatforms()
    {
        long id = _service.Create(ProjectId, ContentStatus.Idea, Draft("TikTok", "Instagram"), OwnerUserId);

        CollectionAssert.AreEqual(new[] { "Instagram", "TikTok" }, PlatformNames(id));
    }

    [TestMethod]
    public void Update_ChangesPlatforms_AddsAndRemovesPlatforms()
    {
        long id = _service.Create(ProjectId, ContentStatus.Idea, Draft("TikTok", "Instagram"), OwnerUserId);

        _service.Update(id, Draft("TikTok", "YouTube", "Facebook"), OwnerUserId);

        CollectionAssert.AreEqual(new[] { "Facebook", "TikTok", "YouTube" }, PlatformNames(id));
    }

    [TestMethod]
    public void Create_WithMixedCaseAndDuplicates_StoresDistinctCanonicalNames()
    {
        long id = _service.Create(ProjectId, ContentStatus.Idea, Draft("tiktok", " TikTok ", "YOUTUBE"), OwnerUserId);

        CollectionAssert.AreEqual(new[] { "TikTok", "YouTube" }, PlatformNames(id));
    }

    [TestMethod]
    public void Create_WithUnknownPlatform_ThrowsValidationException()
    {
        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Idea, Draft("TikTok", "Zalo"), OwnerUserId));
    }

    [TestMethod]
    public void NewContentPlatform_DefaultsToPlannedWithoutPostUrl()
    {
        long id = _service.Create(ProjectId, ContentStatus.Idea, Draft("TikTok"), OwnerUserId);

        ContentPlatform platform = _platformRepo.GetByContentId(id).Single();

        Assert.AreEqual(PublicationStatus.Planned, platform.PublicationStatus);
        Assert.IsNull(platform.PostUrl);
    }
}
