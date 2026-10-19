using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Repositories.Projects;
using CreatorFlow.Api.Repositories.Workflow;
using CreatorFlow.Api.Services.Projects;
using CreatorFlow.Api.Services.Workflow;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Api.Tests.Workflow;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Contracts.Workflow;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Api;

[TestClass]
[TestCategory("ProjectAuthorization")]
public sealed class ProjectAuthorizationTests
{
    [TestMethod]
    public async Task JwtSubjectDeterminesActor_ClientRoleClaimsQueryAndHeadersDoNotElevate()
    {
        await using var f = await Fixture.StartAsync("CREATOR");
        f.Client.DefaultRequestHeaders.Add("X-UserId", "55");
        f.Client.DefaultRequestHeaders.Add("project_role", "OWNER");
        using var response = await f.Client.PutAsJsonAsync(
            "/api/contents/10/review/decision?userId=55&project_role=OWNER", new ReviewDecisionRequest(true));
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual((8L, 42L), f.Workflow.RoleReads.Last());
        Assert.AreEqual(0, f.Workflow.Writes.Count);
        using var bodyOverride = await f.Client.PutAsJsonAsync("/api/contents/10/review/decision",
            new { approve = true, userId = 55, project_role = "OWNER" });
        Assert.AreEqual(HttpStatusCode.BadRequest, bodyOverride.StatusCode);
        Assert.AreEqual(0, f.Workflow.Writes.Count);
    }

    [TestMethod]
    public async Task ManagerCannotUseOwnerOperations_EvenWithTargetRoleOwner()
    {
        await using var f = await Fixture.StartAsync("MANAGER");
        using var edit = await f.Client.PutAsJsonAsync("/api/projects/8", new UpdateProjectRequest("Other", null));
        using var archive = await f.Client.PostAsync("/api/projects/8/archive", null);
        using var restore = await f.Client.PostAsync("/api/projects/8/restore", null);
        using var delete = await f.Client.DeleteAsync("/api/projects/8");
        using var role = await f.Client.PutAsJsonAsync("/api/projects/8/members/55/role", new UpdateProjectMemberRoleRequest("OWNER"));
        using var remove = await f.Client.DeleteAsync("/api/projects/8/members/55");
        foreach (var response in new[] { edit, archive, restore, delete, role, remove })
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(0, f.Projects.WriteCount);
        using var review = await f.Client.PutAsJsonAsync("/api/contents/10/review/decision", new ReviewDecisionRequest(true));
        Assert.AreEqual(HttpStatusCode.OK, review.StatusCode);
        Assert.AreEqual((10L, 42L), f.Workflow.Writes.Single());
    }

    [TestMethod]
    public async Task SameTokenUsesCurrentMembership_ForEachProjectAndAfterRemoval()
    {
        await using var f = await Fixture.StartAsync("OWNER");
        f.Workflow.Roles[(9, 42)] = ProjectRole.Creator;
        using var crossProject = await f.Client.PutAsJsonAsync("/api/contents/20/review/decision?projectId=8", new ReviewDecisionRequest(true));
        Assert.AreEqual(HttpStatusCode.Forbidden, crossProject.StatusCode);
        using var edit = await f.Client.PutAsJsonAsync("/api/projects/8", new UpdateProjectRequest("Allowed", null));
        Assert.AreEqual(HttpStatusCode.OK, edit.StatusCode);
        Assert.AreEqual(1, f.Projects.WriteCount);
        f.Projects.Project = ProjectFake.Record("MANAGER");
        using var demoted = await f.Client.PostAsync("/api/projects/8/archive", null);
        Assert.AreEqual(HttpStatusCode.Forbidden, demoted.StatusCode);
        f.Workflow.Roles.Remove((8, 42));
        using var removed = await f.Client.GetAsync("/api/contents/10/status-history");
        Assert.AreEqual(HttpStatusCode.Forbidden, removed.StatusCode);
        Assert.AreEqual(0, f.Workflow.Writes.Count);
        Assert.AreEqual(1, f.Projects.WriteCount);
    }

    [TestMethod]
    public async Task MissingInvalidAndRevokedTokensAreRejectedBeforeServices()
    {
        await using var f = await Fixture.StartAsync("OWNER");
        foreach (string? token in new[] { null, "invalid-token", f.Token })
        {
            if (token == f.Token) f.Users.User = new User
            {
                UserId = 42, Email = "actor@example.test", DisplayName = "Actor", PasswordHash = "unused",
                AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTimeOffset.UtcNow, TokenVersion = 1
            };
            f.Client.DefaultRequestHeaders.Authorization = token is null ? null : new("Bearer", token);
            using var response = await f.Client.PutAsJsonAsync("/api/contents/10/review/decision", new ReviewDecisionRequest(true));
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        Assert.AreEqual(0, f.Workflow.RoleReads.Count);
        Assert.AreEqual(0, f.Workflow.Writes.Count);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required WebApplication App { get; init; }
        public required HttpClient Client { get; init; }
        public required UserAccountFake Users { get; init; }
        public required ProjectFake Projects { get; init; }
        public required WorkflowRepositoryFake Workflow { get; init; }
        public required string Token { get; init; }

        public static async Task<Fixture> StartAsync(string role)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var config = JwtTests.Configuration();
            var user = new User { UserId = 42, Email = "actor@example.test", DisplayName = "Actor", PasswordHash = "unused",
                AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTimeOffset.UtcNow, IsSystemAdmin = true };
            var users = new UserAccountFake { User = user };
            var projects = new ProjectFake { Project = ProjectFake.Record(role) };
            var workflow = new WorkflowRepositoryFake();
            workflow.Roles[(8, 42)] = Enum.Parse<ProjectRole>(role, true);
            builder.Services.AddCreatorFlowAuthentication(config);
            builder.Services.AddSingleton<IUserRepository>(users);
            builder.Services.AddSingleton<IProjectRepository>(projects);
            builder.Services.AddSingleton<IWorkflowRepository>(workflow);
            builder.Services.AddScoped<ProjectService>();
            builder.Services.AddScoped<ProjectMembersService>();
            builder.Services.AddScoped<WorkflowService>();
            builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
            var app = builder.Build();
            app.UseAuthentication(); app.UseAuthorization();
            app.MapProjectEndpoints(); app.MapWorkflowEndpoints();
            await app.StartAsync();
            // Claim role cố tình khác membership: token hợp lệ vẫn không cấp quyền Project.
            var jwt = new JwtSecurityToken(config.Issuer, config.Audience,
                [new Claim("sub", "42"), new Claim("token_version", "0"), new Claim("project_role", "OWNER")],
                DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(config.SigningKey, SecurityAlgorithms.HmacSha256));
            string token = new JwtSecurityTokenHandler().WriteToken(jwt);
            var client = app.GetTestClient();
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            return new() { App = app, Client = client, Users = users, Projects = projects, Workflow = workflow, Token = token };
        }

        public async ValueTask DisposeAsync() { Client.Dispose(); await App.DisposeAsync(); }
    }
}
