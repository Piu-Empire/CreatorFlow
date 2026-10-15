using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.ContentDetail;
using CreatorFlow.Api.Repositories.Ideas;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.ContentDetail;
using CreatorFlow.Api.Services.Ideas;
using CreatorFlow.Api.Services.Invitations;
using CreatorFlow.Api.Tests.ContentDetail;
using CreatorFlow.Api.Tests.Ideas;
using CreatorFlow.Api.Tests.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Api;

/// <summary>
/// Route thật của Idea Bank (SCRUM-30/31), Content Detail/Script/Platform (SCRUM-32/33/34) và lời mời (SCRUM-26/27):
/// mọi route phải đòi đăng nhập, đúng verb/đường dẫn mà ApiClient gọi, từ chối thuộc tính lạ trong body,
/// và người thao tác chỉ lấy từ đăng nhập (không từ query/body).
/// </summary>
[TestClass]
[TestCategory("FeatureApiRouting")]
public sealed class FeatureRoutingTests
{
    private const long ProjectId = 1;
    private const long ActorId = 3; // FixtureAuth luôn đăng nhập là user 42; service dưới đây dùng CurrentAuthenticatedUser cố định = ActorId

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private sealed record Host(WebApplication App, HttpClient Client, IdeaRepositoryFake Ideas, ContentDetailRepositoryFake Contents,
        MemberRolesFake Roles, InvitationFake Invites, MyInvitationFake MyInvites) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() { Client.Dispose(); await App.DisposeAsync(); }
    }

    private static async Task<Host> StartAsync(bool signedIn = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var ideas = new IdeaRepositoryFake();
        var contents = new ContentDetailRepositoryFake();
        var roles = new MemberRolesFake();
        var invites = new InvitationFake();
        var myInvites = new MyInvitationFake();
        roles.Roles[(ProjectId, 1)] = ProjectRole.Owner;
        roles.Roles[(ProjectId, ActorId)] = ProjectRole.Creator;
        invites.Roles[(ProjectId, ActorId)] = ProjectRole.Owner; // trong test lời mời, người đăng nhập là Owner

        builder.Services.AddSingleton<IIdeaRepository>(ideas);
        builder.Services.AddSingleton<IContentDetailRepository>(contents);
        builder.Services.AddSingleton<ITaskRepository>(roles);
        builder.Services.AddSingleton<IInvitationRepository>(invites);
        builder.Services.AddSingleton<IMyInvitationRepository>(myInvites);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped(_ => new CurrentAuthenticatedUser
        {
            User = new User { UserId = ActorId, Email = "actor@x.vn", DisplayName = "Actor", PasswordHash = "x" },
        });
        builder.Services.AddScoped<IdeaService>();
        builder.Services.AddScoped<ContentDetailService>();
        builder.Services.AddScoped<InvitationService>();
        builder.Services.AddScoped<MyInvitationService>();
        builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, ProjectRoutingTests.FixtureAuth>("fixture", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapIdeaEndpoints();
        app.MapContentDetailEndpoints();
        app.MapInvitationEndpoints();
        app.MapMyInvitationEndpoints();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        if (signedIn) client.DefaultRequestHeaders.Authorization = new("fixture", "actor");
        return new Host(app, client, ideas, contents, roles, invites, myInvites);
    }

    private static readonly (HttpMethod Method, string Path, object? Body)[] AllRoutes =
    [
        (HttpMethod.Get, "/api/projects/1/ideas", null),
        (HttpMethod.Get, "/api/projects/1/idea-tags", null),
        (HttpMethod.Post, "/api/projects/1/ideas", new { title = "t", status = "Draft" }),
        (HttpMethod.Put, "/api/ideas/1", new { title = "t", status = "Draft" }),
        (HttpMethod.Post, "/api/ideas/1/convert", null),
        (HttpMethod.Delete, "/api/ideas/1", null),
        (HttpMethod.Get, "/api/platforms", null),
        (HttpMethod.Post, "/api/projects/1/contents", new { title = "t", priority = "Low" }),
        (HttpMethod.Get, "/api/contents/10", null),
        (HttpMethod.Put, "/api/contents/10", new { title = "t", priority = "Low" }),
        (HttpMethod.Get, "/api/contents/10/script", null),
        (HttpMethod.Put, "/api/contents/10/script", new { text = "x", expectedVersion = "v" }),
        (HttpMethod.Get, "/api/contents/10/activity", null),
        (HttpMethod.Post, "/api/projects/1/invitations", new { email = "a@x.vn", role = "Creator" }),
        (HttpMethod.Get, "/api/projects/1/invitations", null),
        (HttpMethod.Get, "/api/invitations/mine", null),
        (HttpMethod.Post, "/api/invitations/1/accept", null),
        (HttpMethod.Post, "/api/invitations/1/reject", null),
    ];

    [TestMethod]
    public async Task EveryRoute_WithoutLogin_Returns401()
    {
        await using Host host = await StartAsync(signedIn: false);

        foreach (var (method, path, body) in AllRoutes)
        {
            using var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await host.Client.SendAsync(request)).StatusCode, $"{method} {path}");
        }
    }

    [TestMethod]
    public async Task Ideas_FullLifecycleOverHttp_CreateListUpdateConvertDelete()
    {
        await using Host host = await StartAsync();

        var created = await host.Client.PostAsJsonAsync("/api/projects/1/ideas", new { title = " Vlog ", description = "d", note = "n", status = "Draft", tags = new[] { "#travel" } });
        Assert.IsTrue(created.IsSuccessStatusCode);
        var idea = (await created.Content.ReadFromJsonAsync<JsonElement>(Web));
        long id = idea.GetProperty("ideaId").GetInt64();
        Assert.AreEqual("Vlog", idea.GetProperty("title").GetString());
        Assert.AreEqual(ActorId, idea.GetProperty("createdByUserId").GetInt64());

        var list = await host.Client.GetFromJsonAsync<JsonElement>("/api/projects/1/ideas?status=draft&tag=TRAVEL&search=vlo", Web);
        Assert.AreEqual(1, list.GetProperty("ideas").GetArrayLength());
        Assert.AreEqual("Creator", list.GetProperty("myRole").GetString());

        var tags = await host.Client.GetFromJsonAsync<JsonElement>("/api/projects/1/idea-tags", Web);
        Assert.AreEqual("travel", tags.GetProperty("tags")[0].GetString());

        var updated = await host.Client.PutAsJsonAsync($"/api/ideas/{id}", new { title = "Vlog 2", status = "Backlog" });
        Assert.AreEqual(HttpStatusCode.OK, updated.StatusCode);

        var converted = await host.Client.PostAsync($"/api/ideas/{id}/convert", null);
        Assert.AreEqual(HttpStatusCode.OK, converted.StatusCode);
        Assert.AreEqual(id, (await converted.Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("ideaId").GetInt64());

        Assert.AreEqual(HttpStatusCode.BadRequest, (await host.Client.DeleteAsync($"/api/ideas/{id}")).StatusCode); // Converted không xóa được
        Assert.AreEqual(HttpStatusCode.NotFound, (await host.Client.DeleteAsync("/api/ideas/9999")).StatusCode);
    }

    [TestMethod]
    public async Task Ideas_BadInput_IsRejected_WithProblemDetailsCodeAndField()
    {
        await using Host host = await StartAsync();

        var badStatus = await host.Client.GetAsync("/api/projects/1/ideas?status=1");
        var body = await badStatus.Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.AreEqual(HttpStatusCode.BadRequest, badStatus.StatusCode);
        Assert.AreEqual("validation", body.GetProperty("code").GetString());
        Assert.IsTrue(body.GetProperty("errors").TryGetProperty("status", out _));

        Assert.AreEqual(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/projects/1/ideas", new { title = "t", status = "Draft", createdBy = 1 })).StatusCode, "thuộc tính lạ phải bị từ chối");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/projects/1/ideas", new { title = "", status = "Draft" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/projects/abc/ideas")).StatusCode, "projectId phải là số");
        Assert.AreEqual(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/projects/2/ideas")).StatusCode);
        Assert.AreEqual(0, host.Ideas.Ideas.Count);
    }

    [TestMethod]
    public async Task Ideas_ActorComesFromLogin_NotFromQueryOrBody()
    {
        await using Host host = await StartAsync();
        host.Ideas.Seed(ProjectId, 1, "Của Owner"); // Actor là Creator, không được sửa

        var hijack = await host.Client.PutAsJsonAsync("/api/ideas/1?userId=1", new { title = "Chiếm", status = "Draft" });

        Assert.AreEqual(HttpStatusCode.Forbidden, hijack.StatusCode);
        Assert.AreEqual("Của Owner", host.Ideas.Get(1).Title);
    }

    [TestMethod]
    public async Task ContentDetail_PlatformsCreateGetUpdate_OverHttp()
    {
        await using Host host = await StartAsync();

        var platforms = await host.Client.GetFromJsonAsync<JsonElement>("/api/platforms", Web);
        Assert.AreEqual(4, platforms.GetProperty("platforms").GetArrayLength());

        var created = await host.Client.PostAsJsonAsync("/api/projects/1/contents",
            new { title = "Video", priority = "High", status = "Script", deadline = "2099-01-01", plannedPublishAt = "2099-01-02", platforms = new[] { "youtube" } });
        Assert.IsTrue(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var detail = await created.Content.ReadFromJsonAsync<JsonElement>(Web);
        long id = detail.GetProperty("contentId").GetInt64();
        Assert.AreEqual("Script", detail.GetProperty("status").GetString());
        Assert.AreEqual("2099-01-01", detail.GetProperty("deadline").GetString());
        Assert.AreEqual("YouTube", detail.GetProperty("platforms")[0].GetString());

        var got = await host.Client.GetFromJsonAsync<JsonElement>($"/api/contents/{id}", Web);
        Assert.AreEqual($"CNT-{id:000}", got.GetProperty("code").GetString());

        var updated = await host.Client.PutAsJsonAsync($"/api/contents/{id}",
            new { title = "Video 2", contentType = "SHORT_VIDEO", priority = "Low", deadline = "2099-01-01", platforms = new[] { "TikTok", "YouTube" } });
        Assert.IsTrue(updated.IsSuccessStatusCode, await updated.Content.ReadAsStringAsync());
        Assert.AreEqual("Short video", (await updated.Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("contentType").GetString());

        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await host.Client.PutAsJsonAsync($"/api/contents/{id}", new { title = "x", priority = "Low", status = "Published" })).StatusCode,
            "Status không được đổi qua PUT /contents");
    }

    [TestMethod]
    public async Task ContentDetail_ScriptSaveAndConflict_OverHttp()
    {
        await using Host host = await StartAsync();
        host.Contents.Add(10, ProjectId, ActorId);

        var opened = await host.Client.GetFromJsonAsync<JsonElement>("/api/contents/10/script", Web);
        string version = opened.GetProperty("version").GetString()!;
        Assert.IsTrue(opened.GetProperty("canEdit").GetBoolean());

        var saved = await host.Client.PutAsJsonAsync("/api/contents/10/script", new { text = "Hook\r\nBody", expectedVersion = version });
        Assert.AreEqual(HttpStatusCode.OK, saved.StatusCode);
        Assert.AreEqual("Hook\nBody", (await saved.Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("text").GetString());

        var stale = await host.Client.PutAsJsonAsync("/api/contents/10/script", new { text = "Của tôi", expectedVersion = version });
        var staleBody = await stale.Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.AreEqual(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.AreEqual("script_conflict", staleBody.GetProperty("code").GetString());

        Assert.IsFalse((await host.Client.PutAsJsonAsync("/api/contents/10/script", new { text = "x" })).IsSuccessStatusCode, "thiếu expectedVersion phải bị từ chối");
        Assert.AreEqual("Hook\nBody", host.Contents.Rows[10].Script, "và không được ghi đè script");
        Assert.AreEqual(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/contents/999/script")).StatusCode);
        var activity = await host.Client.GetFromJsonAsync<JsonElement>("/api/contents/10/activity", Web);
        Assert.AreEqual(10, activity.GetProperty("contentId").GetInt64());
    }

    [TestMethod]
    public async Task Invitations_InviteAndList_OverHttp_OnlyOwner()
    {
        await using Host host = await StartAsync();

        var invited = await host.Client.PostAsJsonAsync("/api/projects/1/invitations", new { email = "New@X.vn", role = "creator" });
        var body = await invited.Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.AreEqual(HttpStatusCode.Created, invited.StatusCode);
        Assert.AreEqual("Pending", body.GetProperty("status").GetString());
        Assert.AreEqual("Creator", body.GetProperty("role").GetString());
        Assert.IsFalse(body.TryGetProperty("token", out _), "token không được lộ ra API");

        Assert.AreEqual(HttpStatusCode.Conflict, (await host.Client.PostAsJsonAsync("/api/projects/1/invitations", new { email = "new@x.vn", role = "Creator" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/projects/1/invitations", new { email = "b@x.vn", role = "Owner" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/projects/1/invitations", new { email = "b@x.vn", role = "Creator", invitedBy = 9 })).StatusCode);

        var list = await host.Client.GetFromJsonAsync<JsonElement>("/api/projects/1/invitations", Web);
        Assert.AreEqual(1, list.GetProperty("invitations").GetArrayLength());

        host.Invites.Roles[(ProjectId, ActorId)] = ProjectRole.Creator;
        Assert.AreEqual(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/projects/1/invitations")).StatusCode);
    }

    [TestMethod]
    public async Task MyInvitations_MineAcceptReject_OverHttp()
    {
        await using Host host = await StartAsync();
        host.MyInvites.Seed(10, "actor@x.vn");
        host.MyInvites.Seed(11, "actor@x.vn");
        host.MyInvites.Seed(12, "other@x.vn");

        var mine = await host.Client.GetFromJsonAsync<JsonElement>("/api/invitations/mine", Web);
        Assert.AreEqual(2, mine.GetProperty("invitations").GetArrayLength());

        var accepted = await host.Client.PostAsync("/api/invitations/10/accept", null);
        Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
        Assert.AreEqual("Accepted", (await accepted.Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("status").GetString());

        var again = await host.Client.PostAsync("/api/invitations/10/accept", null);
        Assert.AreEqual(HttpStatusCode.Conflict, again.StatusCode);
        Assert.AreEqual("invitation_not_pending", (await again.Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("code").GetString());

        Assert.AreEqual("Rejected", (await (await host.Client.PostAsync("/api/invitations/11/reject", null)).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("status").GetString());
        Assert.AreEqual(HttpStatusCode.NotFound, (await host.Client.PostAsync("/api/invitations/12/accept", null)).StatusCode, "lời mời của người khác");
    }

    // ================================================================== test double

    internal sealed class InvitationFake : IInvitationRepository
    {
        public Dictionary<(long Project, long User), ProjectRole> Roles { get; } = new();
        private readonly List<ProjectInvitation> _rows = new();

        public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out ProjectRole r) ? r : null);

        public Task<bool> IsActiveMemberByEmailAsync(long projectId, string email, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<ProjectInvitation?> CreatePendingAsync(long projectId, string email, ProjectRole role, long invitedBy, string token,
            DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
        {
            if (_rows.Any(i => i.ProjectId == projectId && i.Status == InvitationStatus.Pending && i.InvitedEmail.Equals(email, StringComparison.OrdinalIgnoreCase)))
                return Task.FromResult<ProjectInvitation?>(null);
            var row = new ProjectInvitation(_rows.Count + 1, projectId, email, role, InvitationStatus.Pending, "Owner", nowUtc, expiresAtUtc, null);
            _rows.Add(row);
            return Task.FromResult<ProjectInvitation?>(row);
        }

        public Task<IReadOnlyList<ProjectInvitation>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProjectInvitation>>(_rows.Where(i => i.ProjectId == projectId).ToList());
    }

    internal sealed class MyInvitationFake : IMyInvitationRepository
    {
        private readonly Dictionary<long, string> _emails = new();
        private readonly List<MyInvitation> _rows = new();

        public void Seed(long id, string email)
        {
            _emails[id] = email;
            _rows.Add(new MyInvitation(id, ProjectId, "Project 1", ProjectRole.Creator, InvitationStatus.Pending, "Owner",
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(6), null));
        }

        public Task<IReadOnlyList<MyInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MyInvitation>>(_rows.Where(i => _emails[i.InvitationId].Equals(email, StringComparison.OrdinalIgnoreCase)).ToList());

        public Task<InvitationDecisionResult> DecideAsync(long invitationId, long userId, string email, InvitationStatus decision,
            DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            int index = _rows.FindIndex(i => i.InvitationId == invitationId && _emails[invitationId].Equals(email, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return Task.FromResult(new InvitationDecisionResult(InvitationDecisionOutcome.NotFound));
            if (_rows[index].Status != InvitationStatus.Pending)
                return Task.FromResult(new InvitationDecisionResult(InvitationDecisionOutcome.NotPending, CurrentStatus: _rows[index].Status));
            _rows[index] = _rows[index] with { Status = decision, RespondedAt = nowUtc };
            return Task.FromResult(new InvitationDecisionResult(InvitationDecisionOutcome.Decided, _rows[index]));
        }
    }
}
