using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Tasks;
using CreatorFlow.Api.Tests.Api;
using CreatorFlow.Api.Tests.Notifications;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Repositories.Notifications;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Services.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Tasks;

[TestClass]
[TestCategory("MyTasksApiRouting")]
public sealed class TaskRoutingTests
{
    private const long ProjectId = 81;
    private const long ActorId = 42;
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [TestMethod]
    public async Task MyTasksRoute_RequiresAuthentication()
    {
        await using Host host = await StartAsync(signedIn: false);

        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/my-tasks");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, host.Repository.MyTasksReads);
    }

    [TestMethod]
    public async Task MyTasksRoute_ReturnsTasksForRouteProjectAndAuthenticatedUser()
    {
        await using Host host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/my-tasks?userId=999");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement tasks = json.RootElement.GetProperty("tasks");
        Assert.AreEqual(1, tasks.GetArrayLength());
        Assert.AreEqual(ActorId, tasks[0].GetProperty("assigneeUserId").GetInt64());
        Assert.AreEqual(ProjectId, tasks[0].GetProperty("projectId").GetInt64());
        Assert.AreEqual("CNT-0901", tasks[0].GetProperty("contentCode").GetString());
        Assert.AreEqual((ProjectId, ActorId), host.Repository.LastMyTasksQuery);
    }

    [TestMethod]
    public async Task MyTasksRoute_NonMemberGets403WithoutReadingAssignments()
    {
        await using Host host = await StartAsync(member: false);

        using HttpResponseMessage response = await host.Client.GetAsync($"/api/projects/{ProjectId}/my-tasks");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(0, host.Repository.MyTasksReads);
    }

    [TestMethod]
    public async Task TaskEndpoints_AreMappedAtExpectedHttpRoutes()
    {
        await using Host host = await StartAsync(signedIn: false);
        var routes = new (HttpMethod Method, string Path, object? Body)[]
        {
            (HttpMethod.Get, $"/api/projects/{ProjectId}/my-tasks", null),
            (HttpMethod.Get, $"/api/projects/{ProjectId}/my-role", null),
            (HttpMethod.Get, $"/api/projects/{ProjectId}/assignable-members", null),
            (HttpMethod.Get, $"/api/projects/{ProjectId}/board/assignees", null),
            (HttpMethod.Get, "/api/contents/901/assignments", null),
            (HttpMethod.Put, "/api/contents/901/assignments", new { assignments = new[] { new { userId = 42, deadline = (string?)null } } }),
            (HttpMethod.Put, "/api/contents/901/progress", new { progressPercent = 50 }),
            (HttpMethod.Put, "/api/contents/901/assignments/42/deadline", new { deadline = "2099-01-01" }),
        };

        foreach ((HttpMethod method, string path, object? body) in routes)
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = body is null ? null : JsonContent.Create(body),
            };
            using HttpResponseMessage response = await host.Client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, $"{method} {path} should be a mapped authenticated endpoint.");
        }
    }

    private static async Task<Host> StartAsync(bool signedIn = true, bool member = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var repository = new TaskRepositoryFake();
        if (member) repository.Roles[(ProjectId, ActorId)] = ProjectRole.Creator;
        var notifications = new FakeNotificationRepository();
        var users = new NotificationUserRepository();
        var emailSender = new TestEmailSender();

        builder.Services.AddSingleton<ITaskRepository>(repository);
        builder.Services.AddSingleton<INotificationRepository>(notifications);
        builder.Services.AddSingleton<IUserRepository>(users);
        builder.Services.AddSingleton<IEmailSender>(emailSender);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped(_ => new CurrentAuthenticatedUser
        {
            User = new User { UserId = ActorId, Email = "actor@example.test", DisplayName = "Actor", PasswordHash = "unused" },
        });
        builder.Services.AddScoped<TaskService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, ProjectRoutingTests.FixtureAuth>("fixture", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTaskEndpoints();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();
        if (signedIn) client.DefaultRequestHeaders.Authorization = new("fixture", "actor");
        return new Host(app, client, repository);
    }

    private sealed record Host(WebApplication App, HttpClient Client, TaskRepositoryFake Repository) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
        }
    }

    private sealed class TaskRepositoryFake : ITaskRepository
    {
        private static readonly TaskAssignment Assignment = new(
            701, 901, ProjectId, ActorId, "CNT-0901", "Sample task", "Script", "High",
            AssignmentStatus.InProgress, 35, new DateOnly(2026, 11, 1), "Owner", ["YouTube"], 2, 1);

        public Dictionary<(long Project, long User), ProjectRole> Roles { get; } = new();
        public int MyTasksReads { get; private set; }
        public (long Project, long User)? LastMyTasksQuery { get; private set; }

        public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out ProjectRole role) ? role : null);

        public Task<IReadOnlyList<TaskAssignment>> GetMyTasksAsync(long projectId, long userId, CancellationToken cancellationToken = default)
        {
            MyTasksReads++;
            LastMyTasksQuery = (projectId, userId);
            return Task.FromResult<IReadOnlyList<TaskAssignment>>([Assignment]);
        }

        public Task<IReadOnlyList<ProjectMember>> GetMembersAsync(long projectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ContentInfo?> GetContentAsync(long contentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TaskAssignment?> GetAssignmentAsync(long contentId, long userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskAssignment>> GetAssignmentsAsync(long contentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<BoardAssignee>> GetBoardAssigneesAsync(long projectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateProgressAsync(long assignmentId, int percent, AssignmentStatus status, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAssignmentDeadlineAsync(long assignmentId, DateOnly? deadline, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ApplyAssignmentsAsync(long contentId, long actorUserId, IReadOnlyList<long> cancelAssignmentIds,
            IReadOnlyList<DeadlineChange> deadlineChanges, IReadOnlyList<NewAssignment> additions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
