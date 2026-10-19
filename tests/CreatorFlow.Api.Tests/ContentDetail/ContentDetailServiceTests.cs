using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.ContentDetail;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Services.ContentDetail;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Contracts.ContentDetail;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.ContentDetail;

/// <summary>
/// SCRUM-32 Content Planning, SCRUM-33 Quản lý Script, SCRUM-34 Content đa nền tảng (API). Quyền và quy tắc được thực thi Ở BACKEND:
///   - xem: thành viên Project; người thao tác luôn lấy từ JWT;
///   - sửa thông tin: Owner/Manager mọi Content, Creator chỉ Content do mình tạo; Published bị khóa; chỉ Owner/Manager đổi deadline;
///   - platform: phải tồn tại và đang bật, không bỏ platform đã PUBLISHED;
///   - script: Creator chỉ sửa Content mình tạo hoặc được giao; Published/Archived bị khóa; lưu có kiểm tra xung đột theo Version.
/// </summary>
[TestClass]
public sealed class ContentDetailServiceTests
{
    private const long ProjectId = 1;
    private const long OtherProjectId = 2;
    private const long OwnerId = 1;
    private const long ManagerId = 2;
    private const long CreatorId = 3;       // người tạo Content 10
    private const long OtherCreatorId = 4;  // Creator khác
    private const long OutsiderId = 99;

    private const long OwnContent = 10;       // do CreatorId tạo, trạng thái Script
    private const long OwnersContent = 11;    // do OwnerId tạo
    private const long PublishedContent = 12; // đã Published
    private const long ArchivedContent = 13;  // đã Archived
    private const long ForeignContent = 20;   // thuộc Project khác

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static UpdateContentRequest Upd(string title = "Tiêu đề mới", string? description = "mô tả", string? type = "Reel",
        string priority = "High", DateOnly? deadline = null, DateOnly? planned = null, params string[] platforms) =>
        new(title, description, type, priority, deadline, planned, platforms);

    private static CreateContentRequest New(string title = "Content mới", string? type = "Short video", string priority = "Medium",
        string? status = null, DateOnly? deadline = null, DateOnly? planned = null, string[]? platforms = null, long? assignee = null) =>
        new(title, "mô tả", type, priority, status, deadline, planned, platforms, assignee);

    // ================================================================== platform (SCRUM-34)

    [TestMethod]
    public async Task GetPlatforms_NotSignedIn_Returns401()
    {
        var f = new Fixture(actingUserId: null);

        Assert.AreEqual(401, (await f.Service.GetPlatformsAsync()).Status);
    }

    [TestMethod]
    public async Task GetPlatforms_ReturnsOnlyActivePlatformNames()
    {
        var f = new Fixture(CreatorId);
        f.Repo.ActivePlatforms.Remove("Facebook");

        var result = await f.Service.GetPlatformsAsync();

        CollectionAssert.AreEqual(new[] { "TikTok", "YouTube", "Instagram" }, result.Value!.Platforms.ToArray());
    }

    // ================================================================== chi tiết

    [TestMethod]
    public async Task GetDetail_NotSignedIn_Returns401()
    {
        var f = new Fixture(actingUserId: null);

        Assert.AreEqual(401, (await f.Service.GetDetailAsync(OwnContent)).Status);
    }

    [TestMethod]
    public async Task GetDetail_NotProjectMember_Returns403()
    {
        var f = new Fixture(OutsiderId);

        var result = await f.Service.GetDetailAsync(OwnContent);

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual("forbidden", result.ErrorCode);
    }

    [TestMethod]
    public async Task GetDetail_MemberOfAnotherProject_Returns403()
    {
        var f = new Fixture(OwnerId); // Owner của Project 1, không thuộc Project 2

        Assert.AreEqual(403, (await f.Service.GetDetailAsync(ForeignContent)).Status);
    }

    [TestMethod]
    public async Task GetDetail_UnknownContent_Returns404()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.GetDetailAsync(9999);

