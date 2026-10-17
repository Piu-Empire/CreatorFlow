using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Api.Services.Invitations;
using CreatorFlow.Contracts.Invitations;
using CreatorFlow.Api.Tests.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Invitations;

/// <summary>
/// SCRUM-26 "Mời User tham gia Project". Quyền và quy tắc được thực thi Ở BACKEND:
///   - chỉ Owner được mời / xem lời mời;
///   - không mời trùng member (đã là thành viên, hoặc còn lời mời Pending còn hạn);
///   - lưu trạng thái Pending, lời mời thuộc đúng Project.
/// </summary>
[TestClass]
public sealed class InvitationServiceTests
{
    private const long ProjectId = 1;
    private const long OtherProjectId = 2;
    private const long OwnerId = 1;
    private const long CreatorId = 2;
    private const long ManagerId = 3;
    private const string MemberEmail = "member@x.vn";

    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 30, 0, TimeSpan.Zero);

    private static InviteMemberRequest Invite(string? email = "new@x.vn", string? role = "Creator") => new(email, role);

    // ================================================================== quyền

    [TestMethod]
    public async Task Invite_NotSignedIn_Returns401AndWritesNothing()
    {
        var f = new Fixture(actingUserId: null);

        var result = await f.Service.InviteAsync(ProjectId, Invite());

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual("unauthorized", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.Invitations.Count);
    }

    [TestMethod]
    public async Task Invite_NotProjectMember_Returns403()
    {
        var f = new Fixture(99);

        var result = await f.Service.InviteAsync(ProjectId, Invite());

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Repo.Invitations.Count);
    }

    [TestMethod]
    [DataRow(ManagerId)]
    [DataRow(CreatorId)]
    public async Task Invite_ManagerOrCreator_Returns403(long actingUserId)
    {
        var f = new Fixture(actingUserId);

        var result = await f.Service.InviteAsync(ProjectId, Invite());

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual("forbidden", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.Invitations.Count);
    }

    [TestMethod]
    public async Task Invite_OwnerOfAnotherProject_Returns403()
    {
        // Owner của Project #1 không có vai trò gì ở Project #2: đổi projectId trên route vẫn bị chặn.
        var f = new Fixture(OwnerId, ownsOtherProject: false);

        var result = await f.Service.InviteAsync(OtherProjectId, Invite());

        Assert.AreEqual(403, result.Status);
    }

    // ================================================================== validation

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not-an-email")]
    public async Task Invite_InvalidEmail_Returns400OnEmailField(string? email)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.InviteAsync(ProjectId, Invite(email));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("email", result.ErrorField);
        Assert.AreEqual(0, f.Repo.Invitations.Count);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Owner")]      // schema cấm mời Owner
    [DataRow("Admin")]
    [DataRow("1")]          // số không được coi là tên enum
    [DataRow("Owner,Manager")]
    public async Task Invite_InvalidRole_Returns400OnRoleField(string? role)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.InviteAsync(ProjectId, Invite(role: role));

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("role", result.ErrorField);
        Assert.AreEqual(0, f.Repo.Invitations.Count);
    }

    // ================================================================== AC 2 + 3: lưu Pending, thuộc đúng Project

    [TestMethod]
    [DataRow("Manager")]
    [DataRow("creator")] // không phân biệt hoa/thường
    public async Task Invite_Owner_CreatesPendingInvitationInThatProject(string role)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.InviteAsync(ProjectId, Invite("  New@X.vn  ", role));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(201, result.Status);
        Assert.AreEqual("Pending", result.Value!.Status);
        Assert.AreEqual(ProjectId, result.Value.ProjectId);
        Assert.AreEqual("New@X.vn", result.Value.InvitedEmail); // đã Trim
        Assert.AreEqual(char.ToUpperInvariant(role[0]) + role[1..].ToLowerInvariant(), result.Value.Role);
        Assert.AreEqual("User 1", result.Value.InvitedByName);   // người mời lấy từ JWT
        Assert.AreEqual(Now.UtcDateTime, result.Value.CreatedAt);
        Assert.AreEqual(Now.UtcDateTime + InvitationRules.Lifetime, result.Value.ExpiresAt);

        Assert.AreEqual(1, f.Repo.Invitations.Count);
        Assert.AreEqual(OwnerId, f.Repo.InvitedBy.Single());
        Assert.AreEqual(43, f.Repo.Tokens.Single().Length);      // 256 bit base64url
        var sent = f.EmailSender.Invitations.Single();
        Assert.AreEqual("New@X.vn", sent.Email);
        Assert.AreEqual(ProjectId, sent.ProjectId);
        Assert.AreEqual(result.Value.Role, sent.Role);
        Assert.AreEqual("User 1", sent.InviterName);
        Assert.AreEqual(result.Value.ExpiresAt, sent.ExpiresAtUtc);
    }

    [TestMethod]
    public async Task Invite_EmailNotConfigured_Returns503AndDoesNotPersistInvitation()
    {
        var f = new Fixture(OwnerId);
        f.EmailSender.Configured = false;

        var result = await f.Service.InviteAsync(ProjectId, Invite());

        Assert.AreEqual(503, result.Status);
        Assert.AreEqual("email_not_configured", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.Invitations.Count);
        Assert.AreEqual(0, f.EmailSender.Invitations.Count);
    }

    [TestMethod]
    public async Task Invite_EmailDeliveryFails_CancelsPendingInvitationAndReturns503()
    {
        var f = new Fixture(OwnerId);
        f.EmailSender.Fail = true;

        var result = await f.Service.InviteAsync(ProjectId, Invite());

        Assert.AreEqual(503, result.Status);
        Assert.AreEqual("email_delivery_failed", result.ErrorCode);
        Assert.AreEqual(InvitationStatus.Cancelled, f.Repo.Invitations.Single().Status);
        Assert.IsNotNull(f.Repo.Invitations.Single().RespondedAt);
    }

    [TestMethod]
    public async Task Invite_EachInvitationGetsADifferentToken()
    {
        var f = new Fixture(OwnerId);

        await f.Service.InviteAsync(ProjectId, Invite("a@x.vn"));
        await f.Service.InviteAsync(ProjectId, Invite("b@x.vn"));

        Assert.AreEqual(2, f.Repo.Tokens.Distinct().Count());
    }

    // ================================================================== AC 1: không mời trùng member

    [TestMethod]
    [DataRow(MemberEmail)]
    [DataRow("MEMBER@X.VN")]   // không phân biệt hoa/thường
    [DataRow("  member@x.vn ")]
    public async Task Invite_AlreadyActiveMember_Returns409AndWritesNothing(string email)
    {
        var f = new Fixture(OwnerId);

        var result = await f.Service.InviteAsync(ProjectId, Invite(email));

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("already_member", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.Invitations.Count);
    }

    [TestMethod]
    public async Task Invite_SameEmailWhilePending_Returns409_ThenOnlyOneInvitationExists()
    {
        var f = new Fixture(OwnerId);

        var first = await f.Service.InviteAsync(ProjectId, Invite("dup@x.vn"));
        var second = await f.Service.InviteAsync(ProjectId, Invite("DUP@x.vn"));

        Assert.IsTrue(first.Succeeded);
        Assert.AreEqual(409, second.Status);
        Assert.AreEqual("invitation_pending", second.ErrorCode);
        Assert.AreEqual(1, f.Repo.Invitations.Count);
    }

    [TestMethod]
    public async Task Invite_PendingExpired_IsExpiredAndNewInvitationIsCreated()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Invitations.Add(Row(10, ProjectId, "late@x.vn", InvitationStatus.Pending, expiresAt: Now.UtcDateTime.AddDays(-1)));

        var result = await f.Service.InviteAsync(ProjectId, Invite("late@x.vn"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(InvitationStatus.Expired, f.Repo.Invitations.Single(i => i.InvitationId == 10).Status);
        Assert.AreEqual(2, f.Repo.Invitations.Count);
    }

    [TestMethod]
    [DataRow(InvitationStatus.Rejected)]
    [DataRow(InvitationStatus.Cancelled)]
    public async Task Invite_AfterRejectedOrCancelled_CanInviteAgain(InvitationStatus previous)
    {
        var f = new Fixture(OwnerId);
        f.Repo.Invitations.Add(Row(10, ProjectId, "again@x.vn", previous));

        var result = await f.Service.InviteAsync(ProjectId, Invite("again@x.vn"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(2, f.Repo.Invitations.Count);
    }

    [TestMethod]
    public async Task Invite_PendingInAnotherProject_DoesNotBlock()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Invitations.Add(Row(10, OtherProjectId, "multi@x.vn", InvitationStatus.Pending));

        var result = await f.Service.InviteAsync(ProjectId, Invite("multi@x.vn"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ProjectId, result.Value!.ProjectId);
    }

    // ================================================================== danh sách

    [TestMethod]
    public async Task GetInvitations_Owner_SeesOnlyThatProject_AndStalePendingShownAsExpired()
    {
        var f = new Fixture(OwnerId);
        f.Repo.Invitations.Add(Row(1, ProjectId, "a@x.vn", InvitationStatus.Pending));
        f.Repo.Invitations.Add(Row(2, ProjectId, "b@x.vn", InvitationStatus.Accepted));
        f.Repo.Invitations.Add(Row(3, ProjectId, "c@x.vn", InvitationStatus.Pending, expiresAt: Now.UtcDateTime.AddHours(-1)));
        f.Repo.Invitations.Add(Row(4, OtherProjectId, "d@x.vn", InvitationStatus.Pending));

        var result = await f.Service.GetInvitationsAsync(ProjectId);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, result.Value!.Invitations.Select(i => i.InvitationId).ToArray());
        CollectionAssert.AreEqual(new[] { "Pending", "Accepted", "Expired" }, result.Value.Invitations.Select(i => i.Status).ToArray());
    }

    [TestMethod]
    public async Task GetInvitations_NotSignedIn_NonMember_AndNonOwner_AreRejected()
    {
        Assert.AreEqual(401, (await new Fixture(null).Service.GetInvitationsAsync(ProjectId)).Status);
        Assert.AreEqual(403, (await new Fixture(99).Service.GetInvitationsAsync(ProjectId)).Status);
        Assert.AreEqual(403, (await new Fixture(ManagerId).Service.GetInvitationsAsync(ProjectId)).Status);
        Assert.AreEqual(403, (await new Fixture(CreatorId).Service.GetInvitationsAsync(ProjectId)).Status);
    }

    // ================================================================== quy tắc thuần

    [TestMethod]
    public void Rules_OnlyOwnerCanInvite()
    {
        Assert.IsTrue(InvitationRules.CanInvite(ProjectRole.Owner));
        Assert.IsFalse(InvitationRules.CanInvite(ProjectRole.Manager));
        Assert.IsFalse(InvitationRules.CanInvite(ProjectRole.Creator));
        Assert.IsFalse(InvitationRules.CanInvite(null));
    }

    [TestMethod]
    public void Rules_EffectiveStatus_OnlyPendingCanExpire()
    {
        DateTime now = Now.UtcDateTime;

        Assert.AreEqual(InvitationStatus.Expired, InvitationRules.EffectiveStatus(InvitationStatus.Pending, now, now));
        Assert.AreEqual(InvitationStatus.Pending, InvitationRules.EffectiveStatus(InvitationStatus.Pending, now.AddSeconds(1), now));
        Assert.AreEqual(InvitationStatus.Accepted, InvitationRules.EffectiveStatus(InvitationStatus.Accepted, now.AddDays(-9), now));
    }

    // ================================================================== test double

    private static ProjectInvitation Row(
        long id, long projectId, string email, InvitationStatus status, DateTime? expiresAt = null) =>
        new(id, projectId, email, ProjectRole.Creator, status, "User 1",
            Now.UtcDateTime.AddDays(-1), expiresAt ?? Now.UtcDateTime.AddDays(6), null);

    private sealed class Fixture
    {
        private readonly long? _actingUserId;
        private readonly bool _ownsOtherProject;
        private InvitationService? _service;
        private FakeInvitationRepository? _repo;
        public TestEmailSender EmailSender { get; } = new();

        public Fixture(long? actingUserId, bool ownsOtherProject = true)
        {
            _actingUserId = actingUserId;
            _ownsOtherProject = ownsOtherProject;
        }

        public FakeInvitationRepository Repo => _repo ??= new FakeInvitationRepository(_ownsOtherProject);

        public InvitationService Service => _service ??= new InvitationService(
            Repo,
            new CurrentAuthenticatedUser
            {
                User = _actingUserId is long id
                    ? new User { UserId = id, Email = $"u{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                    : null,
            },
            new FixedTimeProvider(), EmailSender, NullLogger<InvitationService>.Instance);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>Repository giả lập đúng ngữ nghĩa database: unique Pending theo (project, email không phân biệt hoa/thường).</summary>
    private sealed class FakeInvitationRepository(bool ownsOtherProject) : IInvitationRepository
    {
        public List<ProjectInvitation> Invitations { get; } = new();
        public List<long> InvitedBy { get; } = new();
        public List<string> Tokens { get; } = new();

        private readonly Dictionary<(long Project, long User), ProjectRole> _roles = new()
        {
            [(ProjectId, OwnerId)] = ProjectRole.Owner,
            [(ProjectId, ManagerId)] = ProjectRole.Manager,
            [(ProjectId, CreatorId)] = ProjectRole.Creator,
        };

        public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default)
        {
            if (ownsOtherProject) _roles[(OtherProjectId, OwnerId)] = ProjectRole.Owner;
            return Task.FromResult<ProjectRole?>(_roles.TryGetValue((projectId, userId), out ProjectRole role) ? role : null);
        }

        public Task<bool> IsActiveMemberByEmailAsync(long projectId, string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(projectId == ProjectId && string.Equals(email, MemberEmail, StringComparison.OrdinalIgnoreCase));

        public Task<ProjectInvitation?> CreatePendingAsync(
            long projectId, string email, ProjectRole role, long invitedBy, string token,
            DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
        {
            bool Same(ProjectInvitation i) =>
                i.ProjectId == projectId && string.Equals(i.InvitedEmail, email, StringComparison.OrdinalIgnoreCase);

            // 1) Pending đã hết hạn → Expired (giống câu UPDATE trong transaction thật).
            for (int i = 0; i < Invitations.Count; i++)
                if (Same(Invitations[i]) && Invitations[i].Status == InvitationStatus.Pending && Invitations[i].ExpiresAt <= nowUtc)
                    Invitations[i] = Invitations[i] with { Status = InvitationStatus.Expired };

            // 2) Còn Pending còn hạn → unique index uq_pending_invitation vi phạm → null.
            if (Invitations.Any(i => Same(i) && i.Status == InvitationStatus.Pending))
                return Task.FromResult<ProjectInvitation?>(null);

            var created = new ProjectInvitation(
                Invitations.Count + 100, projectId, email, role, InvitationStatus.Pending,
                $"User {invitedBy}", nowUtc, expiresAtUtc, null);
            Invitations.Add(created);
            InvitedBy.Add(invitedBy);
            Tokens.Add(token);
            return Task.FromResult<ProjectInvitation?>(created);
        }

        public Task<IReadOnlyList<ProjectInvitation>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProjectInvitation>>(Invitations.Where(i => i.ProjectId == projectId).OrderBy(i => i.InvitationId).ToList());

        public Task<bool> CancelPendingAsync(long invitationId, DateTime respondedAtUtc, CancellationToken cancellationToken = default)
        {
            int index = Invitations.FindIndex(i => i.InvitationId == invitationId && i.Status == InvitationStatus.Pending);
            if (index < 0) return Task.FromResult(false);
            Invitations[index] = Invitations[index] with { Status = InvitationStatus.Cancelled, RespondedAt = respondedAtUtc };
            return Task.FromResult(true);
        }
    }
}
