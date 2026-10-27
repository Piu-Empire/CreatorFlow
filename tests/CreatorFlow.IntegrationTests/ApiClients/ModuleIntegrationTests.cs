using System.Net;
using System.Text;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
public sealed class ModuleIntegrationTests
{
    private static UserSession SignedIn(string token = "current-token")
    {
        var session = new UserSession();
        Login(session, token);
        return session;
    }

    private static void Login(UserSession session, string token) => Assert.IsTrue(session.TrySetLogin(
        new LoginResponse(token, DateTimeOffset.UtcNow.AddMinutes(20), new(7, "owner@example.test", "Owner", false)), session.Generation));

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [TestMethod]
    [DataRow(401)]
    [DataRow(403)]
    public void ProtectedRequests_UseCurrentToken_AndApplyConsistentSessionPolicy(int status)
    {
        var session = SignedIn();
        int clears = 0, rejections = 0, requests = 0;
        session.Cleared += (_, _) => clears++;
        session.AuthorizationRejected += (_, _) => rejections++;
        using var http = new HttpClient(new Handler(request =>
        {
            requests++;
            Assert.AreEqual("Bearer current-token", request.Headers.Authorization!.ToString());
            return Task.FromResult(Json("{\"code\":\"forbidden\",\"title\":\"Access rejected\"}", (HttpStatusCode)status));
        })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        Assert.ThrowsExactly<UnauthorizedWorkflowActionException>(() => new ApiMyTaskBackend(api, session).GetMyTasks(8));
        Assert.AreEqual(1, requests);
        Assert.AreEqual(status == 401 ? 1 : 0, clears);
        Assert.AreEqual(status == 403 ? 1 : 0, rejections);
        Assert.AreEqual(status != 401, session.IsAuthenticated);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NetworkAndTimeout_KeepSession_AndDoNotBecomeEmptySuccess(bool timeout)
    {
        var session = SignedIn();
        using var http = new HttpClient(new Handler(_ => Task.FromException<HttpResponseMessage>(
            timeout ? new OperationCanceledException() : new HttpRequestException()))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        Assert.ThrowsExactly<ContentValidationException>(() => new ApiMyTaskBackend(api, session).GetMyTasks(8));
        Assert.ThrowsExactly<IdeaValidationException>(() => new ApiIdeaBackend(api, session).CountBacklog(8, 7));
        Assert.IsTrue(session.IsAuthenticated);
        Assert.AreEqual("current-token", session.AccessToken);
    }

    [TestMethod]
    public async Task BoardResponse_FromPreviousLoginIsDiscardedWithoutClearingNewLogin()
    {
        var session = SignedIn();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reply = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new Handler(_ => { started.SetResult(true); return reply.Task; })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        Task<List<ContentBoardCard>> pending = new ApiBoardBackend(api, session).GetBoardCardsAsync(8);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        session.Clear();
        Login(session, "new-token");
        reply.SetResult(Json("{\"cards\":[]}"));
        await Assert.ThrowsExactlyAsync<UnauthorizedWorkflowActionException>(async () => { await pending; });
        Assert.IsTrue(session.IsAuthenticated);
        Assert.AreEqual("new-token", session.AccessToken);
    }

    [TestMethod]
    public async Task ScriptAi_ReadsSavedScriptAndContextViaApi_AndDoesNotSaveSuggestion()
    {
        var paths = new List<string>();
        var session = SignedIn();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("Bearer current-token", request.Headers.Authorization!.ToString());
            paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(request.RequestUri.AbsolutePath.EndsWith("/script")
                ? Json("""{"contentId":10,"projectId":8,"text":"Saved script","version":"v1","canEdit":true,"isContentLocked":false} """)
                : Json("""{"contentId":10,"projectId":8,"code":"CNT-010","title":"API content","description":"Context","contentType":"Reel","status":"Editing","priority":"High","createdBy":7,"platforms":["TikTok"],"role":"Owner"} """));
        })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        var ai = new CapturingAi();
        var service = new ScriptService(api, session, ai);
        var result = await service.SendToAiAsync(10, 999, AiRequestType.Outline, "Short outline");
        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(ai.Request);
        Assert.AreEqual("Saved script", ai.Request.Script);
        Assert.AreEqual("API content", ai.Request.Title);
        Assert.AreEqual(8L, ai.Request.ProjectId);
        Assert.AreEqual(7L, ai.Request.RequestedByUserId);
        CollectionAssert.AreEqual(new[] { "TikTok" }, ai.Request.Platforms.ToArray());
        CollectionAssert.AreEqual(new[] { "/api/contents/10", "/api/contents/10/script" }, paths);
    }

    [TestMethod]
    public void ScriptConflict_IsPreservedAcrossApiBoundary()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Json(
            "{\"code\":\"script_conflict\",\"title\":\"Script changed\"}", HttpStatusCode.Conflict)))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        Assert.ThrowsExactly<ScriptConflictException>(() => new ScriptService(api, SignedIn(), new NotConfiguredAiService()).Save(10, "edit", "old-version", 7));
    }

    [TestMethod]
    public void WinFormsAssembly_DoesNotReferenceDatabaseDriverOrBackend()
    {
        var references = typeof(ApiClient).Assembly.GetReferencedAssemblies();
        Assert.IsFalse(references.Any(reference => reference.Name is "Npgsql" or "CreatorFlow.Api"));
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }

    private sealed class CapturingAi : IAiService
    {
        public bool IsConfigured => true;
        public AiScriptRequest? Request { get; private set; }
        public Task<AiScriptResponse> RequestScriptAssistAsync(AiScriptRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(AiScriptResponse.Success("Suggestion awaiting user review"));
        }
    }
}
