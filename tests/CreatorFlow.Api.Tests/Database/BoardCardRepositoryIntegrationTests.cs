using CreatorFlow.Api.Repositories.Board;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.Api.Tests.Database;

/// <summary>Checks the Board/library SQL against PostgreSQL using the opt-in disposable test database.</summary>
[TestClass]
[TestCategory("BoardDatabase")]
public sealed class BoardCardRepositoryIntegrationTests
{
    [TestMethod]
    public async Task ProjectLibraryQueryIncludesArchivedAndReadsPlatformsTagsAndAssignments()
    {
        await using var fixture = await Fixture.CreateAsync();
        long activeContent = await fixture.InsertContent("Active content", "SCRIPT");
        long archivedContent = await fixture.InsertContent("Archived content", "ARCHIVED");
        long platformId = await fixture.InsertPlatform();
        await fixture.LinkPlatform(activeContent, platformId);
        await fixture.LinkPlatform(archivedContent, platformId);
        long tagId = await fixture.InsertTag("project library");
        await fixture.LinkTag(archivedContent, tagId);
        await fixture.Assign(archivedContent);
        await fixture.AddPendingReview(activeContent);

        IReadOnlyList<CreatorFlow.Api.Models.Board.BoardCard> board =
            await new BoardCardRepository(fixture.Factory).GetCardsAsync(fixture.ProjectId);
        IReadOnlyList<CreatorFlow.Api.Models.Board.BoardCard> library =
            await new BoardCardRepository(fixture.Factory).GetProjectContentsAsync(fixture.ProjectId);
        var assignees = await new TaskRepository(fixture.Factory).GetBoardAssigneesAsync(fixture.ProjectId);

        Assert.AreEqual(1, board.Count);
        Assert.AreEqual("Active content", board[0].Title);
        Assert.IsTrue(board[0].HasPendingReview);
        Assert.AreEqual(2, library.Count);
        var archived = library.Single(card => card.ContentId == archivedContent);
        Assert.AreEqual("Archived", archived.Status);
        CollectionAssert.AreEqual(new[] { fixture.PlatformName }, archived.Platforms.ToArray());
        CollectionAssert.AreEqual(new[] { "project library" }, archived.Tags.ToArray());
        var assignee = assignees.Single(item => item.ContentId == archivedContent);
        Assert.AreEqual(fixture.AssigneeName, assignee.DisplayName);
        Assert.AreEqual("InProgress", assignee.Status.ToString());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string tag = Guid.NewGuid().ToString("N");
        private readonly string ownerEmail;
        private readonly string assigneeEmail;
        private readonly string platformCode;
        public string PlatformName => "TikTok " + tag;
        public NpgsqlConnectionFactory Factory { get; }
        public long ProjectId { get; private set; }
        public long OwnerId { get; private set; }
        public long AssigneeId { get; private set; }
        public string AssigneeName => "Library assignee " + tag;

        private Fixture(string connectionString)
        {
            Factory = new NpgsqlConnectionFactory(connectionString);
            ownerEmail = $"board-owner-{tag}@example.test";
            assigneeEmail = $"board-assignee-{tag}@example.test";
            platformCode = "BOARD_" + tag;
        }

        public static async Task<Fixture> CreateAsync()
        {
            string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true")
                Assert.Inconclusive("Board repository integration tests require CREATORFLOW_TEST_CONNECTION_STRING and CREATORFLOW_TEST_ALLOW_WRITES=true.");

            var fixture = new Fixture(connectionString!);
            try
            {
                fixture.OwnerId = await fixture.InsertUser(fixture.ownerEmail, "Board owner " + fixture.tag);
                fixture.AssigneeId = await fixture.InsertUser(fixture.assigneeEmail, fixture.AssigneeName);
                fixture.ProjectId = await fixture.InsertProject();
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public Task<long> InsertContent(string title, string status) => Scalar<long>("""
            INSERT INTO contents(project_id,title,created_by,status)
            VALUES(@project,@title,@owner,CAST(@status AS content_status)) RETURNING content_id
            """, ("project", ProjectId), ("title", title), ("owner", OwnerId), ("status", status));

        public Task<long> InsertPlatform() => Scalar<long>("""
            INSERT INTO platforms(code,name) VALUES(@code,@name) RETURNING platform_id
            """, ("code", platformCode), ("name", PlatformName));

        public Task LinkPlatform(long contentId, long platformId) => Execute("""
            INSERT INTO content_platforms(content_id,platform_id) VALUES(@content,@platform)
            """, ("content", contentId), ("platform", platformId));

        public Task<long> InsertTag(string name) => Scalar<long>("""
            INSERT INTO tags(project_id,name) VALUES(@project,@name) RETURNING tag_id
            """, ("project", ProjectId), ("name", name));

        public Task LinkTag(long contentId, long tagId) => Execute("""
            INSERT INTO content_tags(content_id,tag_id) VALUES(@content,@tag)
            """, ("content", contentId), ("tag", tagId));

        public Task Assign(long contentId) => Execute("""
            INSERT INTO content_assignments(content_id,assignee_id,assigned_by,status,progress_percent)
            VALUES(@content,@assignee,@owner,'IN_PROGRESS',45)
            """, ("content", contentId), ("assignee", AssigneeId), ("owner", OwnerId));

        public Task AddPendingReview(long contentId) => Execute("""
            INSERT INTO reviews(content_id,review_no,submitted_by,status)
            VALUES(@content,1,@owner,'PENDING')
            """, ("content", contentId), ("owner", OwnerId));

        private async Task<long> InsertUser(string email, string name) => await Scalar<long>("""
            INSERT INTO users(email,password_hash,display_name) VALUES(@email,'unused',@name) RETURNING user_id
            """, ("email", email), ("name", name));

        private async Task<long> InsertProject() => await Scalar<long>("""
            INSERT INTO projects(project_name,owner_id) VALUES(@name,@owner) RETURNING project_id
            """, ("name", "Board library " + tag), ("owner", OwnerId));

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
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
            return command;
        }

        public async ValueTask DisposeAsync()
        {
            if (ProjectId != 0) await Execute("DELETE FROM projects WHERE project_id=@project", ("project", ProjectId));
            if (!string.IsNullOrEmpty(platformCode)) await Execute("DELETE FROM platforms WHERE code=@code", ("code", platformCode));
            if (OwnerId != 0 || AssigneeId != 0)
                await Execute("DELETE FROM users WHERE user_id=@owner OR user_id=@assignee", ("owner", OwnerId), ("assignee", AssigneeId));
        }
    }
}
