using CreatorFlow.Api.Repositories.Search;
using CreatorFlow.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("GlobalSearchDatabase")]
public sealed class GlobalSearchRepositoryIntegrationTests
{
    [TestMethod]
    public async Task SearchQuery_ExecutesAgainstConfiguredDatabaseWithoutWriting()
    {
        string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set CREATORFLOW_TEST_CONNECTION_STRING to execute the read-only global search SQL check.");

        var repository = new GlobalSearchRepository(new NpgsqlConnectionFactory(connectionString!));
        IReadOnlyList<CreatorFlow.Api.Models.Search.GlobalSearchRecord> results =
            await repository.SearchAsync(long.MaxValue, "bán người sang cam", 1);

        Assert.AreEqual(0, results.Count, "The deliberately unmatched user must not see project search results.");
    }

    [TestMethod]
    public async Task Search_FindsAnExistingTitleAfterOneCharacterIsOmitted()
    {
        string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set CREATORFLOW_TEST_CONNECTION_STRING to run the read-only fuzzy search check.");

        var factory = new NpgsqlConnectionFactory(connectionString!);
        long userId;
        long contentId;
        string title;
        await using (NpgsqlConnection connection = await factory.OpenConnectionAsync())
        await using (var command = new NpgsqlCommand("""
            SELECT pm.user_id, c.content_id, c.title
            FROM contents c
            JOIN projects p ON p.project_id = c.project_id AND p.status = 'ACTIVE'
            JOIN project_members pm ON pm.project_id = c.project_id AND pm.is_active
            WHERE char_length(c.title) >= 6
            ORDER BY c.updated_at DESC, c.content_id
            LIMIT 1
            """, connection))
        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync())
                Assert.Inconclusive("The configured test database has no active project content to use for a read-only fuzzy check.");
            userId = reader.GetInt64(0);
            contentId = reader.GetInt64(1);
            title = reader.GetString(2);
        }

        string typo = OmitOneCharacter(title);
        IReadOnlyList<CreatorFlow.Api.Models.Search.GlobalSearchRecord> results =
            await new GlobalSearchRepository(factory).SearchAsync(userId, typo, 50);

        Assert.IsTrue(results.Any(item => item.Type == "Content" && item.EntityId == contentId),
            "Search should retain an existing authorized Content candidate after one title character is omitted.");
    }

    [TestMethod]
    public async Task Search_FindsScreenshotTitleWithExactAccentlessAndMisspelledQueries_WhenPresent()
    {
        string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set CREATORFLOW_TEST_CONNECTION_STRING to run the read-only screenshot regression check.");

        var factory = new NpgsqlConnectionFactory(connectionString!);
        long userId;
        long contentId;
        await using (NpgsqlConnection connection = await factory.OpenConnectionAsync())
        await using (var command = new NpgsqlCommand("""
            SELECT pm.user_id, c.content_id
            FROM contents c
            JOIN projects p ON p.project_id = c.project_id AND p.status = 'ACTIVE'
            JOIN project_members pm ON pm.project_id = c.project_id AND pm.is_active
            WHERE LOWER(c.title) = LOWER(@title)
            ORDER BY c.content_id
            LIMIT 1
            """, connection))
        {
            command.Parameters.AddWithValue("title", "bán người sang cam");
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                Assert.Inconclusive("The configured test database does not contain the screenshot's sample title.");
            userId = reader.GetInt64(0);
            contentId = reader.GetInt64(1);
        }

        var repository = new GlobalSearchRepository(factory);
        foreach (string query in new[] { "bán người sang cam", "ban nguoi sang cam", "ban nguo sang cam" })
        {
            IReadOnlyList<CreatorFlow.Api.Models.Search.GlobalSearchRecord> results =
                await repository.SearchAsync(userId, query, 30);
            Assert.IsTrue(results.Any(item => item.Type == "Content" && item.EntityId == contentId),
                $"The screenshot's Content should match '{query}'.");
        }
    }

    [TestMethod]
    public async Task Search_FindsAccentedAccentlessAndMisspelledTitle_WithinActiveMemberships()
    {
        await using var fixture = await Fixture.CreateAsync();
        long visibleContentId = await fixture.InsertContent(fixture.ProjectId, fixture.OwnerId, "bán người sang cam");
        long hiddenContentId = await fixture.InsertContent(fixture.OtherProjectId, fixture.OtherUserId, "bán người sang cam");
        var repository = new GlobalSearchRepository(fixture.Factory);

        foreach (string query in new[] { "bán người sang cam", "ban nguoi sang cam", "ban nguo sang cam", "ban nguio sang cam" })
        {
            IReadOnlyList<CreatorFlow.Api.Models.Search.GlobalSearchRecord> results =
                await repository.SearchAsync(fixture.OwnerId, query, 30);

            Assert.IsTrue(results.Any(item => item.Type == "Content" && item.EntityId == visibleContentId),
                $"Expected the visible content to match '{query}'.");
            Assert.IsFalse(results.Any(item => item.EntityId == hiddenContentId),
                "Search must not return content in a project where the caller is not a member.");
        }

        IReadOnlyList<CreatorFlow.Api.Models.Search.GlobalSearchRecord> otherUserResults =
            await repository.SearchAsync(fixture.OtherUserId, "bán người sang cam", 30);
        Assert.IsTrue(otherUserResults.Any(item => item.Type == "Content" && item.EntityId == hiddenContentId));
        Assert.IsFalse(otherUserResults.Any(item => item.EntityId == visibleContentId));
    }

    private static string OmitOneCharacter(string title)
    {
        string[] words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string word = words.FirstOrDefault(candidate => candidate.Length >= 5 && candidate.All(char.IsLetterOrDigit))
            ?? throw new AssertInconclusiveException("The selected title has no word long enough for a deletion check.");
        int position = title.IndexOf(word, StringComparison.Ordinal) + word.Length / 2;
        return title.Remove(position, 1);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string suffix = Guid.NewGuid().ToString("N");
        public NpgsqlConnectionFactory Factory { get; }
        public long OwnerId { get; private set; }
        public long OtherUserId { get; private set; }
        public long ProjectId { get; private set; }
        public long OtherProjectId { get; private set; }

        private Fixture(string connectionString) => Factory = new NpgsqlConnectionFactory(connectionString);

        public static async Task<Fixture> CreateAsync()
        {
            string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true")
                Assert.Inconclusive("Global search database tests require CREATORFLOW_TEST_CONNECTION_STRING and CREATORFLOW_TEST_ALLOW_WRITES=true.");

            var fixture = new Fixture(connectionString!);
            try
            {
                fixture.OwnerId = await fixture.InsertUser("owner");
                fixture.OtherUserId = await fixture.InsertUser("other");
                fixture.ProjectId = await fixture.InsertProject(fixture.OwnerId, "visible");
                fixture.OtherProjectId = await fixture.InsertProject(fixture.OtherUserId, "other");
                await fixture.AddOwnerMembership(fixture.ProjectId, fixture.OwnerId);
                await fixture.AddOwnerMembership(fixture.OtherProjectId, fixture.OtherUserId);
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public Task<long> InsertContent(long projectId, long creatorId, string title) => Scalar<long>("""
            INSERT INTO contents(project_id, title, created_by)
            VALUES(@project, @title, @creator)
            RETURNING content_id
            """, ("project", projectId), ("title", title), ("creator", creatorId));

        private Task<long> InsertUser(string role) => Scalar<long>("""
            INSERT INTO users(email, password_hash, display_name)
            VALUES(@email, 'unused', @name)
            RETURNING user_id
            """, ("email", $"search-{role}-{suffix}@example.test"), ("name", $"Search {role} {suffix}"));

        private Task<long> InsertProject(long ownerId, string role) => Scalar<long>("""
            INSERT INTO projects(project_name, owner_id)
            VALUES(@name, @owner)
            RETURNING project_id
            """, ("name", $"Global search {role} {suffix}"), ("owner", ownerId));

        private Task AddOwnerMembership(long projectId, long userId) => Execute("""
            INSERT INTO project_members(project_id, user_id, role)
            VALUES(@project, @user, 'OWNER')
            """, ("project", projectId), ("user", userId));

        private async Task<T> Scalar<T>(string sql, params (string Name, object Value)[] parameters)
        {
            await using NpgsqlConnection connection = await Factory.OpenConnectionAsync();
            await using var command = Build(connection, sql, parameters);
            return (T)(await command.ExecuteScalarAsync())!;
        }

        private async Task Execute(string sql, params (string Name, object Value)[] parameters)
        {
            await using NpgsqlConnection connection = await Factory.OpenConnectionAsync();
            await using var command = Build(connection, sql, parameters);
            await command.ExecuteNonQueryAsync();
        }

        private static NpgsqlCommand Build(NpgsqlConnection connection, string sql, (string Name, object Value)[] parameters)
        {
            var command = new NpgsqlCommand(sql, connection);
            foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value);
            return command;
        }

        public async ValueTask DisposeAsync()
        {
            if (ProjectId != 0) await Execute("DELETE FROM projects WHERE project_id=@id", ("id", ProjectId));
            if (OtherProjectId != 0) await Execute("DELETE FROM projects WHERE project_id=@id", ("id", OtherProjectId));
            if (OwnerId != 0) await Execute("DELETE FROM users WHERE user_id=@id", ("id", OwnerId));
            if (OtherUserId != 0) await Execute("DELETE FROM users WHERE user_id=@id", ("id", OtherUserId));
        }
    }
}
