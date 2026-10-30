using System.Net;
using System.Net.Http.Json;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[TestCategory("ProjectClient")]
[DoNotParallelize]
public sealed class ProjectClientTests
{
    private static UserSession Session()
    {
        var session = new UserSession(); session.TrySetLogin(new LoginResponse("fixture-token", DateTimeOffset.UtcNow.AddMinutes(30), new(42, "fixture@example.test", "Fixture", false)), 0); return session;
    }
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task UnauthenticatedRequests_DoNotRepeatClearOrSendHttp_ExpiredCredentialsAreRemoved(bool expired)
    {
        var session = expired ? Session() : new UserSession();
        if (expired) typeof(UserSession).GetProperty(nameof(UserSession.ExpiresAtUtc))!
            .SetValue(session, DateTimeOffset.UtcNow.AddMinutes(-1));
        long initialGeneration = session.Generation;
        int clears = 0, requests = 0;
        session.Cleared += (_, _) => clears++;
        using var http = new HttpClient(new Handler((_, _) =>
        {
            requests++;
            throw new AssertFailedException("Unauthenticated request must not reach HTTP.");
        })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        using var projects = new ProjectApiFacade(api, session);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            var error = await Assert.ThrowsExactlyAsync<ApiBusinessException>(() => projects.ListAsync(default));
            Assert.AreEqual(HttpStatusCode.Unauthorized, error.StatusCode);
        }

        Assert.AreEqual(expired ? 1 : 0, clears);
        Assert.AreEqual(initialGeneration + (expired ? 1 : 0), session.Generation);
        Assert.IsNull(session.AccessToken);
        Assert.IsNull(session.CurrentUser);
        Assert.AreEqual(0, requests);
    }
    [TestMethod]
    public void Context_RejectsArchivedAndClearsIdNameRole()
    {
        var context = new ProjectContext(); context.Apply(new(81, "A", "OWNER", "ACTIVE")); long version = context.Generation;
        Assert.AreEqual(81L, CurrentSession.CurrentProjectId);
        try { context.Apply(new(82, "B", "CREATOR", "ARCHIVED")); Assert.Fail("Archived must be rejected"); } catch (ApiException) { }
        Assert.AreEqual(81L, context.Current!.ProjectId);
        context.Clear(); Assert.IsNull(context.Current); Assert.AreEqual(0L, CurrentSession.CurrentProjectId); Assert.AreEqual("", CurrentSession.CurrentProjectName); Assert.IsNull(CurrentSession.CurrentUserRole); Assert.IsTrue(context.Generation > version);
    }
    [TestMethod]
    public async Task Access_UsesBearerAndNoClientRoleOrUserId()
    {
        using var http = new HttpClient(new Handler((request, _) =>
        {
            Assert.AreEqual("fixture-token", request.Headers.Authorization!.Parameter);
            Assert.AreEqual("/api/projects/81/access", request.RequestUri!.AbsolutePath);
            Assert.IsNull(request.Content);
            return Task.FromResult(Response(new ProjectAccessResponse(81, "A", "CREATOR", "ACTIVE")));
        })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());
        Assert.AreEqual("CREATOR", (await projects.AccessAsync(81, default)).CurrentUserRole);
    }

    [TestMethod]
    public async Task MemberOperations_UseProjectScopedRoutesAndAcceptProjectRoleResponses()
    {
        var calls = new List<(HttpMethod Method, string Path)>();
        using var http = new HttpClient(new Handler(async (request, _) =>
        {
            calls.Add((request.Method, request.RequestUri!.AbsolutePath));
            Assert.AreEqual("fixture-token", request.Headers.Authorization!.Parameter);
            if (request.Method == HttpMethod.Get)
                return Response(new ProjectMembersResponse([new(55, "Member", "member@example.test", "CREATOR", DateTimeOffset.UtcNow)]));
            if (request.Method == HttpMethod.Put)
            {
                var body = await request.Content!.ReadFromJsonAsync<UpdateProjectMemberRoleRequest>();
                Assert.AreEqual("MANAGER", body!.Role);
                return Response(new ProjectMemberMutationResponse(81, 55, "MANAGER", false));
            }
            return Response(new ProjectMemberMutationResponse(81, 55, "MANAGER", true));
        })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());

        Assert.AreEqual(1, (await projects.GetMembersAsync(81, default)).Members.Count);
        Assert.AreEqual("MANAGER", (await projects.UpdateMemberRoleAsync(81, 55, "MANAGER", default)).Role);
        Assert.IsTrue((await projects.RemoveMemberAsync(81, 55, default)).Removed);
        CollectionAssert.AreEqual(new[]
        {
            (HttpMethod.Get, "/api/projects/81/members"),
            (HttpMethod.Put, "/api/projects/81/members/55/role"),
            (HttpMethod.Delete, "/api/projects/81/members/55")
        }, calls.ToArray());
    }

