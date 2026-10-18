using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.Api.Tests.Database;

/// <summary>
/// SCRUM-26: kiểm chứng câu SQL thật của InvitationRepository trên PostgreSQL (unit test dùng repository giả nên không chạm tới).
/// Cần DB test đã apply 01 → 04 → 05 → 06 và hai biến môi trường CREATORFLOW_TEST_CONNECTION_STRING +
/// CREATORFLOW_TEST_ALLOW_WRITES=true (giống các test Database khác); thiếu thì test được bỏ qua (Inconclusive).
/// Mỗi test tự tạo dữ liệu riêng (user, project) và xoá sạch khi xong.
/// </summary>
[TestClass]
[TestCategory("InvitationDatabase")]
public sealed class InvitationRepositoryIntegrationTests
{
    // Làm tròn tới giây để so sánh với TIMESTAMPTZ (PostgreSQL lưu micro giây, .NET lưu 100ns).
    private static DateTime UtcNow()
    {
        DateTime now = DateTime.UtcNow;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
    }

    // ================================================================== vai trò / thành viên

    [TestMethod]
    public async Task GetRole_ReturnsRoleOnlyForActiveMembersOfThatProject()
    {
        await using var f = await Fixture.CreateAsync();

        Assert.AreEqual(ProjectRole.Owner, await f.Repository.GetRoleAsync(f.ProjectId, f.OwnerId));
        Assert.AreEqual(ProjectRole.Creator, await f.Repository.GetRoleAsync(f.ProjectId, f.CreatorId));
        Assert.IsNull(await f.Repository.GetRoleAsync(f.ProjectId, f.InactiveId));   // đã bị gỡ khỏi Project
        Assert.IsNull(await f.Repository.GetRoleAsync(f.ProjectId, f.OutsiderId));   // chưa từng tham gia
        Assert.IsNull(await f.Repository.GetRoleAsync(f.EmptyProjectId, f.OwnerId)); // Owner Project khác không có quyền ở đây
    }

    [TestMethod]
    public async Task IsActiveMemberByEmail_IgnoresCase_AndIgnoresInactiveAndOtherProjects()
    {
        await using var f = await Fixture.CreateAsync();

        Assert.IsTrue(await f.Repository.IsActiveMemberByEmailAsync(f.ProjectId, f.CreatorEmail));
        Assert.IsTrue(await f.Repository.IsActiveMemberByEmailAsync(f.ProjectId, f.CreatorEmail.ToUpperInvariant()));
        Assert.IsFalse(await f.Repository.IsActiveMemberByEmailAsync(f.ProjectId, f.InactiveEmail));
        Assert.IsFalse(await f.Repository.IsActiveMemberByEmailAsync(f.ProjectId, f.OutsiderEmail));
        Assert.IsFalse(await f.Repository.IsActiveMemberByEmailAsync(f.EmptyProjectId, f.CreatorEmail));
    }

    // ================================================================== tạo lời mời: Pending, đúng Project

    [TestMethod]
    public async Task CreatePending_StoresPendingInvitationInThatProject()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();