        Assert.AreEqual(404, result.Status);
        Assert.AreEqual("not_found", result.ErrorCode);
    }

    [TestMethod]
    public async Task GetDetail_Member_ReturnsCodePlatformsAndMyRole()
    {
        var f = new Fixture(OtherCreatorId);
        f.Repo.Rows[OwnContent].Detail = f.Repo.Rows[OwnContent].Detail with { Platforms = ["TikTok", "YouTube"], Deadline = Today.AddDays(3) };

        var result = await f.Service.GetDetailAsync(OwnContent);

        Assert.IsTrue(result.Succeeded);
        ContentDetailResponse detail = result.Value!;
        Assert.AreEqual("CNT-010", detail.Code);
        Assert.AreEqual("Script", detail.Status);
        Assert.AreEqual("Creator", detail.Role);
        Assert.AreEqual(CreatorId, detail.CreatedBy);
        Assert.AreEqual(Today.AddDays(3), detail.Deadline);
        CollectionAssert.AreEqual(new[] { "TikTok", "YouTube" }, detail.Platforms.ToArray());
    }

    // ================================================================== tạo mới (SCRUM-32)

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    [DataRow(CreatorId)]
    public async Task Create_AnyMember_DefaultsToIdea_AndCreatorComesFromJwt(long actingUserId)
    {
        var f = new Fixture(actingUserId);

        var result = await f.Service.CreateAsync(ProjectId, New("  Video mới  "));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Video mới", result.Value!.Title);
        Assert.AreEqual("Idea", result.Value.Status);
        Assert.AreEqual(ProjectId, result.Value.ProjectId);
        Assert.AreEqual(actingUserId, result.Value.CreatedBy);
        Assert.AreEqual("IDEA", f.Repo.LastCreated!.StatusDb);
        Assert.AreEqual("MEDIUM", f.Repo.LastCreated.Details.PriorityDb);
    }

    [TestMethod]
    public async Task Create_NotMember_Returns403_AndNotSignedIn_Returns401()
    {
        Assert.AreEqual(403, (await new Fixture(OutsiderId).Service.CreateAsync(ProjectId, New())).Status);
        Assert.AreEqual(401, (await new Fixture(actingUserId: null).Service.CreateAsync(ProjectId, New())).Status);
    }

    [TestMethod]
    [DataRow("Script", "SCRIPT")]
    [DataRow("production", "PRODUCTION")]
    [DataRow("EDITING", "EDITING")]
    public async Task Create_InitialStageIsHonoured_CaseInsensitive(string status, string expectedDb)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.CreateAsync(ProjectId, New(status: status));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(expectedDb, f.Repo.LastCreated!.StatusDb);
    }

    [TestMethod]
    [DataRow("Review")]
    [DataRow("review")]
    [DataRow("Archived")]
    [DataRow("Nope")]
    [DataRow("3")]
    public async Task Create_ReviewArchivedOrUnknownStage_Returns400OnStatusField(string status)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.CreateAsync(ProjectId, New(status: status));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("status", result.ErrorField);
        Assert.IsNull(f.Repo.LastCreated);
    }

    [TestMethod]
    public async Task Create_InvalidPlanningFields_Returns400WithTheOffendingField()
    {
        var f = new Fixture(OwnerId);
        var cases = new (string Field, CreateContentRequest Request)[]
        {
            ("title", New("   ")),
            ("title", New(new string('a', 201))),
            ("description", New() with { Description = new string('a', 2001) }),
            ("contentType", New(type: "Podcast")),
            ("priority", New(priority: "Critical")),
            ("priority", New(priority: "1")),
            ("plannedPublishAt", New(deadline: Today.AddDays(5), planned: Today.AddDays(4))),
            ("platforms", New(platforms: ["MySpace"])),
        };

        foreach (var (field, request) in cases)
        {
            var result = await f.Service.CreateAsync(ProjectId, request);

            Assert.AreEqual(400, result.Status, field);
            Assert.AreEqual(field, result.ErrorField, field);
        }
        Assert.IsNull(f.Repo.LastCreated);
    }

    [TestMethod]
    public async Task Create_PublishDateOnTheDeadline_IsAccepted()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.CreateAsync(ProjectId, New(deadline: Today.AddDays(5), planned: Today.AddDays(5)));

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task Create_Platforms_AreResolvedToStoredSpelling_AndDeduplicated()
    {
        var f = new Fixture(CreatorId);

        var result = await f.Service.CreateAsync(ProjectId, New(platforms: ["youtube", " TIKTOK ", "YouTube", ""]));

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new[] { "TikTok", "YouTube" }, result.Value!.Platforms.ToArray());
    }

    [TestMethod]
    public async Task Create_InactivePlatform_Returns400()
    {
        var f = new Fixture(OwnerId);
        f.Repo.ActivePlatforms.Remove("Instagram");

        var result = await f.Service.CreateAsync(ProjectId, New(platforms: ["Instagram"]));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("platforms", result.ErrorField);
    }

    [TestMethod]
    public async Task Create_BlankContentType_UsesDefault()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.CreateAsync(ProjectId, New(type: "  "));

        Assert.AreEqual("Short video", result.Value!.ContentType);
    }

    [TestMethod]
    public async Task Create_OwnerAssignsACreator_AppliesTheAssignmentWithTheContentDeadline()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.CreateAsync(ProjectId, New(deadline: Today.AddDays(7), assignee: CreatorId));

        Assert.IsTrue(result.Succeeded);
        var applied = f.Tasks.Applied.Single();
        Assert.AreEqual(result.Value!.ContentId, applied.ContentId);
        Assert.AreEqual(OwnerId, applied.Actor);
        Assert.AreEqual(CreatorId, applied.Additions.Single().UserId);
        Assert.AreEqual(Today.AddDays(7), applied.Additions.Single().Deadline);
    }

    [TestMethod]
    public async Task Create_CreatorMaySelfAssign_ButNotAssignSomeoneElse()
    {
        var f = new Fixture(CreatorId);

        var self = await f.Service.CreateAsync(ProjectId, New(assignee: CreatorId));
        var other = await f.Service.CreateAsync(ProjectId, New(assignee: OtherCreatorId));

        Assert.IsTrue(self.Succeeded);
        Assert.AreEqual(403, other.Status);
        Assert.AreEqual(1, f.Tasks.Applied.Count);
    }

    [TestMethod]
    [DataRow(ManagerId)]   // Manager không phải Creator → không nhận việc được
    [DataRow(OutsiderId)]  // không thuộc Project
    public async Task Create_AssigneeMustBeACreatorOfTheProject_Returns400(long assignee)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.CreateAsync(ProjectId, New(assignee: assignee));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("assigneeUserId", result.ErrorField);
        Assert.AreEqual(0, f.Tasks.Applied.Count);
        Assert.IsNull(f.Repo.LastCreated, "Không được tạo Content khi người nhận không hợp lệ.");
    }

    // ================================================================== sửa thông tin (SCRUM-32)

    [TestMethod]
    public async Task Update_NotSignedIn_401_NotFound_404_NotMember_403()
    {
        Assert.AreEqual(401, (await new Fixture(actingUserId: null).Service.UpdateAsync(OwnContent, Upd())).Status);
        Assert.AreEqual(404, (await new Fixture(OwnerId).Service.UpdateAsync(9999, Upd())).Status);
        Assert.AreEqual(403, (await new Fixture(OutsiderId).Service.UpdateAsync(OwnContent, Upd())).Status);
        Assert.AreEqual(403, (await new Fixture(OwnerId).Service.UpdateAsync(ForeignContent, Upd())).Status);
    }

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    public async Task Update_OwnerAndManager_CanEditAnyContent_WithoutChangingTheStage(long actingUserId)
    {
        var f = new Fixture(actingUserId);

        var result = await f.Service.UpdateAsync(OwnContent, Upd("Đã chỉnh", priority: "urgent", platforms: ["YouTube"]));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Đã chỉnh", result.Value!.Title);
        Assert.AreEqual("Reel", result.Value.ContentType);
        Assert.AreEqual("Urgent", result.Value.Priority);
        Assert.AreEqual("Script", result.Value.Status);
        CollectionAssert.AreEqual(new[] { "YouTube" }, result.Value.Platforms.ToArray());
    }

    [TestMethod]
    public async Task Update_CreatorOwnContent_Succeeds_ButNotAnotherUsersContent()
    {
        var f = new Fixture(CreatorId);

        var own = await f.Service.UpdateAsync(OwnContent, Upd("Của tôi"));
        var others = await f.Service.UpdateAsync(OwnersContent, Upd("Sửa trộm"));

        Assert.IsTrue(own.Succeeded);
        Assert.AreEqual(403, others.Status);
        Assert.AreEqual("forbidden", others.ErrorCode);
        Assert.AreEqual("Content 11", f.Repo.Rows[OwnersContent].Detail.Title);
    }

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(CreatorId)]
    public async Task Update_PublishedContent_Returns409Locked_ForEveryRole(long actingUserId)
    {
        var f = new Fixture(actingUserId);

        var result = await f.Service.UpdateAsync(PublishedContent, Upd());

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("content_locked", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.UpdateCalls);
    }

    [TestMethod]
    public async Task Update_Deadline_OnlyOwnerOrManagerMayChangeIt()
    {
        var f = new Fixture(CreatorId);
        DateOnly current = Today.AddDays(10);
        f.Repo.Rows[OwnContent].Detail = f.Repo.Rows[OwnContent].Detail with { Deadline = current };

        var changed = await f.Service.UpdateAsync(OwnContent, Upd(deadline: current.AddDays(2)));
        var cleared = await f.Service.UpdateAsync(OwnContent, Upd(deadline: null));
        var same = await f.Service.UpdateAsync(OwnContent, Upd("Giữ deadline", deadline: current));

        Assert.AreEqual(403, changed.Status);
        Assert.AreEqual(403, cleared.Status);
        Assert.IsTrue(same.Succeeded);
        Assert.AreEqual(current, f.Repo.Rows[OwnContent].Detail.Deadline);
    }

    [TestMethod]
    public async Task Update_Deadline_ManagerCannotMoveItIntoThePast_ButMayKeepAnOverdueOne()
    {
        var f = new Fixture(ManagerId);
        DateOnly overdue = Today.AddDays(-3);
        f.Repo.Rows[OwnContent].Detail = f.Repo.Rows[OwnContent].Detail with { Deadline = overdue };

        var toPast = await f.Service.UpdateAsync(OwnContent, Upd(deadline: Today.AddDays(-1)));
        var keepOverdue = await f.Service.UpdateAsync(OwnContent, Upd(deadline: overdue));
        var toFuture = await f.Service.UpdateAsync(OwnContent, Upd(deadline: Today.AddDays(4)));

        Assert.AreEqual(400, toPast.Status);
        Assert.AreEqual("deadline", toPast.ErrorField);
        Assert.IsTrue(keepOverdue.Succeeded);
        Assert.IsTrue(toFuture.Succeeded);
        Assert.AreEqual(Today.AddDays(4), f.Repo.Rows[OwnContent].Detail.Deadline);
    }

    [TestMethod]
    public async Task Update_PublishDateBeforeDeadline_Returns400()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.UpdateAsync(OwnContent, Upd(deadline: Today.AddDays(6), planned: Today.AddDays(5)));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("plannedPublishAt", result.ErrorField);
    }

    [TestMethod]
    public async Task Update_InvalidFields_Returns400_AndWritesNothing()
    {
        var f = new Fixture(OwnerId);
        var cases = new (string Field, UpdateContentRequest Request)[]
        {
            ("title", Upd("")),
            ("title", Upd(new string('a', 201))),
            ("description", Upd(description: new string('a', 2001))),
            ("contentType", Upd(type: "Podcast")),
            ("priority", Upd(priority: "Nope")),
            ("platforms", Upd(platforms: ["MySpace"])),
        };

        foreach (var (field, request) in cases)
        {
            var result = await f.Service.UpdateAsync(OwnContent, request);

            Assert.AreEqual(400, result.Status, field);
            Assert.AreEqual(field, result.ErrorField, field);
        }
        Assert.AreEqual(0, f.Repo.UpdateCalls);
    }

    [TestMethod]
    public async Task Update_Platforms_CanBeAddedAndRemoved_WhileNotYetPublished()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Rows[OwnContent].Detail = f.Repo.Rows[OwnContent].Detail with { Platforms = ["Facebook", "TikTok"] };

        var result = await f.Service.UpdateAsync(OwnContent, Upd(platforms: ["tiktok", "Instagram"]));

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new[] { "Instagram", "TikTok" }, result.Value!.Platforms.ToArray());
    }

    [TestMethod]
    public async Task Update_CannotRemoveAPlatformThatIsAlreadyPublished()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Rows[OwnContent].Detail = f.Repo.Rows[OwnContent].Detail with { Platforms = ["TikTok", "YouTube"] };
        f.Repo.Rows[OwnContent].Published.Add("TikTok");

        var removed = await f.Service.UpdateAsync(OwnContent, Upd(platforms: ["YouTube"]));
        var kept = await f.Service.UpdateAsync(OwnContent, Upd(platforms: ["TikTok"]));

        Assert.AreEqual(400, removed.Status);
        Assert.AreEqual("platforms", removed.ErrorField);
        StringAssert.Contains(removed.Message!, "TikTok");
        Assert.IsTrue(kept.Succeeded);
        CollectionAssert.AreEqual(new[] { "TikTok" }, f.Repo.Rows[OwnContent].Detail.Platforms.ToArray());
    }

    [TestMethod]
    [DataRow("SHORT_VIDEO", "Short video")]  // đúng giá trị trong database/02_seed.sql
    [DataRow("short-video", "Short video")]
    [DataRow("long video", "Long video")]
    [DataRow("IMAGE_POST", "Image post")]
    [DataRow("reel", "Reel")]
    public async Task Update_LegacyOrDifferentlyCasedContentType_IsMappedToTheCanonicalName(string sent, string stored)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.UpdateAsync(OwnContent, Upd(type: sent));

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(stored, result.Value!.ContentType);
    }

    [TestMethod]
    public async Task Update_ContentWhoseStoredTypeIsLegacy_CanBeSavedAgainWithTheSameType()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Rows[OwnContent].Detail = f.Repo.Rows[OwnContent].Detail with { ContentType = "SHORT_VIDEO" };
        string typeAsShownByTheApi = (await f.Service.GetDetailAsync(OwnContent)).Value!.ContentType; // client gửi lại đúng giá trị đọc được

        var result = await f.Service.UpdateAsync(OwnContent, Upd("Sửa tiêu đề thôi", type: typeAsShownByTheApi));

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual("Short video", result.Value!.ContentType);
    }

    // ================================================================== script (SCRUM-33)

    [TestMethod]
    public async Task Script_NotSignedIn_401_Unknown_404_NotMember_403()
    {
        Assert.AreEqual(401, (await new Fixture(actingUserId: null).Service.GetScriptAsync(OwnContent)).Status);
        Assert.AreEqual(401, (await new Fixture(actingUserId: null).Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("x", "v"))).Status);
        Assert.AreEqual(404, (await new Fixture(OwnerId).Service.GetScriptAsync(9999)).Status);
        Assert.AreEqual(404, (await new Fixture(OwnerId).Service.SaveScriptAsync(9999, new SaveScriptRequest("x", "v"))).Status);
        Assert.AreEqual(403, (await new Fixture(OutsiderId).Service.GetScriptAsync(OwnContent)).Status);
        Assert.AreEqual(403, (await new Fixture(OutsiderId).Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("x", "v"))).Status);
        Assert.AreEqual(403, (await new Fixture(OwnerId).Service.GetScriptAsync(ForeignContent)).Status);
    }

    [TestMethod]
    public async Task GetScript_ReturnsTextVersionAndWhoMayEdit()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Rows[OwnContent].Script = "Hook\r\nBody";

        var owner = (await f.Service.GetScriptAsync(OwnContent)).Value!;
        var creatorOfContent = (await f.ServiceFor(CreatorId).GetScriptAsync(OwnContent)).Value!;
        var otherCreator = (await f.ServiceFor(OtherCreatorId).GetScriptAsync(OwnContent)).Value!;

        Assert.AreEqual("Hook\nBody", owner.Text);
        Assert.IsTrue(owner.Version.Length > 0);
        Assert.IsTrue(owner.CanEdit);
        Assert.IsTrue(creatorOfContent.CanEdit);
        Assert.IsFalse(otherCreator.CanEdit);
        Assert.IsFalse(otherCreator.IsContentLocked);
        Assert.IsNotNull(otherCreator.ReadOnlyReason);
        Assert.AreEqual(owner.Version, otherCreator.Version);
    }

    [TestMethod]
    [DataRow(PublishedContent)]
    [DataRow(ArchivedContent)]
    public async Task GetScript_PublishedOrArchived_IsLockedEvenForTheOwner(long contentId)
    {
        var f = new Fixture(OwnerId);

        var script = (await f.Service.GetScriptAsync(contentId)).Value!;

        Assert.IsFalse(script.CanEdit);
        Assert.IsTrue(script.IsContentLocked);
        Assert.IsNotNull(script.ReadOnlyReason);
    }

    [TestMethod]
    public async Task GetScript_ContentWithoutScript_ReturnsEmptyText()
    {
        var f = new Fixture(OwnerId);

        var script = (await f.Service.GetScriptAsync(OwnContent)).Value!;

        Assert.AreEqual(string.Empty, script.Text);
    }

    [TestMethod]
    public async Task SaveScript_Owner_StoresNormalizedText_AndReturnsANewVersion()
    {
        var f = new Fixture(OwnerId);
        string before = (await f.Service.GetScriptAsync(OwnContent)).Value!.Version;

        var result = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("\uFEFF  Hook\r\nBody\rCTA\0  ", before));

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual("Hook\nBody\nCTA", result.Value!.Text);
        Assert.AreEqual("Hook\nBody\nCTA", f.Repo.Rows[OwnContent].Script);
        Assert.AreNotEqual(before, result.Value.Version);
        Assert.AreEqual(1, f.Repo.ScriptWrites);
    }

    [TestMethod]
    public async Task SaveScript_CreatorOfContentAndAssignee_MayEdit_OtherCreatorMayNot()
    {
        var f = new Fixture(CreatorId);
        f.Tasks.Assign(OwnContent, ProjectId, OtherCreatorId);
        f.Tasks.Roles[(ProjectId, 5)] = ProjectRole.Creator;
        string version = (await f.Service.GetScriptAsync(OwnContent)).Value!.Version;

        var byCreator = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("viết bởi người tạo", version));
        string v2 = byCreator.Value!.Version;
        var byAssignee = await f.ServiceFor(OtherCreatorId).SaveScriptAsync(OwnContent, new SaveScriptRequest("viết bởi người được giao", v2));
        var byStranger = await f.ServiceFor(5).SaveScriptAsync(OwnContent, new SaveScriptRequest("người lạ", byAssignee.Value!.Version));

        Assert.IsTrue(byCreator.Succeeded);
        Assert.IsTrue(byAssignee.Succeeded, byAssignee.Message);
        Assert.AreEqual(403, byStranger.Status);
        Assert.AreEqual("forbidden", byStranger.ErrorCode);
        Assert.AreEqual("viết bởi người được giao", f.Repo.Rows[OwnContent].Script);
    }

    [TestMethod]
    public async Task SaveScript_CreatorOnAnotherCreatorsContent_Returns403_AndWritesNothing()
    {
        var f = new Fixture(CreatorId);
        string version = (await f.Service.GetScriptAsync(OwnersContent)).Value!.Version;

        var result = await f.Service.SaveScriptAsync(OwnersContent, new SaveScriptRequest("sửa trộm", version));

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Repo.ScriptWrites);
        Assert.IsNull(f.Repo.Rows[OwnersContent].Script);
    }

    [TestMethod]
    [DataRow(PublishedContent)]
    [DataRow(ArchivedContent)]
    public async Task SaveScript_PublishedOrArchived_Returns409Locked_EvenForTheOwner(long contentId)
    {
        var f = new Fixture(OwnerId);
        string version = (await f.Service.GetScriptAsync(contentId)).Value!.Version;

        var result = await f.Service.SaveScriptAsync(contentId, new SaveScriptRequest("muộn rồi", version));

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("content_locked", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.ScriptWrites);
    }

    [TestMethod]
    public async Task SaveScript_StaleVersion_Returns409Conflict_AndKeepsTheOtherPersonsScript()
    {
        var f = new Fixture(OwnerId);
        string opened = (await f.Service.GetScriptAsync(OwnContent)).Value!.Version;
        f.Repo.Rows[OwnContent].Script = "bản của người khác"; // người khác lưu sau khi mình mở

        var result = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("bản của tôi", opened));

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("script_conflict", result.ErrorCode);
        Assert.AreEqual("bản của người khác", f.Repo.Rows[OwnContent].Script);
        Assert.AreEqual(0, f.Repo.ScriptWrites);
    }

    [TestMethod]
    public async Task SaveScript_LostCompareAndSet_Returns409Conflict()
    {
        var f = new Fixture(OwnerId);
        string version = (await f.Service.GetScriptAsync(OwnContent)).Value!.Version;
        f.Repo.FailNextScriptWrite = true; // người khác ghi xen giữa lúc service kiểm tra và lúc ghi

        var result = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("bản của tôi", version));

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("script_conflict", result.ErrorCode);
        Assert.AreEqual("[người khác vừa sửa]", f.Repo.Rows[OwnContent].Script);
    }

    [TestMethod]
    public async Task SaveScript_EachSaveNeedsTheVersionReturnedByThePreviousOne()
    {
        var f = new Fixture(OwnerId);
        string v0 = (await f.Service.GetScriptAsync(OwnContent)).Value!.Version;

        var first = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("lần 1", v0));
        var reuseOld = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("lần 2", v0));
        var second = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("lần 2", first.Value!.Version));

        Assert.IsTrue(first.Succeeded);
        Assert.AreEqual(409, reuseOld.Status);
        Assert.IsTrue(second.Succeeded);
        Assert.AreEqual("lần 2", f.Repo.Rows[OwnContent].Script);
        Assert.AreEqual(2, f.Repo.ScriptWrites);
    }

    [TestMethod]
    public async Task SaveScript_SameTextAfterNormalization_DoesNotWrite_AndKeepsTheVersion()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Rows[OwnContent].Script = "Hook\nBody";
        var opened = (await f.Service.GetScriptAsync(OwnContent)).Value!;

        var result = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest("  Hook\r\nBody  ", opened.Version));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, f.Repo.ScriptWrites);
        Assert.AreEqual(opened.Version, result.Value!.Version);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   \r\n  ")]
    public async Task SaveScript_BlankText_ClearsTheScript(string? text)
    {
        var f = new Fixture(OwnerId);
        f.Repo.Rows[OwnContent].Script = "có nội dung";
        string version = (await f.Service.GetScriptAsync(OwnContent)).Value!.Version;

        var result = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest(text, version));

        Assert.IsTrue(result.Succeeded);
        Assert.IsNull(f.Repo.Rows[OwnContent].Script);
        Assert.AreEqual(string.Empty, result.Value!.Text);
    }

    [TestMethod]
    public async Task SaveScript_LongScript_UpToTheLimitIsAccepted_OneOverIsRejected()
    {
        var f = new Fixture(OwnerId);
        string version = (await f.Service.GetScriptAsync(OwnContent)).Value!.Version;

        var tooLong = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest(new string('a', 100_001), version));
        var atLimit = await f.Service.SaveScriptAsync(OwnContent, new SaveScriptRequest(new string('a', 100_000), version));

        Assert.AreEqual(400, tooLong.Status);
        Assert.AreEqual("text", tooLong.ErrorField);
        Assert.IsTrue(atLimit.Succeeded, atLimit.Message);
        Assert.AreEqual(100_000, f.Repo.Rows[OwnContent].Script!.Length);
    }

    // ================================================================== activity

    [TestMethod]
    public async Task Activity_NotSignedIn_401_Unknown_404_NotMember_403()
    {
        Assert.AreEqual(401, (await new Fixture(actingUserId: null).Service.GetActivityAsync(OwnContent)).Status);
        Assert.AreEqual(404, (await new Fixture(OwnerId).Service.GetActivityAsync(9999)).Status);
        Assert.AreEqual(403, (await new Fixture(OutsiderId).Service.GetActivityAsync(OwnContent)).Status);
    }

    [TestMethod]
    public async Task Activity_Member_ReturnsTheTimelineOfThatContentOnly()
    {
        var f = new Fixture(OtherCreatorId);
        var at = new DateTime(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc);
        f.Repo.Activity[OwnContent] = [
            new ActivityRecord(at, "Owner", "Idea → Script", "ok", ActivityKinds.StatusChange, "Idea", "Script"),
            new ActivityRecord(at.AddHours(1), "Creator", "submitted Review #1", null,
                ActivityKinds.ReviewSubmitted, "Editing", "Review", 1, "Pending")];
        f.Repo.Activity[OwnersContent] = [new ActivityRecord(at, "Owner", "không liên quan", null)];

        var result = await f.Service.GetActivityAsync(OwnContent);

        Assert.AreEqual(OwnContent, result.Value!.ContentId);
        CollectionAssert.AreEqual(new[] { "Idea → Script", "submitted Review #1" }, result.Value.Items.Select(i => i.Description).ToArray());
        Assert.AreEqual("ok", result.Value.Items[0].Feedback);
        Assert.AreEqual(ActivityKinds.StatusChange, result.Value.Items[0].Kind);
        Assert.AreEqual("Script", result.Value.Items[0].ToStatus);
        Assert.AreEqual(ActivityKinds.ReviewSubmitted, result.Value.Items[1].Kind);
        Assert.AreEqual(1, result.Value.Items[1].ReviewNo);
        Assert.AreEqual("Pending", result.Value.Items[1].ReviewStatus);
    }

    // ================================================================== references

    [TestMethod]
    public async Task References_ListRequiresMembership_AndReturnsCanManage()
    {
        var f = new Fixture(CreatorId);
        f.Repo.References[OwnContent] = [new ContentReferenceRecord(1, OwnContent, "Brief", "https://example.test/brief.pdf", CreatorId, DateTime.UtcNow)];

        Assert.AreEqual(401, (await new Fixture(actingUserId: null).Service.GetReferencesAsync(OwnContent)).Status);
        Assert.AreEqual(403, (await new Fixture(OutsiderId).Service.GetReferencesAsync(OwnContent)).Status);
        Assert.AreEqual(404, (await f.Service.GetReferencesAsync(9999)).Status);

        var own = await f.Service.GetReferencesAsync(OwnContent);
        var anotherCreators = await f.ServiceFor(OtherCreatorId).GetReferencesAsync(OwnContent);

        Assert.IsTrue(own.Succeeded);
        Assert.IsTrue(own.Value!.CanManage);
        Assert.AreEqual("Brief", own.Value.References.Single().DisplayName);
        Assert.IsFalse(anotherCreators.Value!.CanManage);
    }

    [TestMethod]
    public async Task References_AddValidatesAndStoresHttpLinksWithoutFetchingThem()
    {
        var f = new Fixture(CreatorId);

        var added = await f.Service.AddReferenceAsync(OwnContent, new CreateContentReferenceRequest("  Brief  ", "https://example.invalid/file.pdf"));

        Assert.AreEqual(201, added.Status);
        Assert.AreEqual("Brief", added.Value!.DisplayName);
        Assert.AreEqual("https://example.invalid/file.pdf", added.Value.ResourceUrl);
        Assert.AreEqual(CreatorId, added.Value.AddedBy);
        Assert.AreEqual(1, (await f.Repo.GetReferencesAsync(OwnContent)).Count);
        Assert.AreEqual(403, (await new Fixture(OtherCreatorId).Service.AddReferenceAsync(OwnContent,
            new CreateContentReferenceRequest("No access", "https://example.test"))).Status);
    }

    [TestMethod]
    [DataRow("", "https://example.test")]
    [DataRow("  ", "https://example.test")]
    [DataRow("a", "file:///C:/private.pdf")]
    [DataRow("a", "not-a-url")]
    public async Task References_AddRejectsInvalidNameOrUrl(string name, string url)
    {
        var f = new Fixture(CreatorId);

        var result = await f.Service.AddReferenceAsync(OwnContent, new CreateContentReferenceRequest(name, url));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual(0, (await f.Repo.GetReferencesAsync(OwnContent)).Count);
    }

    [TestMethod]
    public async Task References_AddAndDeleteRespectOwnershipAndContentState()
    {
        var f = new Fixture(OtherCreatorId);

        Assert.AreEqual(403, (await f.Service.AddReferenceAsync(OwnContent,
            new CreateContentReferenceRequest("Brief", "https://example.test"))).Status);
        Assert.AreEqual(409, (await f.Service.AddReferenceAsync(PublishedContent,
            new CreateContentReferenceRequest("Brief", "https://example.test"))).Status);

        var owner = await f.ServiceFor(OwnerId).AddReferenceAsync(OwnContent,
            new CreateContentReferenceRequest("Brief", "http://example.test"));
        Assert.IsTrue(owner.Succeeded);
        Assert.AreEqual(404, (await f.ServiceFor(OwnerId).DeleteReferenceAsync(OwnersContent, owner.Value!.ReferenceId)).Status);
        Assert.IsTrue((await f.ServiceFor(OwnerId).DeleteReferenceAsync(OwnContent, owner.Value.ReferenceId)).Succeeded);
        Assert.AreEqual(0, (await f.Repo.GetReferencesAsync(OwnContent)).Count);
    }

    [TestMethod]
    public async Task References_SaveChangesAppliesAddsAndDeletesAsOneAuthorizedOperation()
    {
        var f = new Fixture(CreatorId);
        ContentReferenceRecord existing = await f.Repo.AddReferenceAsync(OwnContent, "Old", "https://example.test/old", CreatorId);
        var request = new SaveContentReferenceChangesRequest(
            [new CreateContentReferenceRequest(" New ", "https://example.test/new")],
            [existing.ReferenceId]);

        var saved = await f.Service.SaveReferenceChangesAsync(OwnContent, request);

        Assert.IsTrue(saved.Succeeded);
        Assert.AreEqual(1, saved.Value!.References.Count);
        Assert.AreEqual("New", saved.Value.References[0].DisplayName);
        Assert.AreEqual(CreatorId, saved.Value.References[0].AddedBy);
        Assert.AreEqual(403, (await new Fixture(OtherCreatorId).Service.SaveReferenceChangesAsync(OwnContent, request)).Status);
        Assert.AreEqual(409, (await f.ServiceFor(OwnerId).SaveReferenceChangesAsync(PublishedContent, request)).Status);
    }

    [TestMethod]
    public async Task References_SaveChangesRejectsInvalidOrMissingDeleteWithoutPartialWrite()
    {
        var f = new Fixture(CreatorId);
        ContentReferenceRecord existing = await f.Repo.AddReferenceAsync(OwnContent, "Old", "https://example.test/old", CreatorId);

        var invalid = await f.Service.SaveReferenceChangesAsync(OwnContent, new SaveContentReferenceChangesRequest(
            [new CreateContentReferenceRequest("", "https://example.test/new")], [existing.ReferenceId]));
        var missingDelete = await f.Service.SaveReferenceChangesAsync(OwnContent, new SaveContentReferenceChangesRequest(
            [new CreateContentReferenceRequest("New", "https://example.test/new")], [long.MaxValue]));

        Assert.AreEqual(400, invalid.Status);
        Assert.AreEqual(404, missingDelete.Status);
        IReadOnlyList<ContentReferenceRecord> references = await f.Repo.GetReferencesAsync(OwnContent);
        Assert.AreEqual(1, references.Count);
        Assert.AreEqual(existing.ReferenceId, references[0].ReferenceId);
    }

    // ================================================================== test double

    private sealed class Fixture
    {
        public Fixture(long? actingUserId = CreatorId)
        {
            Tasks.Roles[(ProjectId, OwnerId)] = ProjectRole.Owner;
            Tasks.Roles[(ProjectId, ManagerId)] = ProjectRole.Manager;
            Tasks.Roles[(ProjectId, CreatorId)] = ProjectRole.Creator;
            Tasks.Roles[(ProjectId, OtherCreatorId)] = ProjectRole.Creator;
            Tasks.Roles[(OtherProjectId, OtherCreatorId)] = ProjectRole.Creator;

            Repo.Add(OwnContent, ProjectId, CreatorId);
            Repo.Add(OwnersContent, ProjectId, OwnerId);
            Repo.Add(PublishedContent, ProjectId, CreatorId, status: "Published");
            Repo.Add(ArchivedContent, ProjectId, CreatorId, status: "Archived");
            Repo.Add(ForeignContent, OtherProjectId, OtherCreatorId);

            Service = ServiceFor(actingUserId);
        }

        public ContentDetailRepositoryFake Repo { get; } = new();
        public MemberRolesFake Tasks { get; } = new();
        public ContentDetailService Service { get; }

        public ContentDetailService ServiceFor(long? userId) => new(Repo, Tasks, new CurrentAuthenticatedUser
        {
            User = userId is { } id
                ? new User { UserId = id, Email = $"user{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                : null,
        });
    }
}
