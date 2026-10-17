using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Projects;
using CreatorFlow.Api.Services.Projects;
using CreatorFlow.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("ProjectDatabase")]
public sealed class ProjectRepositoryIntegrationTests
{
    [TestMethod]
    public async Task FailedMembershipInsert_RollsBackProject()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Sql("ALTER TABLE project_members ADD CONSTRAINT fixture_membership_failure CHECK (FALSE)");
        try { await fixture.Service.CreateAsync(new("Rollback", null), default); Assert.Fail("Insert must fail"); }
        catch (PostgresException error) { Assert.AreEqual("23514", error.SqlState); }
        Assert.AreEqual(0L, await fixture.Count("projects")); Assert.AreEqual(0L, await fixture.Count("project_members"));
    }
    [TestMethod]
    public async Task PopulatedProject_ArchiveRetainsAndHardDeleteCascades()
    {
        await using var fixture = await Fixture.CreateAsync();
        var created = await fixture.Service.CreateAsync(new(" Team ", " text "), default); long a = created.Value!.ProjectId;
        long b = (await fixture.Service.CreateAsync(new("Other", null), default)).Value!.ProjectId;
        Assert.AreEqual("Team", created.Value.Name);
        Assert.AreEqual(2L, await fixture.Count("project_members"));
        // This schema is unique to the fixture; parameters refer only to its records.
        await fixture.Sql("""
            INSERT INTO ideas(idea_id,project_id,title,created_by) VALUES(101,@a,'Idea',@user);
            INSERT INTO contents(content_id,project_id,title,created_by) VALUES(201,@a,'A',@user);
            INSERT INTO contents(content_id,project_id,title,source_idea_id,created_by) VALUES(202,@b,'B',101,@user);
            INSERT INTO platforms(platform_id,code,name) VALUES(301,'FIXTURE','Fixture');
            INSERT INTO content_platforms(content_platform_id,content_id,platform_id) VALUES(401,201,301);
            INSERT INTO content_metrics(content_platform_id,views,recorded_by) VALUES(401,5,@user);
            INSERT INTO tags(tag_id,project_id,name) VALUES(501,@a,'Fixture');
            INSERT INTO content_tags(content_id,tag_id) VALUES(201,501);
            INSERT INTO content_attachments(content_id,display_name,resource_url,added_by) VALUES(201,'Fixture','https://example.test/fixture',@user);
            INSERT INTO project_invitations(project_id,invited_email,invited_by,role,token,expires_at)
            VALUES(@a,'invited@example.test',@user,'CREATOR','fixture-token',CURRENT_TIMESTAMP + INTERVAL '1 day');
            INSERT INTO content_assignments(content_id,assignee_id,assigned_by) VALUES(201,@user,@user);
            INSERT INTO content_status_history(content_id,to_status,changed_by) VALUES(201,'SCRIPT',@user);
            INSERT INTO reviews(content_id,review_no,submitted_by) VALUES(201,1,@user);
            INSERT INTO content_relations(source_content_id,derived_content_id,relation_type) VALUES(201,202,'REPURPOSED');
            INSERT INTO notifications(recipient_user_id,project_id,content_id,type,title,message) VALUES(@user,@a,201,'SYSTEM','Fixture','Fixture');
            INSERT INTO ai_requests(user_id,project_id,content_id,request_type) VALUES(@user,@b,201,'OTHER');
            """, a, b);
        Assert.AreEqual(200, (await fixture.Service.UpdateAsync(a, new("Renamed", "updated"), default)).Status);
        Assert.AreEqual(200, (await fixture.Service.ArchiveAsync(a, default)).Status);
        Assert.AreEqual(2L, await fixture.Count("contents")); Assert.AreEqual(409, (await fixture.Service.AccessAsync(a, default)).Status);
        Assert.AreEqual(200, (await fixture.Service.DeleteAsync(a, default)).Status);
        foreach (string table in new[] { "ideas", "project_invitations", "tags", "content_tags", "content_attachments", "content_platforms", "content_metrics", "content_assignments", "content_status_history", "reviews", "content_relations", "notifications" })
            Assert.AreEqual(0L, await fixture.Count(table), table);
        Assert.AreEqual(1L, await fixture.Count("contents")); Assert.AreEqual(1L, await fixture.Count("projects"));
        Assert.AreEqual(1L, await fixture.Count("project_members"));
        Assert.AreEqual(1L, await fixture.Count("users")); Assert.AreEqual(1L, await fixture.Count("platforms"));
        Assert.AreEqual(1L, await fixture.Count("ai_requests"));
        await using var connection = await fixture.Factory.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT source_idea_id IS NULL FROM contents WHERE content_id=202", connection);
        Assert.AreEqual(true, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT content_id IS NULL FROM ai_requests"; Assert.AreEqual(true, await command.ExecuteScalarAsync());
    }
    [TestMethod]
    public async Task OwnerMembership_IsLockedUntilMutationTransactionCompletes()
    {
        await using var fixture = await Fixture.CreateAsync();
        long id = (await fixture.Service.CreateAsync(new("Lock", null), default)).Value!.ProjectId;
        var repository = new ProjectRepository(fixture.Factory);
        await repository.InTransactionAsync(async (tx, token) =>
        {
            Assert.IsNotNull(await tx.LockAsync(id, fixture.UserId, token));
            await using var competing = await fixture.Factory.OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand("SET lock_timeout = '100ms'; UPDATE project_members SET is_active = FALSE WHERE project_id=@id", competing);
            command.Parameters.AddWithValue("id", id);
            try { await command.ExecuteNonQueryAsync(token); Assert.Fail("Membership lock must block revocation"); }
            catch (PostgresException error) { Assert.AreEqual("55P03", error.SqlState); }
            return true;
        }, default);
        await fixture.Sql("UPDATE project_members SET is_active=FALSE WHERE project_id=@a", id);
        Assert.AreEqual(404, (await fixture.Service.DeleteAsync(id, default)).Status);
        Assert.AreEqual(1L, await fixture.Count("projects"));
    }

    [TestMethod]
    public async Task MemberRolesAreProjectScoped_AndProjectAlwaysKeepsAnActiveOwner()
    {
        await using var fixture = await Fixture.CreateAsync();
        long firstProject = (await fixture.Service.CreateAsync(new("First", null), default)).Value!.ProjectId;
        long secondProject = (await fixture.Service.CreateAsync(new("Second", null), default)).Value!.ProjectId;
        long memberId = await fixture.AddUser("member@example.test");
        long secondOwnerId = await fixture.AddUser("second-owner@example.test");
        await fixture.InsertMember(firstProject, memberId, "CREATOR");
        await fixture.InsertMember(secondProject, memberId, "CREATOR");
        var service = new ProjectMembersService(new ProjectRepository(fixture.Factory), new CurrentAuthenticatedUser { User = new User
        { UserId = fixture.UserId, Email = "fixture@example.test", DisplayName = "Fixture", PasswordHash = "unused", AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTimeOffset.UtcNow } });

        Assert.AreEqual(200, (await service.UpdateRoleAsync(firstProject, memberId, new("MANAGER"), default)).Status);
        Assert.AreEqual("MANAGER", await fixture.Role(firstProject, memberId));
        Assert.AreEqual("CREATOR", await fixture.Role(secondProject, memberId));

        // The last Owner cannot leave the Project without ownership.
        Assert.AreEqual(409, (await service.UpdateRoleAsync(firstProject, fixture.UserId, new("CREATOR"), default)).Status);
        Assert.AreEqual(409, (await service.RemoveAsync(firstProject, fixture.UserId, default)).Status);
        Assert.AreEqual("OWNER", await fixture.Role(firstProject, fixture.UserId));
        Assert.IsTrue(await fixture.IsActive(firstProject, fixture.UserId));

        // With a second Owner, the original Owner may leave; owner_id follows the remaining active Owner.
        await fixture.InsertMember(firstProject, secondOwnerId, "OWNER");
        Assert.AreEqual(200, (await service.UpdateRoleAsync(firstProject, fixture.UserId, new("MANAGER"), default)).Status);
        Assert.AreEqual("MANAGER", await fixture.Role(firstProject, fixture.UserId));
        Assert.AreEqual("OWNER", await fixture.Role(firstProject, secondOwnerId));
        Assert.AreEqual(secondOwnerId, await fixture.ProjectOwnerId(firstProject));
        Assert.AreEqual("CREATOR", await fixture.Role(secondProject, memberId));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required NpgsqlConnectionFactory Factory { get; init; }
        public required string AdministrativeConnection { get; init; }
        public required string Schema { get; init; }
        public long UserId { get; set; }
        public ProjectService Service => new(new ProjectRepository(Factory), new CurrentAuthenticatedUser { User = new User
        { UserId = UserId, Email = "fixture@example.test", DisplayName = "Fixture", PasswordHash = "unused", AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTimeOffset.UtcNow } });
        public static async Task<Fixture> CreateAsync()
        {
            string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true" || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_SCHEMA_CHANGES") != "true")
                Assert.Inconclusive("Requires approved disposable DB, ALLOW_WRITES=true and ALLOW_SCHEMA_CHANGES=true. Never uses application config.");
            string schema = "project_fixture_" + Guid.NewGuid().ToString("N");
            await using var admin = new NpgsqlConnection(connectionString); await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE SCHEMA {schema}", admin); await create.ExecuteNonQueryAsync();
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema };
            var fixture = new Fixture { Factory = new(builder.ConnectionString), AdministrativeConnection = connectionString!, Schema = schema };
            try
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "database", "01_schema.sql"))) directory = directory.Parent;
                if (directory is null) throw new InvalidOperationException("Fixture schema source not found.");
                await fixture.Sql(await File.ReadAllTextAsync(Path.Combine(directory.FullName, "database", "01_schema.sql")));
                await using var connection = await fixture.Factory.OpenConnectionAsync();
                await using var user = new NpgsqlCommand("INSERT INTO users(email,password_hash,display_name) VALUES('fixture@example.test','unused','Fixture') RETURNING user_id", connection);
                fixture.UserId = (long)(await user.ExecuteScalarAsync())!;
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }
        public async Task Sql(string sql, long a = 0, long b = 0)
        {
            await using var connection = await Factory.OpenConnectionAsync(); await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("a", a); command.Parameters.AddWithValue("b", b); command.Parameters.AddWithValue("user", UserId);
            await command.ExecuteNonQueryAsync();
        }
        public async Task<long> AddUser(string email)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand("INSERT INTO users(email,password_hash,display_name) VALUES(@email,'unused',@name) RETURNING user_id", connection);
            command.Parameters.AddWithValue("email", email);
            command.Parameters.AddWithValue("name", email);
            return (long)(await command.ExecuteScalarAsync())!;
        }
        public async Task InsertMember(long projectId, long userId, string role)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand("INSERT INTO project_members(project_id,user_id,role) VALUES(@project,@user,@role::project_role)", connection);
            command.Parameters.AddWithValue("project", projectId);
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("role", role);
            await command.ExecuteNonQueryAsync();
        }
        public async Task<string> Role(long projectId, long userId)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT role::text FROM project_members WHERE project_id=@project AND user_id=@user", connection);
            command.Parameters.AddWithValue("project", projectId); command.Parameters.AddWithValue("user", userId);
            return (string)(await command.ExecuteScalarAsync())!;
        }
        public async Task<bool> IsActive(long projectId, long userId)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT is_active FROM project_members WHERE project_id=@project AND user_id=@user", connection);
            command.Parameters.AddWithValue("project", projectId); command.Parameters.AddWithValue("user", userId);
            return (bool)(await command.ExecuteScalarAsync())!;
        }
        public async Task<long> ProjectOwnerId(long projectId)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT owner_id FROM projects WHERE project_id=@project", connection);
            command.Parameters.AddWithValue("project", projectId);
            return (long)(await command.ExecuteScalarAsync())!;
        }
        public async Task<long> Count(string fixtureTable)
        {
            // Only fixed table names supplied by these tests, never application/user input.
            await using var connection = await Factory.OpenConnectionAsync(); await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {fixtureTable}", connection);
            return (long)(await command.ExecuteScalarAsync())!;
        }
        public async ValueTask DisposeAsync()
        {
            await using var admin = new NpgsqlConnection(AdministrativeConnection); await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {Schema} CASCADE", admin); await command.ExecuteNonQueryAsync();
        }
    }
}
