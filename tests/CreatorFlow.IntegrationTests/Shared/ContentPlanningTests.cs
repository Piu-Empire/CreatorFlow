using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>
/// SCRUM-32 Content Planning: validation trường bắt buộc, lưu đủ thông tin lập kế hoạch và đọc lại đúng ở Content Detail.
/// Chạy trên repository InMemory (không cần PostgreSQL).
/// </summary>
[TestClass]
public sealed class ContentPlanningTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;
    private const long CreatorId = 2;
    private const long NonMemberId = 99;

    private ContentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new ContentService(
            new InMemoryContentRepository(),
            new InMemoryContentDetailsRepository(),
            new InMemoryContentStatusHistoryRepository(),
            new InMemoryProjectMemberRepository(),
            new InMemoryPlatformRepository(),
            new InMemoryUnitOfWork());
    }

    // ---------- Lưu SQL / hiển thị lại đúng Content Detail ----------

    [TestMethod]
    public void Create_SavesAllPlanningFields_AndGetDetailReadsThemBack()
    {
        var draft = FullDraft();

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);
        Content detail = _service.GetDetail(id, OwnerId);

        Assert.AreEqual("Review tai nghe mới", detail.Title);
        Assert.AreEqual("Hook mở đầu 3 giây", detail.Description);
        Assert.AreEqual("[Hook] Mở hộp\n[Body] Thử âm thanh\n[CTA] Theo dõi kênh", detail.Script);
        Assert.AreEqual("Long video", detail.ContentType);
        Assert.AreEqual(Priority.High, detail.Priority);
        Assert.AreEqual(new DateTime(2026, 11, 1), detail.Deadline);
        Assert.AreEqual(new DateTime(2026, 11, 5), detail.PlannedPublishAt);
        Assert.AreEqual(ContentStatus.Script, detail.Status);
        Assert.AreEqual(ProjectId, detail.ProjectId);
        Assert.AreEqual(OwnerId, detail.CreatedByUserId);
    }

    [TestMethod]
    public void Update_ChangesPlanningFields_AndGetDetailReadsNewValues()
    {
        long id = _service.Create(ProjectId, ContentStatus.Script, FullDraft(), OwnerId);

        var edited = FullDraft();
        edited.Title = "Review tai nghe (bản sửa)";
        edited.Script = "Kịch bản mới";
        edited.ContentType = "Reel";
        edited.Priority = Priority.Low;
        edited.Deadline = new DateTime(2026, 12, 1);
        edited.PlannedPublishAt = new DateTime(2026, 12, 3);
        _service.Update(id, edited, OwnerId);

        Content detail = _service.GetDetail(id, OwnerId);
        Assert.AreEqual("Review tai nghe (bản sửa)", detail.Title);
        Assert.AreEqual("Kịch bản mới", detail.Script);
        Assert.AreEqual("Reel", detail.ContentType);
        Assert.AreEqual(Priority.Low, detail.Priority);
        Assert.AreEqual(new DateTime(2026, 12, 1), detail.Deadline);
        Assert.AreEqual(new DateTime(2026, 12, 3), detail.PlannedPublishAt);
    }

    [TestMethod]
    public void Update_ClearingScriptAndPlannedDate_ReadsBackAsNull()
    {
        long id = _service.Create(ProjectId, ContentStatus.Script, FullDraft(), OwnerId);

        var edited = FullDraft();
        edited.Script = string.Empty;
        edited.PlannedPublishAt = null;
        _service.Update(id, edited, OwnerId);

        Content detail = _service.GetDetail(id, OwnerId);
        Assert.IsNull(detail.Script);
        Assert.IsNull(detail.PlannedPublishAt);
    }

    [TestMethod]
    public void Create_TrimsTitleDescriptionAndScript()
    {
        var draft = FullDraft();
        draft.Title = "   Tiêu đề có khoảng trắng   ";
        draft.Description = "  mô tả  ";
        draft.Script = "\n  kịch bản  \n";

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Content detail = _service.GetDetail(id, OwnerId);
        Assert.AreEqual("Tiêu đề có khoảng trắng", detail.Title);
        Assert.AreEqual("mô tả", detail.Description);
        Assert.AreEqual("kịch bản", detail.Script);
    }

    [TestMethod]
    public void Create_IgnoresTimeOfDayForDeadlineAndPlannedPublishDate()
    {
        var draft = FullDraft();
        draft.Deadline = new DateTime(2026, 11, 1, 17, 45, 0);
        draft.PlannedPublishAt = new DateTime(2026, 11, 5, 9, 30, 0);

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Content detail = _service.GetDetail(id, OwnerId);
        Assert.AreEqual(new DateTime(2026, 11, 1), detail.Deadline);
        Assert.AreEqual(new DateTime(2026, 11, 5), detail.PlannedPublishAt);
    }

    // ---------- Loại nội dung ----------

    [TestMethod]
    public void Create_WithBlankContentType_UsesDefaultContentType()
    {
        var draft = FullDraft();
        draft.ContentType = "   ";

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Assert.AreEqual(ContentService.DefaultContentType, _service.GetDetail(id, OwnerId).ContentType);
    }

    [TestMethod]
    public void Create_ContentTypeIsCaseInsensitive_StoresCanonicalName()
    {
        var draft = FullDraft();
        draft.ContentType = "  lONG vIDEO ";

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Assert.AreEqual("Long video", _service.GetDetail(id, OwnerId).ContentType);
    }

    [TestMethod]
    public void Create_WithUnknownContentType_Throws()
    {
        var draft = FullDraft();
        draft.ContentType = "Podcast";

        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId));
    }

    [TestMethod]
    public void DefaultContentType_IsOneOfTheAvailableTypes()
    {
        Assert.IsTrue(ContentService.AvailableContentTypes.Contains(ContentService.DefaultContentType));
    }

    // ---------- Validation trường bắt buộc ----------

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Create_WithEmptyTitle_ThrowsAndStoresNothing(string title)
    {
        int before = InMemoryDataStore.Contents.Count;
        var draft = FullDraft();
        draft.Title = title;

        var ex = Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId));

        Assert.AreEqual("Vui lòng nhập tiêu đề.", ex.Message);
        Assert.AreEqual(before, InMemoryDataStore.Contents.Count);
    }

    [TestMethod]
    public void Create_WithTitleOverLimit_Throws()
    {
        var draft = FullDraft();
        draft.Title = new string('a', ContentService.MaxTitleLength + 1);

        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId));
    }

    [TestMethod]
    public void Create_WithTitleAtLimit_IsAccepted()
    {
        var draft = FullDraft();
        draft.Title = new string('a', ContentService.MaxTitleLength);

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Assert.AreEqual(ContentService.MaxTitleLength, _service.GetDetail(id, OwnerId).Title.Length);
    }

    [TestMethod]
    public void Create_WithDescriptionOverLimit_Throws()
    {
        var draft = FullDraft();
        draft.Description = new string('d', ContentService.MaxDescriptionLength + 1);

        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId));
    }

    [TestMethod]
    public void Create_WithScriptOverLimit_Throws()
    {
        var draft = FullDraft();
        draft.Script = new string('s', ContentService.MaxScriptLength + 1);

        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId));
    }

    [TestMethod]
    public void Create_WithUndefinedPriority_Throws()
    {
        var draft = FullDraft();
        draft.Priority = (Priority)99;

        Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId));
    }

    [TestMethod]
    public void Create_PlannedPublishBeforeDeadline_Throws()
    {
        var draft = FullDraft();
        draft.Deadline = new DateTime(2026, 11, 10);
        draft.PlannedPublishAt = new DateTime(2026, 11, 9);

        var ex = Assert.ThrowsExactly<ContentValidationException>(
            () => _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId));

        Assert.AreEqual("Ngày dự kiến đăng không được sớm hơn hạn hoàn thành.", ex.Message);
    }

    [TestMethod]
    public void Create_PlannedPublishOnSameDayAsDeadline_IsAllowed()
    {
        var draft = FullDraft();
        draft.Deadline = new DateTime(2026, 11, 10, 8, 0, 0);
        draft.PlannedPublishAt = new DateTime(2026, 11, 10, 20, 0, 0);

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Assert.AreEqual(new DateTime(2026, 11, 10), _service.GetDetail(id, OwnerId).PlannedPublishAt);
    }

    [TestMethod]
    public void Create_WithPlannedDateButNoDeadline_IsAllowed()
    {
        var draft = FullDraft();
        draft.Deadline = null;
        draft.PlannedPublishAt = new DateTime(2026, 11, 10);

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Content detail = _service.GetDetail(id, OwnerId);
        Assert.IsNull(detail.Deadline);
        Assert.AreEqual(new DateTime(2026, 11, 10), detail.PlannedPublishAt);
    }

    [TestMethod]
    public void Update_WithInvalidData_LeavesStoredContentUnchanged()
    {
        long id = _service.Create(ProjectId, ContentStatus.Script, FullDraft(), OwnerId);

        var bad = FullDraft();
        bad.Title = "Tiêu đề mới không được lưu";
        bad.ContentType = "Podcast";
        Assert.ThrowsExactly<ContentValidationException>(() => _service.Update(id, bad, OwnerId));

        Content detail = _service.GetDetail(id, OwnerId);
        Assert.AreEqual("Review tai nghe mới", detail.Title);
        Assert.AreEqual("Long video", detail.ContentType);
    }

    // ---------- GetValidationError (dialog dùng để báo lỗi mà không ném exception) ----------

    [TestMethod]
    public void GetValidationError_ReturnsNull_ForValidDraft()
    {
        Assert.IsNull(ContentService.GetValidationError(FullDraft()));
    }

    [TestMethod]
    public void GetValidationError_ReturnsMessage_AndDoesNotThrow_ForEmptyTitle()
    {
        var draft = FullDraft();
        draft.Title = " ";
        ContentService.Normalize(draft);

        Assert.AreEqual("Vui lòng nhập tiêu đề.", ContentService.GetValidationError(draft));
    }

    [TestMethod]
    public void GetValidationError_DoesNotModifyTheDraft()
    {
        var draft = FullDraft();
        draft.Title = "  Có khoảng trắng  ";

        Assert.IsNull(ContentService.GetValidationError(draft));
        Assert.AreEqual("  Có khoảng trắng  ", draft.Title);
    }

    [TestMethod]
    public void Normalize_TrimsTitleAndFillsDefaultContentType()
    {
        var draft = FullDraft();
        draft.Title = "  Có khoảng trắng  ";
        draft.ContentType = "";

        ContentService.Normalize(draft);

        Assert.AreEqual("Có khoảng trắng", draft.Title);
        Assert.AreEqual(ContentService.DefaultContentType, draft.ContentType);
        Assert.IsNull(ContentService.GetValidationError(draft));
    }

    [TestMethod]
    public void Create_WithBlankDescription_ReadsBackAsNull()
    {
        var draft = FullDraft();
        draft.Description = "   ";

        long id = _service.Create(ProjectId, ContentStatus.Script, draft, OwnerId);

        Assert.IsTrue(string.IsNullOrEmpty(_service.GetDetail(id, OwnerId).Description));
    }

    // ---------- Phân quyền ----------

    [TestMethod]
    public void GetDetail_ByNonProjectMember_Throws()
    {
        long id = _service.Create(ProjectId, ContentStatus.Script, FullDraft(), OwnerId);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.GetDetail(id, NonMemberId));
    }

    [TestMethod]
    public void GetDetail_ByCreatorMember_Works()
    {
        long id = _service.Create(ProjectId, ContentStatus.Script, FullDraft(), OwnerId);

        Assert.AreEqual("Review tai nghe mới", _service.GetDetail(id, CreatorId).Title);
    }

    [TestMethod]
    public void GetDetail_ForUnknownContent_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _service.GetDetail(long.MaxValue, OwnerId));
    }

    [TestMethod]
    public void Update_ByCreatorOnSomeoneElsesContent_Throws()
    {
        long id = _service.Create(ProjectId, ContentStatus.Script, FullDraft(), OwnerId);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => _service.Update(id, FullDraft(), CreatorId));
    }

    private static ContentDraft FullDraft() => new()
    {
        Title = "Review tai nghe mới",
        Description = "Hook mở đầu 3 giây",
        Script = "[Hook] Mở hộp\n[Body] Thử âm thanh\n[CTA] Theo dõi kênh",
        ContentType = "Long video",
        Priority = Priority.High,
        Deadline = new DateTime(2026, 11, 1),
        PlannedPublishAt = new DateTime(2026, 11, 5),
        Platforms = new List<string> { "YouTube" },
    };
}