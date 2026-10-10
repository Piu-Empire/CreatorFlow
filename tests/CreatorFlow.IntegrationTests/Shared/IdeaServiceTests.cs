using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// Idea Bank: CRUD qua Service/Repository, phân quyền thêm/sửa/xóa, tag/trạng thái/ghi chú, tìm kiếm/lọc,
/// và dữ liệu tách theo Project. Chạy trên repository InMemory (không cần PostgreSQL).
/// Thành viên Project #1 (InMemoryDataStore): User 1 = Owner, 2 = Creator A, 3 = Manager, 4 = Creator B. Project #2 không có thành viên.
/// </summary>
[TestClass]
public sealed class IdeaServiceTests
{
    private const long ProjectId = 1;
    private const long OtherProjectId = 2;
    private const long OwnerId = 1;
    private const long CreatorId = 2;
    private const long ManagerId = 3;
    private const long OtherCreatorId = 4;
    private const long NonMemberId = 99;

    private InMemoryIdeaRepository _repo = null!;
    private IdeaService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repo = new InMemoryIdeaRepository();
        _service = new IdeaService(_repo, new InMemoryProjectMemberRepository(), new InMemoryContentStatusHistoryRepository(), new InMemoryUnitOfWork());
    }

    // ---------- Thêm + đọc lại ----------

    [TestMethod]
    public void Create_SavesAllFields_AndGetIdeasReadsThemBack()
    {
        long id = _service.Create(ProjectId, FullDraft(), ManagerId);

        Idea idea = _service.GetIdeas(ProjectId, ManagerId).Single(i => i.IdeaId == id);

        Assert.AreEqual("Series TikTok mới", idea.Title);
        Assert.AreEqual("Chuỗi video ngắn", idea.Description);
        Assert.AreEqual("Chờ chốt khách mời", idea.Note);
        Assert.AreEqual(IdeaStatus.Backlog, idea.Status);
        CollectionAssert.AreEquivalent(new[] { "TikTok", "Marketing" }, idea.Tags);
        Assert.AreEqual(ManagerId, idea.CreatedByUserId);
        Assert.AreEqual(ProjectId, idea.ProjectId);
    }

    [TestMethod]
    public void Create_ByCreator_IsAllowed()
    {
        long id = _service.Create(ProjectId, FullDraft(), CreatorId);

        Assert.AreEqual(CreatorId, _service.GetIdeas(ProjectId, CreatorId).Single(i => i.IdeaId == id).CreatedByUserId);
    }

    [TestMethod]
    public void Create_ByNonMember_Throws()
    {
        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.Create(ProjectId, FullDraft(), NonMemberId));
    }

    // ---------- Validation ----------

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Create_WithEmptyTitle_ThrowsAndStoresNothing(string title)
    {
        var draft = FullDraft();
        draft.Title = title;

        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Create(ProjectId, draft, OwnerId));
        Assert.AreEqual(0, _service.GetIdeas(ProjectId, OwnerId).Count);
    }

    [TestMethod]
    public void Create_TitleAtLimit_IsAccepted_AndOverLimitThrows()
    {
        var ok = FullDraft();
        ok.Title = new string('a', IdeaService.MaxTitleLength);
        _service.Create(ProjectId, ok, OwnerId);

        var tooLong = FullDraft();
        tooLong.Title = new string('a', IdeaService.MaxTitleLength + 1);
        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Create(ProjectId, tooLong, OwnerId));
    }

    [TestMethod]
    public void Create_DescriptionOrNoteOverLimit_Throws()
    {
        var longDescription = FullDraft();
        longDescription.Description = new string('d', IdeaService.MaxDescriptionLength + 1);
        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Create(ProjectId, longDescription, OwnerId));

        var longNote = FullDraft();
        longNote.Note = new string('n', IdeaService.MaxNoteLength + 1);
        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Create(ProjectId, longNote, OwnerId));
    }

    [TestMethod]
    public void Create_TooManyTagsOrTagTooLong_Throws()
    {
        var manyTags = FullDraft();
        manyTags.Tags = Enumerable.Range(1, IdeaService.MaxTagCount + 1).Select(n => $"tag{n}").ToList();
        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Create(ProjectId, manyTags, OwnerId));

        var longTag = FullDraft();
        longTag.Tags = new List<string> { new string('t', IdeaService.MaxTagLength + 1) };
        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Create(ProjectId, longTag, OwnerId));
    }

    [TestMethod]
    public void Create_NormalizesTags_RemovesHashSpacesAndDuplicates()
    {
        var draft = FullDraft();
        draft.Tags = new List<string> { "  #TikTok ", "tiktok", "Short   form", "", "  " };

        long id = _service.Create(ProjectId, draft, OwnerId);

        CollectionAssert.AreEquivalent(new[] { "TikTok", "Short form" },
            _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == id).Tags);
    }

    [TestMethod]
    public void Create_ReusesExistingProjectTagSpelling()
    {
        var first = FullDraft();
        first.Tags = new List<string> { "TikTok" };
        _service.Create(ProjectId, first, OwnerId);

        var second = FullDraft();
        second.Tags = new List<string> { "tiktok" };
        long id = _service.Create(ProjectId, second, OwnerId);

        CollectionAssert.AreEqual(new[] { "TikTok" }, _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == id).Tags);
        Assert.AreEqual(1, _service.GetProjectTags(ProjectId, OwnerId).Count);
    }

    [TestMethod]
    public void Create_WithConvertedStatus_Throws()
    {
        var draft = FullDraft();
        draft.Status = IdeaStatus.Converted;

        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Create(ProjectId, draft, OwnerId));
    }

    [TestMethod]
    public void GetValidationError_DoesNotModifyTheDraft_AndNormalizeDoes()
    {
        var draft = FullDraft();
        draft.Title = "  Có khoảng trắng  ";

        Assert.IsNull(IdeaService.GetValidationError(draft));
        Assert.AreEqual("  Có khoảng trắng  ", draft.Title);

        IdeaService.Normalize(draft);
        Assert.AreEqual("Có khoảng trắng", draft.Title);
    }

    // ---------- Sửa ----------

    [TestMethod]
    public void Update_ByOwner_ChangesFieldsAndTags()
    {
        long id = _service.Create(ProjectId, FullDraft(), CreatorId);

        var edit = FullDraft();
        edit.Title = "Tiêu đề mới";
        edit.Note = "Ghi chú mới";
        edit.Status = IdeaStatus.Archived;
        edit.Tags = new List<string> { "YouTube" };
        _service.Update(id, edit, OwnerId);

        Idea idea = _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == id);
        Assert.AreEqual("Tiêu đề mới", idea.Title);
        Assert.AreEqual("Ghi chú mới", idea.Note);
        Assert.AreEqual(IdeaStatus.Archived, idea.Status);
        CollectionAssert.AreEqual(new[] { "YouTube" }, idea.Tags);
        Assert.AreEqual(CreatorId, idea.CreatedByUserId); // người tạo không đổi
    }

    [TestMethod]
    public void Update_ByManager_OnCreatorsIdea_IsAllowed()
    {
        long id = _service.Create(ProjectId, FullDraft(), CreatorId);

        _service.Update(id, FullDraft(), ManagerId);
    }

    [TestMethod]
    public void Update_ByCreator_OnOwnIdea_IsAllowed_ButNotOnSomeoneElses()
    {
        long own = _service.Create(ProjectId, FullDraft(), CreatorId);
        long others = _service.Create(ProjectId, FullDraft(), OtherCreatorId);

        _service.Update(own, FullDraft(), CreatorId);
        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.Update(others, FullDraft(), CreatorId));
    }

    [TestMethod]
    public void Update_WithInvalidData_LeavesStoredIdeaUnchanged()
    {
        long id = _service.Create(ProjectId, FullDraft(), OwnerId);

        var bad = FullDraft();
        bad.Title = "";
        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Update(id, bad, OwnerId));

        Assert.AreEqual("Series TikTok mới", _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == id).Title);
    }

    [TestMethod]
    public void Update_UnknownIdea_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _service.Update(9999, FullDraft(), OwnerId));
    }

    [TestMethod]
    public void Update_ConvertedIdea_CannotChangeStatus_ButCanChangeNote()
    {
        long id = _repo.Create(ProjectId, new IdeaDraft { Title = "Đã chuyển", Status = IdeaStatus.Converted }, OwnerId);

        var changeStatus = FullDraft();
        changeStatus.Status = IdeaStatus.Backlog;
        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Update(id, changeStatus, OwnerId));

        var keepStatus = FullDraft();
        keepStatus.Status = IdeaStatus.Converted;
        keepStatus.Note = "Cập nhật ghi chú";
        _service.Update(id, keepStatus, OwnerId);
        Assert.AreEqual("Cập nhật ghi chú", _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == id).Note);
    }

    // ---------- Xóa ----------

    [TestMethod]
    public void Delete_ByOwner_RemovesIdea()
    {
        long id = _service.Create(ProjectId, FullDraft(), CreatorId);

        _service.Delete(id, OwnerId);

        Assert.AreEqual(0, _service.GetIdeas(ProjectId, OwnerId).Count);
    }

    [TestMethod]
    public void Delete_ByCreator_OnOwnIdea_IsAllowed_ButNotOnSomeoneElses()
    {
        long own = _service.Create(ProjectId, FullDraft(), CreatorId);
        long others = _service.Create(ProjectId, FullDraft(), OtherCreatorId);

        _service.Delete(own, CreatorId);
        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.Delete(others, CreatorId));
        Assert.AreEqual(1, _service.GetIdeas(ProjectId, OwnerId).Count);
    }

    [TestMethod]
    public void Delete_ConvertedIdea_Throws()
    {
        long id = _repo.Create(ProjectId, new IdeaDraft { Title = "Đã chuyển", Status = IdeaStatus.Converted }, OwnerId);

        Assert.ThrowsExactly<IdeaValidationException>(() => _service.Delete(id, OwnerId));
    }

    [TestMethod]
    public void Delete_UnknownIdea_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _service.Delete(9999, OwnerId));
    }

    // ---------- Quyền cho UI ----------

    [TestMethod]
    public void CanEditAndCanDelete_FollowRoleAndOwnership()
    {
        long id = _service.Create(ProjectId, FullDraft(), CreatorId);
        Idea idea = _service.GetIdeas(ProjectId, OwnerId).Single(i => i.IdeaId == id);

        Assert.IsTrue(_service.CanEdit(idea, CreatorId));
        Assert.IsTrue(_service.CanEdit(idea, ManagerId));
        Assert.IsFalse(_service.CanEdit(idea, OtherCreatorId));
        Assert.IsFalse(_service.CanEdit(idea, NonMemberId));
        Assert.IsTrue(_service.CanDelete(idea, OwnerId));
        Assert.IsFalse(_service.CanDelete(idea, OtherCreatorId));
        Assert.IsTrue(_service.CanCreate(ProjectId, CreatorId));
        Assert.IsFalse(_service.CanCreate(ProjectId, NonMemberId));
    }

    // ---------- Tìm kiếm / lọc ----------

    [TestMethod]
    public void Search_MatchesTitleDescriptionNoteAndTag_CaseInsensitive()
    {
        Add("Video hướng dẫn", "Giới thiệu tính năng", "Cần quay lại", IdeaStatus.Draft, "Tutorial");
        Add("Podcast số 12", "Chủ đề năng suất", "Chờ khách mời", IdeaStatus.Backlog, "Audio");

        Assert.AreEqual(1, Search("HƯỚNG DẪN").Count);   // tiêu đề
        Assert.AreEqual(1, Search("năng suất").Count);   // mô tả
        Assert.AreEqual(1, Search("khách mời").Count);   // ghi chú
        Assert.AreEqual(1, Search("audio").Count);       // tag
        Assert.AreEqual(0, Search("không có từ này").Count);
        Assert.AreEqual(2, Search("").Count);            // trống = không lọc
    }

    [TestMethod]
    public void Filter_ByStatus()
    {
        Add("A", "", "", IdeaStatus.Draft);
        Add("B", "", "", IdeaStatus.Backlog);
        Add("C", "", "", IdeaStatus.Backlog);

        var backlog = _service.GetIdeas(ProjectId, OwnerId, new IdeaFilter { Status = IdeaStatus.Backlog });

        CollectionAssert.AreEquivalent(new[] { "B", "C" }, backlog.Select(i => i.Title).ToArray());
    }

    [TestMethod]
    public void Filter_ByTag_AndCombinedWithStatusAndSearch()
    {
        Add("Series 1", "", "", IdeaStatus.Backlog, "TikTok");
        Add("Series 2", "", "", IdeaStatus.Draft, "TikTok");
        Add("Series 3", "", "", IdeaStatus.Backlog, "YouTube");

        Assert.AreEqual(2, _service.GetIdeas(ProjectId, OwnerId, new IdeaFilter { Tag = "tiktok" }).Count);

        var combined = _service.GetIdeas(ProjectId, OwnerId,
            new IdeaFilter { Tag = "TikTok", Status = IdeaStatus.Backlog, SearchText = "series" });
        CollectionAssert.AreEqual(new[] { "Series 1" }, combined.Select(i => i.Title).ToArray());
    }

    [TestMethod]
    public void GetIdeas_SortsMostRecentlyUpdatedFirst()
    {
        long first = Add("Cũ", "", "", IdeaStatus.Draft);
        Add("Mới", "", "", IdeaStatus.Draft);

        var edit = FullDraft();
        edit.Title = "Cũ (vừa sửa)";
        _service.Update(first, edit, OwnerId);

        Assert.AreEqual("Cũ (vừa sửa)", _service.GetIdeas(ProjectId, OwnerId).First().Title);
    }

    [TestMethod]
    public void CountBacklog_CountsOnlyBacklogOfTheProject()
    {
        Add("A", "", "", IdeaStatus.Backlog);
        Add("B", "", "", IdeaStatus.Draft);
        _repo.Create(OtherProjectId, new IdeaDraft { Title = "Project khác", Status = IdeaStatus.Backlog }, OwnerId);

        Assert.AreEqual(1, _service.CountBacklog(ProjectId, OwnerId));
        Assert.AreEqual(0, _service.CountBacklog(ProjectId, NonMemberId));
    }

    // ---------- Dữ liệu tách theo Project ----------

    [TestMethod]
    public void Ideas_AreSeparatedByProject()
    {
        Add("Của Project 1", "", "", IdeaStatus.Draft, "Chung");
        long otherId = _repo.Create(OtherProjectId, new IdeaDraft { Title = "Của Project 2", Tags = new List<string> { "Chung" } }, OwnerId);

        // Project 1 chỉ thấy Idea của Project 1, tag chỉ của Project 1.
        CollectionAssert.AreEqual(new[] { "Của Project 1" }, _service.GetIdeas(ProjectId, OwnerId).Select(i => i.Title).ToArray());
        Assert.AreEqual(0, _repo.GetByProject(ProjectId, new IdeaFilter { SearchText = "Project 2" }).Count);

        // Người không thuộc Project 2 không đọc/sửa/xóa được Idea ở đó.
        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.GetIdeas(OtherProjectId, OwnerId));
        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.Update(otherId, FullDraft(), OwnerId));
        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.Delete(otherId, OwnerId));
        Assert.AreEqual(1, _repo.GetByProject(OtherProjectId, new IdeaFilter()).Count);
    }

    [TestMethod]
    public void Seed_ProvidesSampleIdeasForProject1Only()
    {
        var seeded = new InMemoryIdeaRepository(withSeed: true);

        Assert.IsTrue(seeded.GetByProject(ProjectId, new IdeaFilter()).Count > 0);
        Assert.AreEqual(0, seeded.GetByProject(OtherProjectId, new IdeaFilter()).Count);
    }

    // ---------- Helpers ----------

    private long Add(string title, string description, string note, IdeaStatus status, params string[] tags) =>
        _service.Create(ProjectId, new IdeaDraft
        {
            Title = title,
            Description = description,
            Note = note,
            Status = status,
            Tags = tags.ToList(),
        }, OwnerId);

    private List<Idea> Search(string text) =>
        _service.GetIdeas(ProjectId, OwnerId, new IdeaFilter { SearchText = text });

    private static IdeaDraft FullDraft() => new()
    {
        Title = "Series TikTok mới",
        Description = "Chuỗi video ngắn",
        Note = "Chờ chốt khách mời",
        Status = IdeaStatus.Backlog,
        Tags = new List<string> { "TikTok", "Marketing" },
    };
}