        ProjectInvitation? created = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Manager, f.OwnerId, "token-" + f.Tag, now, now.AddDays(7));

        Assert.IsNotNull(created);
        Assert.AreEqual(f.ProjectId, created.ProjectId);
        Assert.AreEqual(f.OutsiderEmail, created.InvitedEmail);
        Assert.AreEqual(ProjectRole.Manager, created.Role);
        Assert.AreEqual(InvitationStatus.Pending, created.Status);
        Assert.AreEqual(f.OwnerName, created.InvitedByName);
        Assert.AreEqual(now, created.CreatedAt.ToUniversalTime());
        Assert.AreEqual(now.AddDays(7), created.ExpiresAt.ToUniversalTime());
        Assert.IsNull(created.RespondedAt);

        // token được lưu ở DB; email đã có tài khoản thì invited_user_id được điền.
        Assert.AreEqual("token-" + f.Tag, await f.ScalarAsync<string>(
            "SELECT token FROM project_invitations WHERE invitation_id = @id", ("id", created.InvitationId)));
        Assert.AreEqual(f.OutsiderId, await f.ScalarAsync<long>(
            "SELECT invited_user_id FROM project_invitations WHERE invitation_id = @id", ("id", created.InvitationId)));
    }

    [TestMethod]
    public async Task CreatePending_EmailWithoutAccount_LeavesInvitedUserNull()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();
        string email = $"nobody-{f.Tag}@example.test";

        ProjectInvitation? created = await f.Repository.CreatePendingAsync(
            f.ProjectId, email, ProjectRole.Creator, f.OwnerId, "t-" + f.Tag, now, now.AddDays(7));

        Assert.IsNotNull(created);
        Assert.IsTrue(await f.ScalarAsync<bool>(
            "SELECT invited_user_id IS NULL FROM project_invitations WHERE invitation_id = @id", ("id", created.InvitationId)));
    }

    // ================================================================== AC: không mời trùng

    [TestMethod]
    public async Task CreatePending_SameEmailWhilePending_ReturnsNull_EvenWithDifferentCase_AndKeepsOneRow()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();

        var first = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "a-" + f.Tag, now, now.AddDays(7));
        var second = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail.ToUpperInvariant(), ProjectRole.Manager, f.OwnerId, "b-" + f.Tag, now, now.AddDays(7));

        Assert.IsNotNull(first);
        Assert.IsNull(second); // uq_pending_invitation chặn, repository đổi thành null
        Assert.AreEqual(1, (await f.Repository.GetByProjectAsync(f.ProjectId)).Count);
    }

    [TestMethod]
    public async Task CreatePending_PendingAlreadyExpired_IsMarkedExpiredAndNewOneIsCreated()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();

        // Lời mời cũ: tạo cách đây 10 ngày, đã hết hạn cách đây 3 ngày nhưng DB vẫn ghi PENDING.
        var old = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "old-" + f.Tag, now.AddDays(-10), now.AddDays(-3));
        var fresh = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "new-" + f.Tag, now, now.AddDays(7));

        Assert.IsNotNull(old);
        Assert.IsNotNull(fresh);
        var rows = await f.Repository.GetByProjectAsync(f.ProjectId);
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(InvitationStatus.Expired, rows.Single(r => r.InvitationId == old.InvitationId).Status);
        Assert.AreEqual(InvitationStatus.Pending, rows.Single(r => r.InvitationId == fresh.InvitationId).Status);
    }

    [TestMethod]
    [DataRow("REJECTED")]
    [DataRow("CANCELLED")]
    public async Task CreatePending_AfterRejectedOrCancelled_CreatesNewInvitation(string previousStatus)
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();
        var first = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "a-" + f.Tag, now, now.AddDays(7));
        await f.ExecuteAsync(
            "UPDATE project_invitations SET status = CAST(@s AS invitation_status), responded_at = @now WHERE invitation_id = @id",
            ("s", previousStatus), ("now", now), ("id", first!.InvitationId));

        var again = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "b-" + f.Tag, now, now.AddDays(7));

        Assert.IsNotNull(again);
        Assert.AreEqual(2, (await f.Repository.GetByProjectAsync(f.ProjectId)).Count);
    }

    [TestMethod]
    public async Task CreatePending_SameEmailInAnotherProject_IsNotBlocked()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();

        var inFirst = await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "a-" + f.Tag, now, now.AddDays(7));
        var inSecond = await f.Repository.CreatePendingAsync(
            f.EmptyProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "b-" + f.Tag, now, now.AddDays(7));

        Assert.IsNotNull(inFirst);
        Assert.IsNotNull(inSecond);
        Assert.AreEqual(f.EmptyProjectId, inSecond.ProjectId);
    }

    // ================================================================== schema chặn mời Owner

    [TestMethod]
    public async Task Schema_RejectsOwnerRoleInInvitation()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();

        PostgresException ex = await Assert.ThrowsExactlyAsync<PostgresException>(() => f.ExecuteAsync("""
            INSERT INTO project_invitations (project_id, invited_email, invited_by, role, token, expires_at)
            VALUES (@p, @email, @by, 'OWNER', @token, @exp)
            """, ("p", f.ProjectId), ("email", f.OutsiderEmail), ("by", f.OwnerId), ("token", "o-" + f.Tag), ("exp", now.AddDays(7))));

        Assert.AreEqual(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }

    // ================================================================== danh sách

    [TestMethod]
    public async Task GetByProject_ReturnsOnlyThatProject_NewestFirst()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();
        var older = await f.Repository.CreatePendingAsync(
            f.ProjectId, $"a-{f.Tag}@example.test", ProjectRole.Creator, f.OwnerId, "a-" + f.Tag, now.AddMinutes(-5), now.AddDays(7));
        var newer = await f.Repository.CreatePendingAsync(
            f.ProjectId, $"b-{f.Tag}@example.test", ProjectRole.Manager, f.OwnerId, "b-" + f.Tag, now, now.AddDays(7));
        await f.Repository.CreatePendingAsync(
            f.EmptyProjectId, $"c-{f.Tag}@example.test", ProjectRole.Creator, f.OwnerId, "c-" + f.Tag, now, now.AddDays(7));

        var rows = await f.Repository.GetByProjectAsync(f.ProjectId);

        CollectionAssert.AreEqual(
            new[] { newer!.InvitationId, older!.InvitationId },
            rows.Select(r => r.InvitationId).ToArray());
        Assert.IsTrue(rows.All(r => r.ProjectId == f.ProjectId));
    }

    [TestMethod]
    public async Task CancelPending_ChangesOnlyPendingInvitation_AndSetsResponseTime()
    {
        await using var f = await Fixture.CreateAsync();
        DateTime now = UtcNow();
        ProjectInvitation created = (await f.Repository.CreatePendingAsync(
            f.ProjectId, f.OutsiderEmail, ProjectRole.Creator, f.OwnerId, "cancel-" + f.Tag, now, now.AddDays(7)))!;
        DateTime respondedAt = now.AddMinutes(1);

        Assert.IsTrue(await f.Repository.CancelPendingAsync(created.InvitationId, respondedAt));
        Assert.IsFalse(await f.Repository.CancelPendingAsync(created.InvitationId, respondedAt.AddMinutes(1)));

        ProjectInvitation cancelled = (await f.Repository.GetByProjectAsync(f.ProjectId)).Single(i => i.InvitationId == created.InvitationId);
        Assert.AreEqual(InvitationStatus.Cancelled, cancelled.Status);
        Assert.AreEqual(respondedAt, cancelled.RespondedAt!.Value.ToUniversalTime());
    }

    // ================================================================== fixture

    private sealed class Fixture : IAsyncDisposable
    {
        public required NpgsqlConnectionFactory ConnectionFactory { get; init; }
        public required InvitationRepository Repository { get; init; }
        public required string Tag { get; init; }
        public long OwnerId, CreatorId, InactiveId, OutsiderId, OtherOwnerId, ProjectId, EmptyProjectId;
        public string OwnerName = "SCRUM-26 owner";
        public string CreatorEmail = "", InactiveEmail = "", OutsiderEmail = "";

        public static async Task<Fixture> CreateAsync()
        {
            string? connectionString = Environment.GetEnvironmentVariable("CREATORFLOW_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString) || Environment.GetEnvironmentVariable("CREATORFLOW_TEST_ALLOW_WRITES") != "true")
            {
                Assert.Inconclusive(
                    "Invitation database tests require an explicitly approved test database via " +
                    "CREATORFLOW_TEST_CONNECTION_STRING and CREATORFLOW_TEST_ALLOW_WRITES=true. Application configuration is never used.");
            }

            var factory = new NpgsqlConnectionFactory(connectionString);
            var f = new Fixture
            {
                ConnectionFactory = factory,
                Repository = new InvitationRepository(factory),
                Tag = Guid.NewGuid().ToString("N"),
            };
            try
            {
                f.CreatorEmail = $"scrum26-{f.Tag}-creator@example.test";
                f.InactiveEmail = $"scrum26-{f.Tag}-inactive@example.test";
                f.OutsiderEmail = $"scrum26-{f.Tag}-outsider@example.test";

                f.OwnerId = await f.InsertUserAsync($"scrum26-{f.Tag}-owner@example.test", f.OwnerName);
                f.CreatorId = await f.InsertUserAsync(f.CreatorEmail, "SCRUM-26 creator");
                f.InactiveId = await f.InsertUserAsync(f.InactiveEmail, "SCRUM-26 inactive");
                f.OutsiderId = await f.InsertUserAsync(f.OutsiderEmail, "SCRUM-26 outsider");
                f.OtherOwnerId = await f.InsertUserAsync($"scrum26-{f.Tag}-other-owner@example.test", "SCRUM-26 other owner");

                f.ProjectId = await f.InsertProjectAsync("SCRUM-26 project");
                f.EmptyProjectId = await f.InsertProjectAsync("SCRUM-26 other project", f.OtherOwnerId); // Owner chính không thuộc Project này
                await f.AddMemberAsync(f.ProjectId, f.OwnerId, "OWNER", true);
                await f.AddMemberAsync(f.ProjectId, f.CreatorId, "CREATOR", true);
                await f.AddMemberAsync(f.ProjectId, f.InactiveId, "MANAGER", false);
                await f.AddMemberAsync(f.EmptyProjectId, f.OtherOwnerId, "OWNER", true);
            }
            catch
            {
                await f.DisposeAsync();
                throw;
            }
            return f;
        }

        private Task<long> InsertUserAsync(string email, string displayName) => ScalarAsync<long>("""
            INSERT INTO users (email, password_hash, display_name) VALUES (@email, 'x', @name) RETURNING user_id
            """, ("email", email), ("name", displayName));

        private Task<long> InsertProjectAsync(string name, long? ownerId = null) => ScalarAsync<long>("""
            INSERT INTO projects (project_name, owner_id) VALUES (@name, @owner) RETURNING project_id
            """, ("name", name + " " + Tag), ("owner", ownerId ?? OwnerId));

        private Task AddMemberAsync(long projectId, long userId, string role, bool active) => ExecuteAsync("""
            INSERT INTO project_members (project_id, user_id, role, is_active)
            VALUES (@p, @u, CAST(@role AS project_role), @active)
            """, ("p", projectId), ("u", userId), ("role", role), ("active", active));

        public async Task<T> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
        {
            await using NpgsqlConnection connection = await ConnectionFactory.OpenConnectionAsync();
            await using NpgsqlCommand command = Build(connection, sql, parameters);
            return (T)(await command.ExecuteScalarAsync())!;
        }

        public async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
        {
            await using NpgsqlConnection connection = await ConnectionFactory.OpenConnectionAsync();
            await using NpgsqlCommand command = Build(connection, sql, parameters);
            await command.ExecuteNonQueryAsync();
        }

        private static NpgsqlCommand Build(NpgsqlConnection connection, string sql, (string Name, object Value)[] parameters)
        {
            var command = new NpgsqlCommand(sql, connection);
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
            return command;
        }

        /// <summary>Xoá Project trước (cascade xoá thành viên + lời mời) rồi mới xoá user, vì các khoá ngoại tới users là RESTRICT.</summary>
        public async ValueTask DisposeAsync()
        {
            await ExecuteAsync("DELETE FROM projects WHERE project_name LIKE @pattern", ("pattern", "SCRUM-26%" + Tag));
            await ExecuteAsync("DELETE FROM users WHERE email LIKE @pattern", ("pattern", $"scrum26-{Tag}-%"));
        }
    }
}
