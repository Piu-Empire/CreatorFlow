using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.Repositories.Projects;
using CreatorFlow.Api.Services.Projects;
using CreatorFlow.Api.Tests.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Api;

[TestClass]
[TestCategory("ProjectApiRouting")]
public sealed class ProjectRoutingTests
{
    [TestMethod]
    public async Task Routes_RequireAuthenticationAndRejectInjectedAuthority()
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
        var repo = new ProjectFake(); builder.Services.AddSingleton<IProjectRepository>(repo);
        builder.Services.AddScoped(_ => ProjectServiceTests.Actor()); builder.Services.AddScoped<ProjectService>();
        builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, FixtureAuth>("fixture", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapProjectEndpoints(); await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var (method, path) in new[] { (HttpMethod.Get, "/api/projects"), (HttpMethod.Get, "/api/projects/8"),
                     (HttpMethod.Get, "/api/projects/8/access"), (HttpMethod.Post, "/api/projects"),
                     (HttpMethod.Put, "/api/projects/8"), (HttpMethod.Post, "/api/projects/8/archive"), (HttpMethod.Delete, "/api/projects/8") })
        {
            using var request = new HttpRequestMessage(method, path) { Content = method == HttpMethod.Post || method == HttpMethod.Put ? JsonContent.Create(new { name = "Team" }) : null };
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode, path);
        }
        client.DefaultRequestHeaders.Authorization = new("fixture", "actor");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/projects", new { name = "Team", ownerId = 99 })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/projects", new { name = "Team", description = "new" })).StatusCode);
        Assert.AreEqual(42L, repo.CreatedUser);
        repo.Project = ProjectFake.Record("CREATOR");
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/projects/8")).StatusCode);
        repo.Project = null;
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/projects/8/access")).StatusCode);
        string list = await client.GetStringAsync("/api/projects?userId=99"); Assert.IsFalse(list.Contains("Team"));
    }
    public sealed class FixtureAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
            Request.Headers.Authorization.Count == 0 ? AuthenticateResult.NoResult() : AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")], Scheme.Name)), Scheme.Name)));
    }
}