    [TestMethod]
    public async Task Restore_UsesProjectScopedPostRouteAndReturnsActiveProject()
    {
        using var http = new HttpClient(new Handler((request, _) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/api/projects/81/restore", request.RequestUri!.AbsolutePath);
            Assert.IsNull(request.Content);
            Assert.AreEqual("fixture-token", request.Headers.Authorization!.Parameter);
            return Task.FromResult(Response(new ProjectResponse(81, "A", null, "Owner", "OWNER", "ACTIVE", DateTimeOffset.UtcNow)));
        })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());

        var result = await projects.RestoreAsync(81, default);

        Assert.AreEqual(81L, result.ProjectId);
        Assert.AreEqual("ACTIVE", result.Status);
    }

    [TestMethod]
    public async Task RestoreConflict_PreservesProjectNotArchivedBusinessCode()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
        { Content = JsonContent.Create(new { code = "project_not_archived", title = "Project is already active." }) }))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());

        try
        {
            await projects.RestoreAsync(81, default);
            Assert.Fail("Restoring an active Project must preserve the business conflict.");
        }
        catch (ApiBusinessException error)
        {
            Assert.AreEqual(HttpStatusCode.Conflict, error.StatusCode);
            Assert.AreEqual("project_not_archived", error.Code);
            Assert.AreEqual("Project is already active.", error.Message);
        }
    }

    [TestMethod]
    public async Task MemberConflict_PreservesLastOwnerBusinessCodeAndMessage()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
        { Content = JsonContent.Create(new { code = "last_owner_required", title = "Project must keep one Owner." }) }))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());
        try
        {
            await projects.RemoveMemberAsync(81, 42, default);
            Assert.Fail("Removing the last Owner must return a conflict.");
        }
        catch (ApiBusinessException error)
        {
            Assert.AreEqual(HttpStatusCode.Conflict, error.StatusCode);
            Assert.AreEqual("last_owner_required", error.Code);
            Assert.AreEqual("Project must keep one Owner.", error.Message);
        }
    }

    [TestMethod]
    public async Task LateValidationOfA_CannotClearOrRenameB()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new Handler((_, _) => { started.SetResult(); return pending.Task; })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());
        projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE")); var validating = projects.RevalidateAsync(default); await started.Task;
        projects.Context.Apply(new(82, "B", "CREATOR", "ACTIVE"));
        pending.SetResult(new(HttpStatusCode.NotFound) { Content = JsonContent.Create(new { code = "project_not_found", title = "Gone" }) });
        await validating; Assert.AreEqual(82L, projects.Context.Current!.ProjectId); Assert.AreEqual("B", projects.Context.Current.Name);
    }
    [TestMethod]
    public async Task LogoutWhileRequestPending_RejectsLateResponse()
    {
        var session = Session(); var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new Handler((_, _) => { started.SetResult(); return pending.Task; })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, session);
        projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE")); var request = projects.AccessAsync(81, default); await started.Task;
        session.Clear(); pending.SetResult(Response(new ProjectAccessResponse(81, "A", "OWNER", "ACTIVE")));
        try { await request; Assert.Fail("Late response must be rejected"); } catch (OperationCanceledException) { }
        Assert.IsNull(projects.Context.Current); Assert.AreEqual(0L, CurrentSession.CurrentProjectId);
    }
    private static HttpResponseMessage Response<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    [TestMethod]
    public async Task List_HandlesPayloadAboveAuthLimitAndRejectsMalformedItems()
    {
        bool malformed = false;
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(malformed ?
            Response(new ProjectListResponse([new(0, "Invalid", "OWNER", "ACTIVE")])) :
            Response(new ProjectListResponse(Enumerable.Range(1, 500).Select(i => new ProjectListItem(i, new string('a', 180), "OWNER", "ACTIVE")).ToArray()))))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());
        Assert.AreEqual(500, (await projects.ListAsync(default)).Items.Count);
        malformed = true;
        try { await projects.ListAsync(default); Assert.Fail("Invalid item must be rejected"); } catch (ApiException error) { Assert.AreEqual(ApiErrorKind.InvalidResponse, error.Kind); }
    }
    [TestMethod]
    [DataRow(401)]
    [DataRow(403)]
    [DataRow(404)]
    [DataRow(409)]
    public async Task HttpErrors_AreVisibleAndOnly401ClearsAuthentication(int status)
    {
        var session = Session();
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        { Content = JsonContent.Create(new { code = "project_error", title = "Visible error" }) }))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, session);
        try { await projects.AccessAsync(81, default); Assert.Fail("Error must be reported"); }
        catch (ApiBusinessException error) { Assert.AreEqual(status, (int)error.StatusCode); }
        Assert.AreEqual(status != 401, session.IsAuthenticated);
    }
    [TestMethod]
    public async Task CreateTimeout_DoesNotRetryOrReportSuccess()
    {
        int attempts = 0;
        using var http = new HttpClient(new Handler((_, _) => { attempts++; throw new OperationCanceledException(); })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());
        try { await projects.CreateAsync("Team", "", default); Assert.Fail("Timeout must be reported"); }
        catch (ApiException error) { Assert.AreEqual(ApiErrorKind.Timeout, error.Kind); }
        Assert.AreEqual(1, attempts); Assert.IsNull(projects.Context.Current);
    }
    [TestMethod]
    [DataRow("Name")]
    [DataRow("Description")]
    public async Task ProjectValidation_PreservesFieldForInlineUiErrors(string field)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        { Content = JsonContent.Create(new { code = "validation", title = "Project input is invalid", errors = new Dictionary<string, string[]> { [field] = ["Invalid value"] } }) }))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http); using var projects = new ProjectApiFacade(api, Session());
        try { await projects.CreateAsync("Team", "", default); Assert.Fail("Validation must be reported"); }
        catch (ApiBusinessException error) { Assert.AreEqual(field, error.ErrorField); }
        Assert.IsNull(projects.Context.Current);
    }
    [TestMethod]
    public async Task RevalidationAfter403_ClearsProjectButKeepsLogin()
    {
        var session = Session();
        int rejected = 0;
        session.AuthorizationRejected += (_, _) => rejected++;
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
        { Content = JsonContent.Create(new { code = "forbidden", title = "Membership removed" }) }))) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        using var projects = new ProjectApiFacade(api, session);
        projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));

        Assert.IsFalse(await projects.RevalidateAsync(default));
        Assert.IsNull(projects.Context.Current);
        Assert.IsNull(CurrentSession.CurrentUserRole);
        Assert.IsTrue(session.IsAuthenticated);
        Assert.AreEqual(1, rejected);
    }

    [TestMethod]
    public void Backend403_NotifiesUiWithoutRetryingMutationOrClearingLogin()
    {
        var session = Session();
        int attempts = 0, rejected = 0;
        session.AuthorizationRejected += (_, _) => rejected++;
        using var http = new HttpClient(new Handler((_, _) =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            { Content = JsonContent.Create(new { code = "forbidden", title = "Only Owner/Manager" }) });
        })) { BaseAddress = new("http://fixture.test/") };
        using var api = new ApiClient(http);
        var backend = new CreatorFlow.Services.Backends.ApiMyTaskBackend(api, session);
        try
        {
            backend.ChangeDeadline(10, 55, DateTime.Today.AddDays(3));
            Assert.Fail("403 must not report a successful mutation");
        }
        catch (CreatorFlow.Services.Exceptions.UnauthorizedWorkflowActionException) { }
        Assert.AreEqual(1, attempts);
        Assert.AreEqual(1, rejected);
        Assert.IsTrue(session.IsAuthenticated);
        long generation = session.Generation;
        session.Clear();
        session.NotifyAuthorizationRejected(generation);
        Assert.AreEqual(1, rejected);
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token); }
}
