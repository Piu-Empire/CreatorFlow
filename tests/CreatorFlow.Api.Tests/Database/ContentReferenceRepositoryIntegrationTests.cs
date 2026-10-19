using CreatorFlow.Api.Repositories.ContentDetail;
using CreatorFlow.Api.Models.ContentDetail;
using CreatorFlow.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
[TestCategory("ContentReferenceDatabase")]
public sealed class ContentReferenceRepositoryIntegrationTests
{
    [TestMethod]
    public async Task References_PersistAndDeleteWithinTheirContent()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new ContentDetailRepository(fixture.Factory);

        var first = await repository.AddReferenceAsync(fixture.ContentId, "Brief", "https://example.test/brief.pdf", fixture.UserId);
        var second = await repository.AddReferenceAsync(fixture.ContentId, "Style guide", "https://example.test/style", fixture.UserId);
        IReadOnlyList<CreatorFlow.Api.Models.ContentDetail.ContentReferenceRecord> loaded =
            await repository.GetReferencesAsync(fixture.ContentId);

        Assert.AreEqual(2, loaded.Count);
        CollectionAssert.AreEquivalent(new[] { first.ReferenceId, second.ReferenceId }, loaded.Select(item => item.ReferenceId).ToArray());
        Assert.IsFalse(await repository.DeleteReferenceAsync(fixture.ContentId, first.ReferenceId + 1000));
        Assert.IsTrue(await repository.DeleteReferenceAsync(fixture.ContentId, first.ReferenceId));
        loaded = await repository.GetReferencesAsync(fixture.ContentId);
        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(second.ReferenceId, loaded[0].ReferenceId);
    }

    [TestMethod]
    public async Task ReferenceChanges_CommitTogetherAndRollbackWhenDeleteIsMissing()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new ContentDetailRepository(fixture.Factory);
        var existing = await repository.AddReferenceAsync(fixture.ContentId, "Old", "https://example.test/old", fixture.UserId);

        bool saved = await repository.ApplyReferenceChangesAsync(fixture.ContentId,
            [new NewContentReference("New", "https://example.test/new")], [existing.ReferenceId], fixture.UserId);
        Assert.IsTrue(saved);
        IReadOnlyList<CreatorFlow.Api.Models.ContentDetail.ContentReferenceRecord> loaded =
            await repository.GetReferencesAsync(fixture.ContentId);
        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual("New", loaded[0].DisplayName);

        saved = await repository.ApplyReferenceChangesAsync(fixture.ContentId,
            [new NewContentReference("Should roll back", "https://example.test/rollback")], [long.MaxValue], fixture.UserId);
        Assert.IsFalse(saved);
        loaded = await repository.GetReferencesAsync(fixture.ContentId);
        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual("New", loaded[0].DisplayName);
    }

    [TestMethod]
    public async Task References_IgnoreAndPreserveNonLinkAttachments()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new ContentDetailRepository(fixture.Factory);
        var reference = await repository.AddReferenceAsync(
            fixture.ContentId, "Brief", "https://example.test/brief", fixture.UserId);
        long fileAttachmentId = await fixture.ScalarAsync<long>("""
            INSERT INTO content_attachments(content_id, display_name, attachment_type, resource_url, added_by)
            VALUES(@content, 'File', 'FILE', 'https://example.test/file', @user)
            RETURNING attachment_id;
            """, ("content", fixture.ContentId), ("user", fixture.UserId));

        IReadOnlyList<ContentReferenceRecord> loaded = await repository.GetReferencesAsync(fixture.ContentId);
        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(reference.ReferenceId, loaded[0].ReferenceId);
        Assert.IsFalse(await repository.DeleteReferenceAsync(fixture.ContentId, fileAttachmentId));

        bool saved = await repository.ApplyReferenceChangesAsync(fixture.ContentId,
            [new NewContentReference("Should roll back", "https://example.test/rollback")],
            [reference.ReferenceId, fileAttachmentId], fixture.UserId);

        Assert.IsFalse(saved);
        loaded = await repository.GetReferencesAsync(fixture.ContentId);
        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(reference.ReferenceId, loaded[0].ReferenceId);
        long remainingFileAttachments = await fixture.ScalarAsync<long>("""
            SELECT COUNT(*) FROM content_attachments
            WHERE content_id = @content AND attachment_id = @attachment AND attachment_type = 'FILE';
            """, ("content", fixture.ContentId), ("attachment", fileAttachmentId));
        Assert.AreEqual(1L, remainingFileAttachments);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string connectionString;
        public NpgsqlConnectionFactory Factory { get; }
        public long UserId { get; private set; }
        public long ProjectId { get; private set; }
        public long ContentId { get; private set; }

        private Fixture(string connectionString)
        {
            this.connectionString = connectionString;
            Factory = new NpgsqlConnectionFactory(connectionString);
        }

        public static async Task<Fixture> CreateAsync()
        {
            string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true")
                Assert.Inconclusive("Content reference persistence requires a disposable database, CREATORFLOW_TEST_CONNECTION_STRING, and CREATORFLOW_TEST_ALLOW_WRITES=true.");

            var fixture = new Fixture(connectionString!);
            try
            {
                string tag = Guid.NewGuid().ToString("N");
                fixture.UserId = await fixture.ScalarAsync<long>("""
                    INSERT INTO users(email,password_hash,display_name)
                    VALUES(@email,'unused','Reference fixture') RETURNING user_id;
                    """, ("email", $"reference-{tag}@example.test"));
                fixture.ProjectId = await fixture.ScalarAsync<long>("""
                    INSERT INTO projects(project_name,owner_id) VALUES(@name,@owner) RETURNING project_id;
                    """, ("name", "Reference fixture " + tag), ("owner", fixture.UserId));
                await fixture.ExecuteAsync("""
                    INSERT INTO project_members(project_id,user_id,role) VALUES(@project,@user,'OWNER');
                    """, ("project", fixture.ProjectId), ("user", fixture.UserId));
                fixture.ContentId = await fixture.ScalarAsync<long>("""
                    INSERT INTO contents(project_id,title,created_by) VALUES(@project,'Reference fixture',@user) RETURNING content_id;
                    """, ("project", fixture.ProjectId), ("user", fixture.UserId));
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (ProjectId > 0) await ExecuteAsync("DELETE FROM projects WHERE project_id=@id;", ("id", ProjectId));
            if (UserId > 0) await ExecuteAsync("DELETE FROM users WHERE user_id=@id;", ("id", UserId));
        }

        internal async Task<T> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
        {
            await using NpgsqlConnection connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value);
            return (T)(await command.ExecuteScalarAsync())!;
        }

        private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
        {
            await using NpgsqlConnection connection = await Factory.OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value);
            await command.ExecuteNonQueryAsync();
        }
    }
}
