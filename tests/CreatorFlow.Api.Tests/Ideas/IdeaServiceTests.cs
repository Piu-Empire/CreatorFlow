using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Ideas;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Services.Ideas;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Contracts.Ideas;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Ideas;

/// <summary>
/// SCRUM-30 / SCRUM-31 Idea Bank API. Quyền và validation được thực thi Ở BACKEND (IdeaService):
///   - chỉ thành viên đang hoạt động của Project mới xem/ghi Idea; người thao tác luôn lấy từ JWT;
///   - Owner/Manager sửa/xóa/chuyển mọi Idea, Creator chỉ Idea do mình tạo;
///   - Converted không xóa, không đổi trạng thái tay; chuyển Content chỉ từ Draft/Backlog.
/// </summary>
[TestClass]
public sealed class IdeaServiceTests
{
    private const long ProjectId = 1;
    private const long OtherProjectId = 2;
    private const long OwnerId = 1;
    private const long ManagerId = 2;
    private const long CreatorId = 3;
    private const long OtherCreatorId = 4;
    private const long OutsiderId = 99;

    private static IdeaRequest Req(string title = "Ý tưởng mới", string status = "Draft", string? description = "mô tả",
        string? note = "ghi chú", params string[] tags) => new(title, description, note, status, tags);

    // ================================================================== đăng nhập / thành viên

    [TestMethod]
    public async Task EveryOperation_NotSignedIn_Returns401_AndChangesNothing()
    {
        var f = new Fixture(actingUserId: null);
        Idea existing = f.Repo.Seed(ProjectId, CreatorId, "Có sẵn");

        int[] statuses =
        [
            (await f.Service.GetIdeasAsync(ProjectId, null, null, null)).Status,
            (await f.Service.GetTagsAsync(ProjectId)).Status,
            (await f.Service.CreateAsync(ProjectId, Req())).Status,
            (await f.Service.UpdateAsync(existing.IdeaId, Req("Sửa"))).Status,
            (await f.Service.ConvertAsync(existing.IdeaId)).Status,
            (await f.Service.DeleteAsync(existing.IdeaId)).Status,
        ];

        CollectionAssert.AreEqual(new[] { 401, 401, 401, 401, 401, 401 }, statuses);
        Assert.AreEqual(1, f.Repo.Ideas.Count);
        Assert.AreEqual("Có sẵn", f.Repo.Get(existing.IdeaId).Title);
        Assert.AreEqual(IdeaStatus.Draft, f.Repo.Get(existing.IdeaId).Status);
    }

    [TestMethod]
    public async Task ReadAndCreate_NotProjectMember_Returns403()
    {
        var f = new Fixture(OutsiderId);

        var list = await f.Service.GetIdeasAsync(ProjectId, null, null, null);
        var tags = await f.Service.GetTagsAsync(ProjectId);
        var create = await f.Service.CreateAsync(ProjectId, Req());

        Assert.AreEqual(403, list.Status);
        Assert.AreEqual(403, tags.Status);
        Assert.AreEqual(403, create.Status);
        Assert.AreEqual("forbidden", create.ErrorCode);
        Assert.AreEqual(0, f.Repo.Ideas.Count);
    }

    [TestMethod]
    public async Task UpdateConvertDelete_IdeaOfAnotherProject_Returns403_AndChangesNothing()
    {
        var f = new Fixture(OwnerId); // Owner của Project 1 nhưng KHÔNG thuộc Project 2
        Idea foreign = f.Repo.Seed(OtherProjectId, OtherCreatorId, "Của project khác");

        var update = await f.Service.UpdateAsync(foreign.IdeaId, Req("Chiếm quyền"));
        var convert = await f.Service.ConvertAsync(foreign.IdeaId);
        var delete = await f.Service.DeleteAsync(foreign.IdeaId);

        Assert.AreEqual(403, update.Status);
        Assert.AreEqual(403, convert.Status);
        Assert.AreEqual(403, delete.Status);
        Idea after = f.Repo.Get(foreign.IdeaId);
        Assert.AreEqual("Của project khác", after.Title);
        Assert.AreEqual(IdeaStatus.Draft, after.Status);
        Assert.AreEqual(0, f.Repo.Conversions.Count);
    }

    // ================================================================== đọc

