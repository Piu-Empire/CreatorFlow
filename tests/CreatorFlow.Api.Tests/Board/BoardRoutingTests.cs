using System.Net;
using System.Text.Json;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Board;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Board;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Board;
using CreatorFlow.Api.Tests.Api;
using CreatorFlow.Api.Tests.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Board;

[TestClass]
[TestCategory("BoardApiRouting")]
public sealed class BoardRoutingTests
{
    private const long ProjectId = 81;
    private const long ActorId = 42;
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [TestMethod]
    public async Task ContentLibrary_RequiresAuthentication()
    {
        await using Host host = await StartAsync(signedIn: false);
        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/contents");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ContentLibrary_ReturnsRealProjectContentIncludingArchivedAndAssignees()
    {
        await using Host host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/contents");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement cards = json.RootElement.GetProperty("cards");
        Assert.AreEqual(2, cards.GetArrayLength());
        CollectionAssert.AreEqual(new long[] { 901, 900 }, cards.EnumerateArray()
            .Select(card => card.GetProperty("contentId").GetInt64()).ToArray());
        JsonElement archived = cards.EnumerateArray().Single(card => card.GetProperty("status").GetString() == "Archived");
        Assert.AreEqual("TikTok", archived.GetProperty("platforms")[0].GetString());
        JsonElement assignee = archived.GetProperty("assignees")[0];
        Assert.AreEqual(7, assignee.GetProperty("userId").GetInt64());
        Assert.AreEqual("Creator One", assignee.GetProperty("displayName").GetString());
        Assert.AreEqual(1, host.Repository.LibraryReads);
        Assert.AreEqual(0, host.Repository.BoardReads);
    }

    [TestMethod]
    public async Task ContentLibrary_CombinesAllFiltersAndSortsFromProjectData()
    {
        await using Host host = await StartAsync();
        const string query = "title=active&platform=youtube&status=Script&creatorId=8&priority=Medium" +
            "&hasDeadline=true&deadlineFrom=2026-11-01&deadlineTo=2026-11-30&tag=weekly&sortBy=title&sortDescending=false";

        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/contents?{query}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement cards = json.RootElement.GetProperty("cards");
        Assert.AreEqual(1, cards.GetArrayLength());
        Assert.AreEqual(900, cards[0].GetProperty("contentId").GetInt64());
        Assert.AreEqual(1, host.Repository.LibraryReads);
    }

    [TestMethod]
    public async Task ContentLibrary_RejectsInvalidSortWithoutReadingContent()
    {
        await using Host host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/contents?sortBy=sql");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(0, host.Repository.LibraryReads);
    }

    [TestMethod]
    public async Task ContentLibrary_RejectsReversedDeadlineRange()
    {
        await using Host host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/contents?deadlineFrom=2026-11-30&deadlineTo=2026-11-01");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(0, host.Repository.LibraryReads);
    }

    [TestMethod]
    public async Task ContentLibrary_NonMemberIsForbiddenAndContentIsNotRead()
    {
        await using Host host = await StartAsync(member: false);
        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/contents");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(0, host.Repository.LibraryReads);
    }

    [TestMethod]
    public async Task BoardRoute_ContinuesToExcludeArchivedContent()
    {
        await using Host host = await StartAsync();
        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/board");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(1, json.RootElement.GetProperty("cards").GetArrayLength());
        Assert.AreEqual(1, host.Repository.BoardReads);
        Assert.AreEqual(0, host.Repository.LibraryReads);
    }

    private static async Task<Host> StartAsync(bool signedIn = true, bool member = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        var repository = new BoardRepositoryFake();
        var tasks = new MemberRolesFake();
        if (member) tasks.Roles[(ProjectId, ActorId)] = ProjectRole.Owner;
        tasks.BoardAssignees.Add(new BoardAssignee(901, 7, "Creator One", AssignmentStatus.InProgress, 45, null));
        tasks.BoardAssignees.Add(new BoardAssignee(900, 8, "Creator Two", AssignmentStatus.InProgress, 60, new DateOnly(2026, 11, 15)));
        builder.Services.AddSingleton<IBoardCardRepository>(repository);
        builder.Services.AddSingleton<ITaskRepository>(tasks);
        builder.Services.AddScoped(_ => new CurrentAuthenticatedUser
        {
            User = new User { UserId = ActorId, Email = "actor@example.test", DisplayName = "Actor", PasswordHash = "unused" },
        });
        builder.Services.AddScoped<BoardService>();
        builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, ProjectRoutingTests.FixtureAuth>("fixture", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapBoardEndpoints();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();
        if (signedIn) client.DefaultRequestHeaders.Authorization = new("fixture", "actor");
        return new Host(app, client, repository);
    }

    private sealed record Host(WebApplication App, HttpClient Client, BoardRepositoryFake Repository) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
        }
    }

    private sealed class BoardRepositoryFake : IBoardCardRepository
    {
        private static readonly BoardCard Active = new(900, ProjectId, "CNT-900", "Active script", "description", "Script", "Medium", new DateOnly(2026, 11, 15), null, ["YouTube"], ["weekly"], false);
        private static readonly BoardCard Archived = new(901, ProjectId, "CNT-901", "Archived video", "description", "Archived", "Low", null, null, ["TikTok"], [], false);

        public int BoardReads { get; private set; }
        public int LibraryReads { get; private set; }

        public Task<IReadOnlyList<BoardCard>> GetCardsAsync(long projectId, CancellationToken cancellationToken = default)
        {
            BoardReads++;
            return Task.FromResult<IReadOnlyList<BoardCard>>([Active]);
        }

        public Task<IReadOnlyList<BoardCard>> GetProjectContentsAsync(long projectId, CancellationToken cancellationToken = default)
        {
            LibraryReads++;
            return Task.FromResult<IReadOnlyList<BoardCard>>([Active, Archived]);
        }
    }
}
