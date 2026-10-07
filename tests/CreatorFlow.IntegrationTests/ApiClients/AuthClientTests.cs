using System.Net;
using System.Net.Http.Json;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Users;
using CreatorFlow.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[TestCategory("AuthClientUnit")]
public sealed class AuthClientTests
{
    [TestMethod]
    public async Task LoginProfileLogout_UsesBearerAndSafeMemorySession()
    {
        var session = new UserSession(); string? receivedBearer = null;
        using var http = new HttpClient(new Handler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/login")) return Task.FromResult(Response(Login()));
            receivedBearer = request.Headers.Authorization?.ToString();
            return Task.FromResult(Response(new ProfileResponse(42, "fixture@example.test", "Fixture", "Active", false, null)));
        })) { BaseAddress = new Uri("http://fixture.invalid/") };
        using var api = new ApiClient(http); var facade = new AuthApiFacade(api, session);
        Assert.IsTrue((await facade.LoginAsync("fixture@example.test", "Fixture1!")).Succeeded);
        Assert.IsTrue(session.IsAuthenticated); Assert.AreEqual(42L, session.CurrentUser!.UserId);
        Assert.IsTrue((await facade.GetCurrentProfileAsync()).Succeeded);
        Assert.AreEqual("Bearer fixture-token", receivedBearer);
        facade.Logout(); Assert.IsFalse(session.IsAuthenticated); Assert.IsNull(session.CurrentUser); Assert.IsNull(session.AccessToken);
    }

    [TestMethod]
    public async Task LateLoginAfterClear_DoesNotRestoreSession()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new Handler((_, _) => response.Task)) { BaseAddress = new Uri("http://fixture.invalid/") };
        using var api = new ApiClient(http); var session = new UserSession(); var facade = new AuthApiFacade(api, session);
        Task<LoginResult> pending = facade.LoginAsync("fixture@example.test", "Fixture1!");
        facade.Logout(); response.SetResult(Response(Login()));
        Assert.IsFalse((await pending).Succeeded); Assert.IsNull(session.CurrentUser); Assert.IsNull(session.AccessToken);
    }

    [TestMethod]
    public async Task ProtectedUnauthorizedClearsSession_ServiceUnavailableDoesNot()
    {
        HttpStatusCode status = HttpStatusCode.ServiceUnavailable;
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(status)
            { Content = JsonContent.Create(new { title = "Raw detail must not be displayed", detail = "fixture-private-marker" }) })))
            { BaseAddress = new Uri("http://fixture.invalid/") };
        using var api = new ApiClient(http); var session = new UserSession(); session.TrySetLogin(Login(), session.Generation);
        var facade = new AuthApiFacade(api, session);
        var unavailable = await facade.GetCurrentProfileAsync();
        Assert.IsFalse(unavailable.Succeeded); Assert.IsTrue(session.IsAuthenticated);
        Assert.IsFalse(unavailable.ErrorMessage!.Contains("fixture-private-marker"));
        status = HttpStatusCode.Unauthorized;
        Assert.IsFalse((await facade.GetCurrentProfileAsync()).Succeeded); Assert.IsFalse(session.IsAuthenticated); Assert.IsNull(session.AccessToken);
    }

    private static LoginResponse Login() => new("fixture-token", DateTimeOffset.UtcNow.AddMinutes(30), new(42, "fixture@example.test", "Fixture", false));
    private static HttpResponseMessage Response<T>(T response) => new(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
}
