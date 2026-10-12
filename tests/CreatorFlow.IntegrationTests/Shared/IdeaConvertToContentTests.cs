using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// SCRUM-31 — Chuyển Idea thành Content: mang thông tin sang Content, không làm mất Idea gốc, truy vết được Idea nguồn,
/// phân quyền và các trạng thái không cho chuyển. Chạy trên repository InMemory (không cần PostgreSQL).
/// Thành viên Project #1 (InMemoryDataStore): User 1 = Owner, 2 = Creator A, 3 = Manager, 4 = Creator B.
/// </summary>
[TestClass]
public sealed class IdeaConvertToContentTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;
    private const long CreatorId = 2;
    private const long ManagerId = 3;
    private const long OtherCreatorId = 4;
    private const long NonMemberId = 99;

    private InMemoryIdeaRepository _repo = null!;
    private IdeaService _service = null!;
    private readonly List<long> _createdContentIds = new();

    [TestInitialize]
    public void Setup()
    {
        _repo = new InMemoryIdeaRepository();
        _service = new IdeaService(_repo, new InMemoryProjectMemberRepository(), new InMemoryContentStatusHistoryRepository(), new InMemoryUnitOfWork());
    }

    [TestCleanup]
    public void Cleanup()
    {
        // InMemoryDataStore là static: gỡ Content test đã tạo để không ảnh hưởng test khác.
        InMemoryDataStore.Contents.RemoveAll(c => _createdContentIds.Contains(c.Id));
        InMemoryDataStore.StatusHistory.RemoveAll(h => _createdContentIds.Contains(h.ContentId));
    }

    // ---------- Mang thông tin + giữ liên kết nguồn ----------

    [TestMethod]
    public void Convert_CreatesContentWithIdeaInfo_AndKeepsSourceLink()
    {
        long ideaId = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), ManagerId);

        long contentId = Convert(ideaId, ManagerId);

        InMemoryContentRecord content = InMemoryDataStore.Contents.Single(c => c.Id == contentId);
        Assert.AreEqual("Series TikTok mới", content.Title);
        Assert.AreEqual("Chuỗi video ngắn", content.Description);
        Assert.AreEqual(ProjectId, content.ProjectId);
        Assert.AreEqual(ContentStatus.Idea, content.Status);
        Assert.AreEqual(ContentService.DefaultContentType, content.ContentType);
        Assert.AreEqual(ManagerId, content.CreatedByUserId);
        Assert.AreEqual(ideaId, content.IdeaId);
        CollectionAssert.AreEquivalent(new[] { "TikTok", "Short-form" }, content.Tags);
    }

    [TestMethod]
    public void Convert_KeepsOriginalIdea_MarksItConvertedAndLinksBackToContent()
    {
        long ideaId = _service.Create(ProjectId, FullDraft(IdeaStatus.Draft), OwnerId);

        long contentId = Convert(ideaId, OwnerId);

        Idea idea = _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == ideaId);
        Assert.AreEqual(IdeaStatus.Converted, idea.Status);
        Assert.AreEqual(contentId, idea.ConvertedContentId);
        Assert.AreEqual("Series TikTok mới", idea.Title);
        Assert.AreEqual("Chuỗi video ngắn", idea.Description);
        Assert.AreEqual("Ghi chú nội bộ", idea.Note);
        CollectionAssert.AreEquivalent(new[] { "TikTok", "Short-form" }, idea.Tags);
    }

    [TestMethod]
    public void Convert_WritesHistoryNoteNamingTheSourceIdea()
    {
        long ideaId = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), OwnerId);
        string code = _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == ideaId).Code;

        long contentId = Convert(ideaId, OwnerId);

        ContentStatusHistory history = InMemoryDataStore.StatusHistory.Single(h => h.ContentId == contentId);
        Assert.AreEqual(ContentStatus.Idea, history.ToStatus);
        Assert.AreEqual(OwnerId, history.ChangedByUserId);
        Assert.IsTrue(history.Note!.Contains(code));
    }

    [TestMethod]
    public void Convert_DoesNotChangeOtherIdeas()
    {
        long converted = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), OwnerId);
        long untouched = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), OwnerId);

        Convert(converted, OwnerId);

        Idea other = _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == untouched);
        Assert.AreEqual(IdeaStatus.Backlog, other.Status);
        Assert.IsNull(other.ConvertedContentId);
    }

    // ---------- Trạng thái không cho chuyển ----------

    [TestMethod]
    public void Convert_AlreadyConvertedIdea_Throws_AndCreatesNoSecondContent()
    {
        long ideaId = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), OwnerId);
        Convert(ideaId, OwnerId);
        int contentCount = InMemoryDataStore.Contents.Count;

        Assert.ThrowsExactly<IdeaValidationException>(() => _service.ConvertToContent(ideaId, OwnerId));
        Assert.AreEqual(contentCount, InMemoryDataStore.Contents.Count);
    }

    [TestMethod]
    public void Convert_ArchivedIdea_Throws()
    {
        long ideaId = _service.Create(ProjectId, FullDraft(IdeaStatus.Archived), OwnerId);
        int contentCount = InMemoryDataStore.Contents.Count;

        Assert.ThrowsExactly<IdeaValidationException>(() => _service.ConvertToContent(ideaId, OwnerId));
        Assert.AreEqual(contentCount, InMemoryDataStore.Contents.Count);
        Assert.AreEqual(IdeaStatus.Archived, _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == ideaId).Status);
    }

    [TestMethod]
    public void Convert_UnknownIdea_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _service.ConvertToContent(9999, OwnerId));
    }

    [TestMethod]
    public void Convert_TitleLongerThanContentLimit_ThrowsAndLeavesIdeaUnchanged()
    {
        var draft = FullDraft(IdeaStatus.Backlog);
        draft.Title = new string('a', ContentService.MaxTitleLength + 1); // hợp lệ với Idea (tối đa 250) nhưng quá dài với Content
        long ideaId = _service.Create(ProjectId, draft, OwnerId);
        int contentCount = InMemoryDataStore.Contents.Count;

        Assert.ThrowsExactly<IdeaValidationException>(() => _service.ConvertToContent(ideaId, OwnerId));
        Assert.AreEqual(contentCount, InMemoryDataStore.Contents.Count);
        Assert.AreEqual(IdeaStatus.Backlog, _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == ideaId).Status);
    }

    // ---------- Phân quyền ----------

    [TestMethod]
    public void Convert_ByCreator_OnOwnIdea_IsAllowed_ButNotOnSomeoneElses()
    {
        long own = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), CreatorId);
        long others = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), OtherCreatorId);

        Convert(own, CreatorId);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.ConvertToContent(others, CreatorId));
        Assert.AreEqual(IdeaStatus.Backlog, _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == others).Status);
    }

    [TestMethod]
    public void Convert_ByManager_OnCreatorsIdea_IsAllowed()
    {
        long ideaId = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), CreatorId);

        long contentId = Convert(ideaId, ManagerId);

        Assert.AreEqual(ManagerId, InMemoryDataStore.Contents.Single(c => c.Id == contentId).CreatedByUserId);
    }

    [TestMethod]
    public void Convert_ByNonMember_Throws()
    {
        long ideaId = _service.Create(ProjectId, FullDraft(IdeaStatus.Backlog), OwnerId);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.ConvertToContent(ideaId, NonMemberId));
    }

    [TestMethod]
    public void CanConvert_FollowsRoleOwnershipAndStatus()
    {
        Idea creatorsBacklog = NewIdea(IdeaStatus.Backlog, CreatorId);
        Idea creatorsDraft = NewIdea(IdeaStatus.Draft, CreatorId);
        Idea creatorsArchived = NewIdea(IdeaStatus.Archived, CreatorId);
        Idea creatorsConverted = NewIdea(IdeaStatus.Converted, CreatorId);

        Assert.IsTrue(_service.CanConvert(creatorsBacklog, OwnerId));
        Assert.IsTrue(_service.CanConvert(creatorsBacklog, ManagerId));
        Assert.IsTrue(_service.CanConvert(creatorsBacklog, CreatorId));
        Assert.IsFalse(_service.CanConvert(creatorsBacklog, OtherCreatorId));
        Assert.IsFalse(_service.CanConvert(creatorsBacklog, NonMemberId));
        Assert.IsTrue(_service.CanConvert(creatorsDraft, CreatorId));
        Assert.IsFalse(_service.CanConvert(creatorsArchived, OwnerId));
        Assert.IsFalse(_service.CanConvert(creatorsConverted, OwnerId));
    }

    // ---------- Helpers ----------

    /// <summary>Chuyển và ghi nhớ Id Content để Cleanup gỡ khỏi InMemoryDataStore (static).</summary>
    private long Convert(long ideaId, long userId)
    {
        long contentId = _service.ConvertToContent(ideaId, userId);
        _createdContentIds.Add(contentId);
        return contentId;
    }

    private static Idea NewIdea(IdeaStatus status, long createdBy) => new()
    {
        IdeaId = 1, ProjectId = ProjectId, Title = "Idea", Status = status, CreatedByUserId = createdBy,
    };

    private static IdeaDraft FullDraft(IdeaStatus status) => new()
    {
        Title = "Series TikTok mới",
        Description = "Chuỗi video ngắn",
        Note = "Ghi chú nội bộ",
        Status = status,
        Tags = new List<string> { "TikTok", "Short-form" },
    };
}
