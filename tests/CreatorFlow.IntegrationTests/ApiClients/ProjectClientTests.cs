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
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token); }
}
