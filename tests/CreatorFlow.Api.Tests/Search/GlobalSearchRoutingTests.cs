using System.Net;
using System.Net.Http.Json;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Search;
using CreatorFlow.Api.Repositories.Search;
using CreatorFlow.Api.Services.Search;
using CreatorFlow.Api.Tests.Api;
using CreatorFlow.Contracts.Search;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Search;

[TestClass]
[TestCategory("GlobalSearchApiRouting")]
public sealed class GlobalSearchRoutingTests
{
    [TestMethod]
    public async Task SearchRoute_RequiresLoginTrimsQueryAndUsesBoundedLimit()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var repository = new SearchRepositoryFake();
        builder.Services.AddSingleton<IGlobalSearchRepository>(repository);
        builder.Services.AddScoped(_ => new CurrentAuthenticatedUser
        {
            User = new User
            {
                UserId = 42,
                Email = "search@example.test",
                DisplayName = "Search User",
                PasswordHash = "unused",
            },
        });
        builder.Services.AddScoped<GlobalSearchService>();
        builder.Services.AddAuthentication("fixture")
            .AddScheme<AuthenticationSchemeOptions, ProjectRoutingTests.FixtureAuth>("fixture", _ => { });
        builder.Services.AddAuthorization();

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGlobalSearchEndpoints();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/search?q=content")).StatusCode);
        Assert.AreEqual(0, repository.CallCount);

        client.DefaultRequestHeaders.Authorization = new("fixture", "actor");
        using HttpResponseMessage response = await client.GetAsync("/api/search?q=%20ban%20&limit=500");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        GlobalSearchResponse? body = await response.Content.ReadFromJsonAsync<GlobalSearchResponse>();
        Assert.IsNotNull(body);
        Assert.AreEqual("ban", repository.LastQuery);
        Assert.AreEqual(50, repository.LastLimit);

        using HttpResponseMessage shortQuery = await client.GetAsync("/api/search?q=x");
        Assert.AreEqual(HttpStatusCode.OK, shortQuery.StatusCode);
        Assert.AreEqual(0, (await shortQuery.Content.ReadFromJsonAsync<GlobalSearchResponse>())!.Items.Count);
        Assert.AreEqual(1, repository.CallCount);

        using HttpResponseMessage longQuery = await client.GetAsync("/api/search?q=" + new string('a', 101));
        Assert.AreEqual(HttpStatusCode.BadRequest, longQuery.StatusCode);
        Assert.AreEqual(1, repository.CallCount);
    }

    private sealed class SearchRepositoryFake : IGlobalSearchRepository
    {
        public int CallCount { get; private set; }
        public string? LastQuery { get; private set; }
        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<GlobalSearchRecord>> SearchAsync(
            long userId, string query, int limit, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastQuery = query;
            LastLimit = limit;
            return Task.FromResult<IReadOnlyList<GlobalSearchRecord>>(Array.Empty<GlobalSearchRecord>());
        }
    }
}
