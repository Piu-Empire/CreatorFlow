using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

/// <summary>SCRUM-33: lưu script dài, phân quyền sửa, chống ghi đè và truyền script sang AIService.</summary>
[TestClass]
public sealed class ScriptServiceTests
{
    private const long ProjectId = 1;
    private const long OwnerId = 1;
    private const long CreatorAId = 2;   // người tạo Content trong các test
    private const long ManagerId = 3;
    private const long CreatorBId = 4;   // Creator khác, mặc định không liên quan Content
    private const long OutsiderId = 99;

    private const string SeedScript = "[Hook 0-3s] Mở đầu\n[Body] Nội dung chính";

    // ---------- Đọc ----------

    [TestMethod]
    public void Open_Author_ReturnsTextStatsVersionAndEditable()
    {
        var (service, id, _) = Build();

        var doc = service.Open(id, CreatorAId);

        Assert.AreEqual(SeedScript, doc.Text);
        Assert.IsTrue(doc.CanEdit);
        Assert.AreEqual(ScriptTextAnalyzer.ComputeVersion(SeedScript), doc.Version);
        Assert.IsTrue(doc.Statistics.Words > 0);
    }

    [TestMethod]
    public void Open_NotProjectMember_Throws()
    {
        var (service, id, _) = Build();

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => service.Open(id, OutsiderId));
    }

    [TestMethod]
    public void Open_OtherCreator_CanViewButNotEdit()
    {
        var (service, id, _) = Build();

        var doc = service.Open(id, CreatorBId);

        Assert.IsFalse(doc.CanEdit);
        Assert.AreEqual(SeedScript, doc.Text);
    }

    // ---------- Lưu ----------

    [TestMethod]
    public void Save_LongScript_RoundTrips()
    {
        var (service, id, _) = Build();
        string longScript = string.Join('\n', Enumerable.Repeat("Một câu kịch bản khá dài để kiểm tra lưu script lớn.", 1500));
        Assert.IsTrue(longScript.Length > 70_000);
        var doc = service.Open(id, CreatorAId);

        var saved = service.Save(id, longScript, doc.Version, CreatorAId);

        Assert.AreEqual(longScript, saved.Text);
        Assert.AreEqual(longScript, service.Open(id, ManagerId).Text);
    }

    [TestMethod]
    public void Save_OverLimit_ThrowsValidation_AndKeepsOldScript()
    {
        var (service, id, _) = Build();
        var doc = service.Open(id, CreatorAId);

        Assert.ThrowsExactly<ContentValidationException>(
            () => service.Save(id, new string('a', ScriptService.MaxScriptLength + 1), doc.Version, CreatorAId));

        Assert.AreEqual(SeedScript, service.Open(id, CreatorAId).Text);
    }

    [TestMethod]
    public void Save_WindowsNewLines_AreStoredAsLf()
    {
        var (service, id, _) = Build();
        var doc = service.Open(id, CreatorAId);

        var saved = service.Save(id, "Dòng 1\r\nDòng 2", doc.Version, CreatorAId);

        Assert.AreEqual("Dòng 1\nDòng 2", saved.Text);
    }

    [TestMethod]
    public void Save_Manager_CanEditAnyContent()
    {
        var (service, id, _) = Build();
        var doc = service.Open(id, ManagerId);

        var saved = service.Save(id, "Manager sửa", doc.Version, ManagerId);

        Assert.AreEqual("Manager sửa", saved.Text);
    }

    [TestMethod]
    public void Save_CreatorNotAuthorNotAssigned_ThrowsUnauthorized()
    {
        var (service, id, _) = Build();
        var doc = service.Open(id, CreatorBId);

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(
            () => service.Save(id, "Cố sửa", doc.Version, CreatorBId));
        Assert.AreEqual(SeedScript, service.Open(id, CreatorAId).Text);
    }

    [TestMethod]
    public void Save_AssignedCreator_CanEdit()
    {
        var (service, id, _) = Build();
        new InMemoryMyTaskRepository().AddAssignment(id, CreatorBId, ManagerId, null);
        var doc = service.Open(id, CreatorBId);
        Assert.IsTrue(doc.CanEdit);

        var saved = service.Save(id, "Người được giao sửa", doc.Version, CreatorBId);

        Assert.AreEqual("Người được giao sửa", saved.Text);
    }

    [TestMethod]
    public void Save_PublishedContent_ThrowsValidation()
    {
        var (service, id, _) = Build(ContentStatus.Published);
        var doc = service.Open(id, ManagerId);
        Assert.IsFalse(doc.CanEdit);

        Assert.ThrowsExactly<ContentValidationException>(
            () => service.Save(id, "Sửa sau khi đăng", doc.Version, ManagerId));
    }

    [TestMethod]
    public void Save_StaleVersion_ThrowsConflict_AndKeepsOtherUsersChange()
    {
        var (service, id, _) = Build();
        var openedByAuthor = service.Open(id, CreatorAId);
        var openedByManager = service.Open(id, ManagerId);
        service.Save(id, "Bản của Manager", openedByManager.Version, ManagerId);

        Assert.ThrowsExactly<ScriptConflictException>(
            () => service.Save(id, "Bản của tác giả", openedByAuthor.Version, CreatorAId));

        Assert.AreEqual("Bản của Manager", service.Open(id, OwnerId).Text);
    }

    [TestMethod]
    public void Save_WhitespaceOnly_ClearsScript()
    {
        var (service, id, _) = Build();
        var doc = service.Open(id, CreatorAId);

        var saved = service.Save(id, "  \r\n ", doc.Version, CreatorAId);

        Assert.AreEqual(string.Empty, saved.Text);
        Assert.IsNull(new InMemoryContentScriptRepository().GetByContentId(id)!.Script);
    }

    [TestMethod]
    public void Save_Unchanged_KeepsVersion()
    {
        var (service, id, _) = Build();
        var doc = service.Open(id, CreatorAId);

        var saved = service.Save(id, SeedScript + "\r\n", doc.Version, CreatorAId); // chỉ khác khoảng trắng cuối

        Assert.AreEqual(doc.Version, saved.Version);
    }

    // ---------- AI ----------

    [TestMethod]
    public void PrepareAiRequest_CarriesContextSectionsAndStatistics()
    {
        var (service, id, _) = Build();

        var request = service.PrepareAiRequest(id, CreatorBId, AiRequestType.Outline, "  rút gọn 30 giây ");

        Assert.AreEqual(id, request.ContentId);
        Assert.AreEqual(CreatorBId, request.RequestedByUserId);
        Assert.AreEqual(AiRequestType.Outline, request.RequestType);
        Assert.AreEqual("Video test", request.Title);
        Assert.AreEqual("Short video", request.ContentType);
        CollectionAssert.AreEqual(new[] { "TikTok" }, request.Platforms.ToArray());
        Assert.AreEqual(SeedScript, request.Script);
        Assert.IsFalse(request.IsScriptTruncated);
        Assert.AreEqual(2, request.Sections.Count);
        Assert.AreEqual("rút gọn 30 giây", request.Instruction);
        Assert.IsTrue(request.Statistics.Words > 0);
    }

    [TestMethod]
    public void PrepareAiRequest_VeryLongScript_IsTruncatedButStatisticsKeepFullSize()
    {
        var (service, id, _) = Build();
        string longScript = string.Join('\n', Enumerable.Repeat("Câu kịch bản lặp lại nhiều lần.", 2000));
        service.Save(id, longScript, service.Open(id, CreatorAId).Version, CreatorAId);

        var request = service.PrepareAiRequest(id, CreatorAId);

        Assert.IsTrue(request.IsScriptTruncated);
        Assert.IsTrue(request.Script.Length <= ScriptAiRequestBuilder.MaxScriptCharacters);
        Assert.AreEqual(longScript.Length, request.Statistics.Characters);
    }

    [TestMethod]
    public void PrepareAiRequest_EmptyScript_ThrowsValidation()
    {
        var (service, id, _) = Build(script: string.Empty);

        Assert.ThrowsExactly<ContentValidationException>(() => service.PrepareAiRequest(id, CreatorAId));
    }

    [TestMethod]
    public void PrepareAiRequest_NotProjectMember_Throws()
    {
        var (service, id, _) = Build();

        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => service.PrepareAiRequest(id, OutsiderId));
    }

    [TestMethod]
    public void PrepareAiRequest_UnsupportedType_ThrowsValidation()
    {
        var (service, id, _) = Build();

        Assert.ThrowsExactly<ContentValidationException>(
            () => service.PrepareAiRequest(id, CreatorAId, AiRequestType.PerformanceAnalysis));
    }

    [TestMethod]
    public async Task SendToAiAsync_Configured_PassesPreparedRequestToAiService()
    {
        var (service, id, ai) = Build();

        var response = await service.SendToAiAsync(id, CreatorAId);

        Assert.IsTrue(response.Succeeded);
        Assert.IsNotNull(ai.LastRequest);
        Assert.AreEqual(SeedScript, ai.LastRequest.Script);
    }

    [TestMethod]
    public async Task SendToAiAsync_NotConfigured_ReturnsFailureWithoutCallingService()
    {
        var (service, id, ai) = Build(aiConfigured: false);

        var response = await service.SendToAiAsync(id, CreatorAId);

        Assert.IsFalse(response.Succeeded);
        Assert.AreEqual(NotConfiguredAiService.Message, response.ErrorMessage);
        Assert.IsNull(ai.LastRequest);
    }

    [TestMethod]
    public async Task SendToAiAsync_AiServiceThrows_ReturnsFailure()
    {
        var (service, id, _) = Build(aiError: new HttpRequestException("secret-host"));

        var response = await service.SendToAiAsync(id, CreatorAId);

        Assert.IsFalse(response.Succeeded);
        Assert.IsFalse(response.ErrorMessage!.Contains("secret-host"));
    }

    // ---------- Helpers ----------

    private static (ScriptService Service, long ContentId, FakeAiService Ai) Build(
        ContentStatus status = ContentStatus.Script,
        string script = SeedScript,
        bool aiConfigured = true,
        Exception? aiError = null)
    {
        var details = new InMemoryContentDetailsRepository();
        long id = details.Create(ProjectId, status, new ContentDraft
        {
            Title = "Video test",
            Description = "Concept hook",
            ContentType = "Short video",
            Platforms = { "TikTok" },
            Script = script,
        }, CreatorAId);

        var ai = new FakeAiService(aiConfigured, aiError);
        var service = new ScriptService(
            new InMemoryContentScriptRepository(),
            details,
            new InMemoryPlatformRepository(),
            new InMemoryProjectMemberRepository(),
            new InMemoryMyTaskRepository(),
            new InMemoryUnitOfWork(),
            ai);
        return (service, id, ai);
    }

    private sealed class FakeAiService(bool configured, Exception? error) : IAiService
    {
        public bool IsConfigured => configured;

        public AiScriptRequest? LastRequest { get; private set; }

        public Task<AiScriptResponse> RequestScriptAssistAsync(AiScriptRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            if (error is not null)
                throw error;
            return Task.FromResult(AiScriptResponse.Success("ok"));
        }
    }
}