    [TestMethod]
    public async Task GetIdeas_ReturnsOnlyThisProjectsIdeas_NewestUpdatedFirst_WithMyRole()
    {
        var f = new Fixture(CreatorId);
        f.Repo.Seed(ProjectId, OwnerId, "Cũ nhất");
        f.Repo.Seed(OtherProjectId, OtherCreatorId, "Project khác");
        f.Repo.Seed(ProjectId, CreatorId, "Mới nhất");

        var result = await f.Service.GetIdeasAsync(ProjectId, null, null, null);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new[] { "Mới nhất", "Cũ nhất" }, result.Value!.Ideas.Select(i => i.Title).ToArray());
        Assert.AreEqual("Creator", result.Value.MyRole);
    }

    [TestMethod]
    public async Task GetIdeas_FiltersByStatus_Search_AndTag_CaseInsensitive()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Seed(ProjectId, CreatorId, "Review điện thoại", IdeaStatus.Backlog, tags: ["Tech"]);
        f.Repo.Seed(ProjectId, CreatorId, "Vlog Đà Lạt", IdeaStatus.Draft, note: "quay buổi sáng", tags: ["Travel", "Vlog"]);
        f.Repo.Seed(ProjectId, CreatorId, "Cũ", IdeaStatus.Archived, tags: ["Tech"]);

        var backlog = await f.Service.GetIdeasAsync(ProjectId, null, "backlog", null);
        var byNote = await f.Service.GetIdeasAsync(ProjectId, "BUỔI SÁNG", null, null);
        var byTag = await f.Service.GetIdeasAsync(ProjectId, null, null, "tech");
        var combined = await f.Service.GetIdeasAsync(ProjectId, null, "Archived", "TECH");

        CollectionAssert.AreEqual(new[] { "Review điện thoại" }, backlog.Value!.Ideas.Select(i => i.Title).ToArray());
        CollectionAssert.AreEqual(new[] { "Vlog Đà Lạt" }, byNote.Value!.Ideas.Select(i => i.Title).ToArray());
        CollectionAssert.AreEquivalent(new[] { "Review điện thoại", "Cũ" }, byTag.Value!.Ideas.Select(i => i.Title).ToArray());
        CollectionAssert.AreEqual(new[] { "Cũ" }, combined.Value!.Ideas.Select(i => i.Title).ToArray());
    }

    [TestMethod]
    [DataRow("Nope")]
    [DataRow("1")]               // số: Enum.TryParse sẽ hiểu thành Backlog
    [DataRow("Draft,Backlog")]   // danh sách: Enum.TryParse cộng cờ thành một giá trị hợp lệ
    [DataRow("99")]
    public async Task GetIdeas_InvalidStatus_Returns400WithStatusField(string status)
    {
        var f = new Fixture(OwnerId);
        f.Repo.Seed(ProjectId, CreatorId, "A");

        var result = await f.Service.GetIdeasAsync(ProjectId, null, status, null);

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("validation", result.ErrorCode);
        Assert.AreEqual("status", result.ErrorField);
    }

    [TestMethod]
    public async Task GetTags_ReturnsDistinctProjectTags_SortedAndScopedToProject()
    {
        var f = new Fixture(CreatorId);
        f.Repo.Seed(ProjectId, OwnerId, tags: ["b", "a"]);
        f.Repo.Seed(ProjectId, OwnerId, tags: ["a"]);
        f.Repo.Seed(OtherProjectId, OwnerId, tags: ["khac"]);

        var result = await f.Service.GetTagsAsync(ProjectId);

        CollectionAssert.AreEqual(new[] { "a", "b" }, result.Value!.Tags.ToArray());
    }

    // ================================================================== tạo

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    [DataRow(CreatorId)]
    public async Task Create_AnyMember_StoresTrimmedValues_AndAuthorComesFromJwt(long actingUserId)
    {
        var f = new Fixture(actingUserId);

        var result = await f.Service.CreateAsync(ProjectId, Req("  Quay vlog  ", "Backlog", "  mô tả  ", "  ghi chú  "));

        Assert.IsTrue(result.Succeeded);
        IdeaResponse created = result.Value!;
        Assert.AreEqual("Quay vlog", created.Title);
        Assert.AreEqual("mô tả", created.Description);
        Assert.AreEqual("ghi chú", created.Note);
        Assert.AreEqual("Backlog", created.Status);
        Assert.AreEqual(ProjectId, created.ProjectId);
        Assert.AreEqual(actingUserId, created.CreatedByUserId);
        Assert.IsNull(created.ConvertedContentId);
    }

    [TestMethod]
    public async Task Create_NormalizesTags_AndReusesTheSpellingAlreadyInTheProject()
    {
        var f = new Fixture(CreatorId);
        f.Repo.ExtraProjectTags.Add("YouTube");

        var result = await f.Service.CreateAsync(ProjectId, Req(tags: ["#youtube", "  Vlog  ", "vlog", "#", "  "]));

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEquivalent(new[] { "YouTube", "Vlog" }, result.Value!.Tags.ToArray());
    }

    [TestMethod]
    public async Task Create_InvalidInput_Returns400_AndStoresNothing()
    {
        var f = new Fixture(CreatorId);
        var cases = new (string Label, IdeaRequest Request)[]
        {
            ("tiêu đề rỗng", Req("   ")),
            ("tiêu đề 251 ký tự", Req(new string('a', 251))),
            ("mô tả 2001 ký tự", Req(description: new string('a', 2001))),
            ("ghi chú 2001 ký tự", Req(note: new string('a', 2001))),
            ("11 tag", Req(tags: Enumerable.Range(1, 11).Select(i => $"t{i}").ToArray())),
            ("tag 81 ký tự", Req(tags: [new string('a', 81)])),
            ("trạng thái lạ", Req(status: "Nope")),
            ("trạng thái rỗng", Req(status: "")),
            ("trạng thái là số", Req(status: "1")),
            ("trạng thái là danh sách", Req(status: "Draft,Backlog")),
            ("đặt tay Converted", Req(status: "Converted")),
        };

        foreach (var (label, request) in cases)
        {
            var result = await f.Service.CreateAsync(ProjectId, request);

            Assert.AreEqual(400, result.Status, label);
            Assert.AreEqual("validation", result.ErrorCode, label);
        }
        Assert.AreEqual(0, f.Repo.Ideas.Count);
    }

    [TestMethod]
    public async Task Create_BoundaryLengths_AreAccepted()
    {
        var f = new Fixture(CreatorId);
        string[] tags = Enumerable.Range(0, 10).Select(i => i.ToString().PadRight(80, 'x')).ToArray();

        var result = await f.Service.CreateAsync(ProjectId,
            new IdeaRequest(new string('a', 250), new string('b', 2000), new string('c', 2000), "Draft", tags));

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(10, result.Value!.Tags.Count);
    }

    // ================================================================== sửa

    [TestMethod]
    public async Task Update_CreatorOwnIdea_Succeeds_AndReplacesTags()
    {
        var f = new Fixture(CreatorId);
        Idea own = f.Repo.Seed(ProjectId, CreatorId, "Cũ", tags: ["a", "b"]);

        var result = await f.Service.UpdateAsync(own.IdeaId, Req("Mới", "Backlog", tags: ["b", "c"]));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Mới", result.Value!.Title);
        Assert.AreEqual("Backlog", result.Value.Status);
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, result.Value.Tags.ToArray());
        Assert.AreEqual(CreatorId, result.Value.CreatedByUserId); // người tạo không bị đổi
    }

    [TestMethod]
    public async Task Update_CreatorOthersIdea_Returns403_AndKeepsIdea()
    {
        var f = new Fixture(CreatorId);
        Idea others = f.Repo.Seed(ProjectId, OtherCreatorId, "Của người khác");

        var result = await f.Service.UpdateAsync(others.IdeaId, Req("Sửa trộm"));

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual("forbidden", result.ErrorCode);
        Assert.AreEqual("Của người khác", f.Repo.Get(others.IdeaId).Title);
    }

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    public async Task Update_OwnerAndManager_CanEditAnyIdea(long actingUserId)
    {
        var f = new Fixture(actingUserId);
        Idea others = f.Repo.Seed(ProjectId, CreatorId, "Của creator");

        var result = await f.Service.UpdateAsync(others.IdeaId, Req("Đã chỉnh"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Đã chỉnh", f.Repo.Get(others.IdeaId).Title);
    }

    [TestMethod]
    public async Task Update_ConvertedIdea_CannotLeaveConverted_ButTextCanStillBeEdited()
    {
        var f = new Fixture(OwnerId);
        Idea converted = f.Repo.Seed(ProjectId, CreatorId, "Đã chuyển", IdeaStatus.Converted);

        var backToDraft = await f.Service.UpdateAsync(converted.IdeaId, Req("Đã chuyển", "Draft"));
        var textOnly = await f.Service.UpdateAsync(converted.IdeaId, Req("Đã chuyển (sửa chữ)", "Converted"));

        Assert.AreEqual(400, backToDraft.Status);
        Assert.AreEqual(IdeaStatus.Converted, f.Repo.Get(converted.IdeaId).Status);
        Assert.IsTrue(textOnly.Succeeded);
        Assert.AreEqual("Đã chuyển (sửa chữ)", textOnly.Value!.Title);
        Assert.AreEqual("Converted", textOnly.Value.Status);
    }

    [TestMethod]
    public async Task Update_CannotSetConvertedByHand()
    {
        var f = new Fixture(OwnerId);
        Idea draft = f.Repo.Seed(ProjectId, CreatorId, "Nháp");

        var result = await f.Service.UpdateAsync(draft.IdeaId, Req("Nháp", "Converted"));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual(IdeaStatus.Draft, f.Repo.Get(draft.IdeaId).Status);
    }

    [TestMethod]
    public async Task Update_UnknownIdea_Returns404()
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.UpdateAsync(12345, Req());

        Assert.AreEqual(404, result.Status);
        Assert.AreEqual("not_found", result.ErrorCode);
    }

    // ================================================================== chuyển thành Content (SCRUM-31)

    [TestMethod]
    [DataRow(IdeaStatus.Draft)]
    [DataRow(IdeaStatus.Backlog)]
    public async Task Convert_DraftOrBacklog_CreatesContent_KeepsSourceLink_AndMarksConverted(IdeaStatus status)
    {
        var f = new Fixture(CreatorId);
        Idea idea = f.Repo.Seed(ProjectId, CreatorId, "Vlog Đà Lạt", status, tags: ["travel"]);

        var result = await f.Service.ConvertAsync(idea.IdeaId);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(idea.IdeaId, result.Value!.IdeaId);
        Idea after = f.Repo.Get(idea.IdeaId);
        Assert.AreEqual(IdeaStatus.Converted, after.Status);
        Assert.AreEqual(result.Value.ContentId, after.ConvertedContentId);

        var conversion = f.Repo.Conversions.Single();
        Assert.AreEqual("Short video", conversion.ContentType);
        Assert.AreEqual(CreatorId, conversion.By);
        StringAssert.Contains(conversion.Note, "IDEA-001");
        StringAssert.Contains(conversion.Note, "Vlog Đà Lạt");
    }

    [TestMethod]
    [DataRow(IdeaStatus.Archived, "lưu trữ")]
    [DataRow(IdeaStatus.Converted, "đã được chuyển")]
    public async Task Convert_ArchivedOrAlreadyConverted_Returns400_AndCreatesNoContent(IdeaStatus status, string expectedFragment)
    {
        var f = new Fixture(OwnerId);
        Idea idea = f.Repo.Seed(ProjectId, CreatorId, "Không chuyển được", status);

        var result = await f.Service.ConvertAsync(idea.IdeaId);

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("validation", result.ErrorCode);
        StringAssert.Contains(result.Message!, expectedFragment);
        Assert.AreEqual(0, f.Repo.Conversions.Count);
    }

    [TestMethod]
    public async Task Convert_CreatorCannotConvertOthersIdea_ButOwnerCan()
    {
        var f = new Fixture(CreatorId);
        Idea others = f.Repo.Seed(ProjectId, OtherCreatorId, "Của người khác");
        var denied = await f.Service.ConvertAsync(others.IdeaId);

        var allowed = await f.ServiceFor(OwnerId).ConvertAsync(others.IdeaId);

        Assert.AreEqual(403, denied.Status);
        Assert.IsTrue(allowed.Succeeded);
        Assert.AreEqual(OwnerId, f.Repo.Conversions.Single().By);
    }

    [TestMethod]
    public async Task Convert_TitleLongerThanContentLimit_Returns400_AndDoesNotConvert()
    {
        var f = new Fixture(OwnerId);
        Idea idea = f.Repo.Seed(ProjectId, CreatorId, new string('a', 201)); // Idea cho 250, Content chỉ 200

        var result = await f.Service.ConvertAsync(idea.IdeaId);

        Assert.AreEqual(400, result.Status);
        StringAssert.Contains(result.Message!, "201");
        Assert.AreEqual(IdeaStatus.Draft, f.Repo.Get(idea.IdeaId).Status);
        Assert.AreEqual(0, f.Repo.Conversions.Count);
    }

    [TestMethod]
    public async Task Convert_TitleAtContentLimit_IsAccepted()
    {
        var f = new Fixture(OwnerId);
        Idea idea = f.Repo.Seed(ProjectId, CreatorId, new string('a', 200));

        var result = await f.Service.ConvertAsync(idea.IdeaId);

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task Convert_WhenAnotherUserConvertedItFirst_Returns409()
    {
        var f = new Fixture(OwnerId);
        Idea idea = f.Repo.Seed(ProjectId, CreatorId, "Hai người cùng bấm");
        f.Repo.LoseConvertRace = true;

        var result = await f.Service.ConvertAsync(idea.IdeaId);

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("validation", result.ErrorCode);
    }

    [TestMethod]
    public async Task Convert_Twice_SecondAttemptIsRejected_AndOnlyOneContentIsCreated()
    {
        var f = new Fixture(OwnerId);
        Idea idea = f.Repo.Seed(ProjectId, CreatorId, "Chỉ một Content");

        var first = await f.Service.ConvertAsync(idea.IdeaId);
        var second = await f.Service.ConvertAsync(idea.IdeaId);

        Assert.IsTrue(first.Succeeded);
        Assert.AreEqual(400, second.Status);
        Assert.AreEqual(1, f.Repo.Conversions.Count);
    }

    [TestMethod]
    public async Task Convert_UnknownIdea_Returns404()
    {
        var f = new Fixture(OwnerId);

        Assert.AreEqual(404, (await f.Service.ConvertAsync(777)).Status);
    }

    // ================================================================== xóa

    [TestMethod]
    public async Task Delete_CreatorOwnIdea_Succeeds()
    {
        var f = new Fixture(CreatorId);
        Idea own = f.Repo.Seed(ProjectId, CreatorId);

        var result = await f.Service.DeleteAsync(own.IdeaId);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(own.IdeaId, result.Value!.IdeaId);
        Assert.AreEqual(0, f.Repo.Ideas.Count);
    }

    [TestMethod]
    public async Task Delete_CreatorOthersIdea_Returns403_AndKeepsIdea()
    {
        var f = new Fixture(CreatorId);
        Idea others = f.Repo.Seed(ProjectId, OtherCreatorId);

        var result = await f.Service.DeleteAsync(others.IdeaId);

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(1, f.Repo.Ideas.Count);
    }

    [TestMethod]
    [DataRow(OwnerId)]
    [DataRow(ManagerId)]
    public async Task Delete_OwnerAndManager_CanDeleteAnyIdea(long actingUserId)
    {
        var f = new Fixture(actingUserId);
        Idea others = f.Repo.Seed(ProjectId, CreatorId);

        var result = await f.Service.DeleteAsync(others.IdeaId);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, f.Repo.Ideas.Count);
    }

    [TestMethod]
    public async Task Delete_ConvertedIdea_Returns400_ForEveryRole()
    {
        var f = new Fixture(OwnerId);
        Idea converted = f.Repo.Seed(ProjectId, OwnerId, "Đã chuyển", IdeaStatus.Converted);

        var result = await f.Service.DeleteAsync(converted.IdeaId);

        Assert.AreEqual(400, result.Status);
        StringAssert.Contains(result.Message!, "không thể xóa");
        Assert.AreEqual(1, f.Repo.Ideas.Count);
    }

    [TestMethod]
    public async Task Delete_UnknownIdea_Returns404()
    {
        var f = new Fixture(OwnerId);

        Assert.AreEqual(404, (await f.Service.DeleteAsync(42)).Status);
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
            Service = ServiceFor(actingUserId);
        }

        public IdeaRepositoryFake Repo { get; init; } = new();
        public MemberRolesFake Tasks { get; } = new();
        public IdeaService Service { get; }

        public IdeaService ServiceFor(long? userId) => new(Repo, Tasks, new CurrentAuthenticatedUser
        {
            User = userId is { } id
                ? new User { UserId = id, Email = $"user{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                : null,
        });
    }
}
