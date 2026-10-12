using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace CreatorFlow.Api.Tests.Database;

/// <summary>
/// Kiểm chứng câu SQL thật của MyInvitationRepository trên PostgreSQL (unit test dùng repository giả nên không chạm tới):
/// chấp nhận tạo project_members ĐÚNG MỘT LẦN (kể cả khi chấp nhận đồng thời), từ chối không tạo, trạng thái lời mời được cập nhật.
/// Cần DB test đã apply 01 → 04 → 05 → 06 và hai biến môi trường CREATORFLOW_TEST_CONNECTION_STRING +
/// CREATORFLOW_TEST_ALLOW_WRITES=true (giống các test Database khác); thiếu thì test được bỏ qua (Inconclusive).
/// Mỗi test tự tạo dữ liệu riêng (user, project) và xoá sạch khi xong.
/// </summary>
[TestClass]
[TestCategory("InvitationDatabase")]
public sealed class MyInvitationRepositoryIntegrationTests
{
    // Làm tròn tới giây để so sánh với TIMESTAMPTZ (PostgreSQL lưu micro giây, .NET lưu 100ns).
    private static DateTime UtcNow() => DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));

    // ================================================================== xem lời mời

    [TestMethod]
    public async Task GetByEmail_ReturnsOnlyThatEmail_IgnoringCase_NewestFirst_WithProjectName()
    {
        await using var f = await Fixture.CreateAsync();
        long older = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", "REJECTED", UtcNow().AddDays(5), UtcNow().AddDays(-3));
        long newer = await f.InsertInvitationAsync(f.InviteeEmail.ToUpperInvariant(), "MANAGER", "PENDING", UtcNow().AddDays(5), UtcNow().AddDays(-1));
        await f.InsertInvitationAsync(f.OtherEmail, "CREATOR", "PENDING", UtcNow().AddDays(5), UtcNow());

        var rows = await f.Repository.GetByEmailAsync(f.InviteeEmail);

        CollectionAssert.AreEqual(new[] { newer, older }, rows.Select(r => r.InvitationId).ToArray());
        Assert.AreEqual(f.ProjectName, rows[0].ProjectName);
        Assert.AreEqual(ProjectRole.Manager, rows[0].Role);
        Assert.AreEqual(InvitationStatus.Pending, rows[0].Status);
        Assert.AreEqual(f.OwnerName, rows[0].InvitedByName);
    }

    // ================================================================== chấp nhận

    [TestMethod]
    public async Task Accept_CreatesActiveMembershipWithInvitedRole_AndMarksAccepted()
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "MANAGER", "PENDING", UtcNow().AddDays(7));

        var result = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.Decided, result.Outcome);
        Assert.AreEqual(InvitationStatus.Accepted, result.Invitation!.Status);
        Assert.IsNotNull(result.Invitation.RespondedAt);
        Assert.AreEqual(1L, await f.CountMembersAsync(f.InviteeId));
        Assert.AreEqual("MANAGER", await f.MemberRoleAsync(f.InviteeId));
        Assert.IsTrue(await f.MemberIsActiveAsync(f.InviteeId));
        Assert.AreEqual("ACCEPTED", await f.InvitationStatusAsync(id));
        Assert.AreEqual(f.InviteeId, await f.ScalarAsync<long>(
            "SELECT invited_user_id FROM project_invitations WHERE invitation_id = @id", ("id", id)));
    }

    [TestMethod]
    public async Task Accept_Twice_SecondIsNotPending_AndStillExactlyOneMember()
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", "PENDING", UtcNow().AddDays(7));

        var first = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());
        var second = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.Decided, first.Outcome);
        Assert.AreEqual(InvitationDecisionOutcome.NotPending, second.Outcome);
        Assert.AreEqual(InvitationStatus.Accepted, second.CurrentStatus);
        Assert.AreEqual(1L, await f.CountMembersAsync(f.InviteeId));
    }

    [TestMethod]
    public async Task Accept_Concurrently_OnlyOneSucceeds_AndExactlyOneMember()
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", "PENDING", UtcNow().AddDays(7));

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow())));

        Assert.AreEqual(1, results.Count(r => r.Outcome == InvitationDecisionOutcome.Decided));
        Assert.AreEqual(7, results.Count(r => r.Outcome == InvitationDecisionOutcome.NotPending));
        Assert.AreEqual(1L, await f.CountMembersAsync(f.InviteeId));
        Assert.AreEqual("ACCEPTED", await f.InvitationStatusAsync(id));
    }

    [TestMethod]
    public async Task Accept_WhenPreviouslyRemoved_ReactivatesTheSameRowWithInvitedRole()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddMemberAsync(f.InviteeId, "CREATOR", active: false);
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "MANAGER", "PENDING", UtcNow().AddDays(7));

        var result = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.Decided, result.Outcome);
        Assert.AreEqual(1L, await f.CountMembersAsync(f.InviteeId));
        Assert.IsTrue(await f.MemberIsActiveAsync(f.InviteeId));
        Assert.AreEqual("MANAGER", await f.MemberRoleAsync(f.InviteeId));
    }

    [TestMethod]
    public async Task Accept_WhenAlreadyActiveMember_KeepsRoleAndSingleRow_AndMarksAccepted()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddMemberAsync(f.InviteeId, "MANAGER", active: true);
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", "PENDING", UtcNow().AddDays(7));

        var result = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.Decided, result.Outcome);
        Assert.AreEqual(1L, await f.CountMembersAsync(f.InviteeId));
        Assert.AreEqual("MANAGER", await f.MemberRoleAsync(f.InviteeId)); // không bị hạ vai trò
        Assert.AreEqual("ACCEPTED", await f.InvitationStatusAsync(id));
    }

    // ================================================================== từ chối

    [TestMethod]
    public async Task Reject_MarksRejected_AndCreatesNoMembership()
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", "PENDING", UtcNow().AddDays(7));

        var result = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Rejected, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.Decided, result.Outcome);
        Assert.AreEqual(InvitationStatus.Rejected, result.Invitation!.Status);
        Assert.IsNotNull(result.Invitation.RespondedAt);
        Assert.AreEqual(0L, await f.CountMembersAsync(f.InviteeId));
        Assert.AreEqual("REJECTED", await f.InvitationStatusAsync(id));
    }

    [TestMethod]
    public async Task Reject_ThenAccept_IsNotPending_AndStillNoMembership()
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", "PENDING", UtcNow().AddDays(7));

        await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Rejected, UtcNow());
        var accept = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.NotPending, accept.Outcome);
        Assert.AreEqual(InvitationStatus.Rejected, accept.CurrentStatus);
        Assert.AreEqual(0L, await f.CountMembersAsync(f.InviteeId));
    }

    // ================================================================== không được phép / không còn hiệu lực

    [TestMethod]
    public async Task Decide_InvitationOfAnotherEmail_IsNotFound_AndChangesNothing()
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.OtherEmail, "CREATOR", "PENDING", UtcNow().AddDays(7));

        var result = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.NotFound, result.Outcome);
        Assert.AreEqual(0L, await f.CountMembersAsync(f.InviteeId));
        Assert.AreEqual("PENDING", await f.InvitationStatusAsync(id));
    }

    [TestMethod]
    public async Task Decide_UnknownInvitation_IsNotFound()
    {
        await using var f = await Fixture.CreateAsync();

        var result = await f.Repository.DecideAsync(long.MaxValue, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.NotFound, result.Outcome);
    }

    [TestMethod]
    public async Task Accept_PendingButExpired_MarksExpired_AndCreatesNoMembership()
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", "PENDING", UtcNow().AddMinutes(-5), UtcNow().AddDays(-8));

        var result = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.Expired, result.Outcome);
        Assert.AreEqual(0L, await f.CountMembersAsync(f.InviteeId));
        Assert.AreEqual("EXPIRED", await f.InvitationStatusAsync(id));
    }

    [TestMethod]
    [DataRow("CANCELLED", InvitationStatus.Cancelled)]
    [DataRow("EXPIRED", InvitationStatus.Expired)]
    public async Task Accept_NotPending_ReportsCurrentStatus_AndCreatesNoMembership(string dbStatus, InvitationStatus expected)
    {
        await using var f = await Fixture.CreateAsync();
        long id = await f.InsertInvitationAsync(f.InviteeEmail, "CREATOR", dbStatus, UtcNow().AddDays(7));

        var result = await f.Repository.DecideAsync(id, f.InviteeId, f.InviteeEmail, InvitationStatus.Accepted, UtcNow());

        Assert.AreEqual(InvitationDecisionOutcome.NotPending, result.Outcome);
        Assert.AreEqual(expected, result.CurrentStatus);
        Assert.AreEqual(0L, await f.CountMembersAsync(f.InviteeId));
    }

    // ================================================================== dữ liệu thử

    private sealed class Fixture : IAsyncDisposable
    {
        public NpgsqlConnectionFactory ConnectionFactory { get; private init; } = null!;
        public MyInvitationRepository Repository { get; private init; } = null!;
        public string Tag { get; private init; } = "";

        public long OwnerId { get; private set; }
        public long InviteeId { get; private set; }
        public long ProjectId { get; private set; }
        public string OwnerName { get; } = "INVRESP owner";
        public string ProjectName { get; private set; } = "";
        public string InviteeEmail { get; private set; } = "";
        public string OtherEmail { get; private set; } = "";

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
                Repository = new MyInvitationRepository(factory),
                Tag = Guid.NewGuid().ToString("N"),
            };
            try
            {
                f.InviteeEmail = $"invresp-{f.Tag}-invitee@example.test";
                f.OtherEmail = $"invresp-{f.Tag}-other@example.test";

                f.OwnerId = await f.InsertUserAsync($"invresp-{f.Tag}-owner@example.test", f.OwnerName);
                f.InviteeId = await f.InsertUserAsync(f.InviteeEmail, "INVRESP invitee");
                await f.InsertUserAsync(f.OtherEmail, "INVRESP other");

                f.ProjectName = "INVRESP project " + f.Tag;
                f.ProjectId = await f.ScalarAsync<long>("""
                    INSERT INTO projects (project_name, owner_id) VALUES (@name, @owner) RETURNING project_id
                    """, ("name", f.ProjectName), ("owner", f.OwnerId));
                await f.ExecuteAsync("""
                    INSERT INTO project_members (project_id, user_id, role) VALUES (@p, @u, 'OWNER'::project_role)
                    """, ("p", f.ProjectId), ("u", f.OwnerId));
            }
            catch
            {
                await f.DisposeAsync();
                throw;
            }
            return f;
        }

        public Task<long> InsertInvitationAsync(string email, string role, string status, DateTime expiresAt, DateTime? createdAt = null) =>
            ScalarAsync<long>("""
                INSERT INTO project_invitations (project_id, invited_email, invited_by, role, token, status, expires_at, created_at)
                VALUES (@p, @email, @by, CAST(@role AS project_role), @token, CAST(@status AS invitation_status), @expires, @created)
                RETURNING invitation_id
                """,
                ("p", ProjectId), ("email", email), ("by", OwnerId), ("role", role),
                ("token", Guid.NewGuid().ToString("N")), ("status", status), ("expires", expiresAt),
                ("created", createdAt ?? UtcNow()));

        public Task AddMemberAsync(long userId, string role, bool active) => ExecuteAsync("""
            INSERT INTO project_members (project_id, user_id, role, is_active)
            VALUES (@p, @u, CAST(@role AS project_role), @active)
            """, ("p", ProjectId), ("u", userId), ("role", role), ("active", active));

        public Task<long> CountMembersAsync(long userId) => ScalarAsync<long>(
            "SELECT COUNT(*) FROM project_members WHERE project_id = @p AND user_id = @u", ("p", ProjectId), ("u", userId));

        public Task<string> MemberRoleAsync(long userId) => ScalarAsync<string>(
            "SELECT role::text FROM project_members WHERE project_id = @p AND user_id = @u", ("p", ProjectId), ("u", userId));

        public Task<bool> MemberIsActiveAsync(long userId) => ScalarAsync<bool>(
            "SELECT is_active FROM project_members WHERE project_id = @p AND user_id = @u", ("p", ProjectId), ("u", userId));

        public Task<string> InvitationStatusAsync(long invitationId) => ScalarAsync<string>(
            "SELECT status::text FROM project_invitations WHERE invitation_id = @id", ("id", invitationId));

        private Task<long> InsertUserAsync(string email, string displayName) => ScalarAsync<long>("""
            INSERT INTO users (email, password_hash, display_name) VALUES (@email, 'x', @name) RETURNING user_id
            """, ("email", email), ("name", displayName));

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
            await ExecuteAsync("DELETE FROM projects WHERE project_name LIKE @pattern", ("pattern", "INVRESP%" + Tag));
            await ExecuteAsync("DELETE FROM users WHERE email LIKE @pattern", ("pattern", $"invresp-{Tag}-%"));
        }
    }
}
