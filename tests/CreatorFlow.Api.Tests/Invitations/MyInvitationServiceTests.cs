using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Invitations;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Invitations;
using CreatorFlow.Api.Services.Invitations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Invitations;

/// <summary>
/// Người được mời xem lời mời và chấp nhận / từ chối. Quy tắc được thực thi Ở BACKEND:
///   - Chấp nhận tạo thành viên Project ĐÚNG MỘT LẦN; Từ chối không tạo thành viên;
///   - trạng thái lời mời được cập nhật (Accepted / Rejected) và không trả lời lại được;
///   - chỉ lời mời gửi tới email của chính mình, chỉ khi còn Pending và còn hạn.
/// </summary>
[TestClass]
public sealed class MyInvitationServiceTests
{
    private const long ProjectId = 1;
    private const long InviteeId = 5;
    private const string InviteeEmail = "invitee@x.vn";
    private const string OtherEmail = "someone-else@x.vn";

    private const long MineId = 10;
    private const long OtherPersonsId = 11;

    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 30, 0, TimeSpan.Zero);

    // ================================================================== xem lời mời

    [TestMethod]
    public async Task GetMine_NotSignedIn_Returns401()
    {
        var f = new Fixture(signedIn: false);

        var result = await f.Service.GetMineAsync();

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual("unauthorized", result.ErrorCode);
    }

    [TestMethod]
    public async Task GetMine_ReturnsOnlyMyInvitations_NewestFirst_AndStalePendingShownAsExpired()
    {
        var f = new Fixture();
        f.Repo.Seed(MineId, InviteeEmail, InvitationStatus.Pending, createdAt: Now.UtcDateTime.AddDays(-3));
        f.Repo.Seed(12, InviteeEmail.ToUpperInvariant(), InvitationStatus.Pending, createdAt: Now.UtcDateTime.AddDays(-9), expiresAt: Now.UtcDateTime.AddDays(-2));
        f.Repo.Seed(OtherPersonsId, OtherEmail, InvitationStatus.Pending);

        var result = await f.Service.GetMineAsync();

        Assert.IsTrue(result.Succeeded);
        var rows = result.Value!.Invitations;
        CollectionAssert.AreEqual(new long[] { MineId, 12 }, rows.Select(r => r.InvitationId).ToArray());
        Assert.AreEqual("Pending", rows[0].Status);
        Assert.AreEqual("Expired", rows[1].Status); // DB còn ghi Pending nhưng đã quá hạn
        Assert.AreEqual("Project 1", rows[0].ProjectName);
    }

    // ================================================================== chấp nhận

    [TestMethod]
    public async Task Accept_NotSignedIn_Returns401AndChangesNothing()
    {
        var f = new Fixture(signedIn: false);
        f.Repo.Seed(MineId, InviteeEmail, InvitationStatus.Pending);

        var result = await f.Service.AcceptAsync(MineId);

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual(0, f.Repo.Members.Count);
        Assert.AreEqual(InvitationStatus.Pending, f.Repo.Invitations[0].Status);
    }

    [TestMethod]
    public async Task Accept_Pending_CreatesMembershipAndMarksAccepted()
    {
        var f = new Fixture();
        f.Repo.Seed(MineId, InviteeEmail, InvitationStatus.Pending);

        var result = await f.Service.AcceptAsync(MineId);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(200, result.Status);
        Assert.AreEqual("Accepted", result.Value!.Status);
        Assert.IsNotNull(result.Value.RespondedAt);
        Assert.AreEqual(1, f.Repo.Members.Count);
        Assert.IsTrue(f.Repo.Members.Contains((ProjectId, InviteeId)));
    }

    [TestMethod]
    public async Task Accept_Twice_SecondReturns409_AndMembershipIsCreatedExactlyOnce()
    {
        var f = new Fixture();
        f.Repo.Seed(MineId, InviteeEmail, InvitationStatus.Pending);

        var first = await f.Service.AcceptAsync(MineId);
        var second = await f.Service.AcceptAsync(MineId);

        Assert.IsTrue(first.Succeeded);
        Assert.AreEqual(409, second.Status);
        Assert.AreEqual("invitation_not_pending", second.ErrorCode);
        Assert.AreEqual(1, f.Repo.Members.Count);
        Assert.AreEqual(1, f.Repo.MembershipWrites);
    }

    // ================================================================== từ chối

    [TestMethod]
    public async Task Reject_Pending_MarksRejected_AndCreatesNoMembership()
    {
        var f = new Fixture();
        f.Repo.Seed(MineId, InviteeEmail, InvitationStatus.Pending);

        var result = await f.Service.RejectAsync(MineId);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Rejected", result.Value!.Status);
        Assert.IsNotNull(result.Value.RespondedAt);
        Assert.AreEqual(0, f.Repo.Members.Count);
    }

    [TestMethod]
    public async Task Reject_ThenAccept_Returns409_AndStillNoMembership()
    {
        var f = new Fixture();
        f.Repo.Seed(MineId, InviteeEmail, InvitationStatus.Pending);

        await f.Service.RejectAsync(MineId);
        var accept = await f.Service.AcceptAsync(MineId);

        Assert.AreEqual(409, accept.Status);
        Assert.AreEqual("invitation_not_pending", accept.ErrorCode);
        Assert.AreEqual(0, f.Repo.Members.Count);
        Assert.AreEqual(InvitationStatus.Rejected, f.Repo.Invitations[0].Status);
    }

    // ================================================================== không được phép / không còn hiệu lực

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Decide_InvitationForAnotherEmail_Returns404_AndChangesNothing(bool accept)
    {
        var f = new Fixture();
        f.Repo.Seed(OtherPersonsId, OtherEmail, InvitationStatus.Pending);

        var result = accept ? await f.Service.AcceptAsync(OtherPersonsId) : await f.Service.RejectAsync(OtherPersonsId);

        Assert.AreEqual(404, result.Status);
        Assert.AreEqual("not_found", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.Members.Count);
        Assert.AreEqual(InvitationStatus.Pending, f.Repo.Invitations[0].Status);
    }

    [TestMethod]
    public async Task Decide_UnknownInvitation_Returns404()
    {
        var f = new Fixture();

        var result = await f.Service.AcceptAsync(999);

        Assert.AreEqual(404, result.Status);
        Assert.AreEqual(0, f.Repo.Members.Count);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Decide_PendingButExpired_Returns409Expired_AndCreatesNoMembership(bool accept)
    {
        var f = new Fixture();
        f.Repo.Seed(MineId, InviteeEmail, InvitationStatus.Pending, expiresAt: Now.UtcDateTime.AddMinutes(-1));

        var result = accept ? await f.Service.AcceptAsync(MineId) : await f.Service.RejectAsync(MineId);

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("invitation_expired", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.Members.Count);
        Assert.AreEqual(InvitationStatus.Expired, f.Repo.Invitations[0].Status);
    }

    [TestMethod]
    [DataRow(InvitationStatus.Accepted)]
    [DataRow(InvitationStatus.Rejected)]
    [DataRow(InvitationStatus.Cancelled)]
    [DataRow(InvitationStatus.Expired)]
    public async Task Accept_NotPending_Returns409_AndCreatesNoMembership(InvitationStatus status)
    {
        var f = new Fixture();
        f.Repo.Seed(MineId, InviteeEmail, status);

        var result = await f.Service.AcceptAsync(MineId);

        Assert.AreEqual(409, result.Status);
        Assert.AreEqual("invitation_not_pending", result.ErrorCode);
        Assert.AreEqual(0, f.Repo.Members.Count);
        Assert.AreEqual(status, f.Repo.Invitations[0].Status);
    }

    // ================================================================== quy tắc thuần

    [TestMethod]
    public void Rules_StatusFor_MapsDecisionToStatus()
    {
        Assert.AreEqual(InvitationStatus.Accepted, InvitationRules.StatusFor(accept: true));
        Assert.AreEqual(InvitationStatus.Rejected, InvitationRules.StatusFor(accept: false));
    }

    // ================================================================== test double

    private sealed class Fixture
    {
        private readonly bool _signedIn;
        private MyInvitationService? _service;
        private FakeMyInvitationRepository? _repo;

        public Fixture(bool signedIn = true) => _signedIn = signedIn;

        public FakeMyInvitationRepository Repo => _repo ??= new FakeMyInvitationRepository();

        public MyInvitationService Service => _service ??= new MyInvitationService(
            Repo,
            new CurrentAuthenticatedUser
            {
                User = _signedIn
                    ? new User { UserId = InviteeId, Email = InviteeEmail, DisplayName = "Invitee", PasswordHash = "x" }
                    : null,
            },
            new FixedTimeProvider());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>
    /// Repository giả lập đúng ngữ nghĩa transaction thật: khớp theo email (không phân biệt hoa/thường), NotFound / NotPending / Expired /
    /// Decided, và thành viên là tập (project, user) nên thêm lần hai không tạo dòng mới (giống UNIQUE(project_id, user_id)).
    /// </summary>
    private sealed class FakeMyInvitationRepository : IMyInvitationRepository
    {
        private readonly Dictionary<long, string> _emails = new();

        public List<MyInvitation> Invitations { get; } = new();
        public HashSet<(long Project, long User)> Members { get; } = new();
        public int MembershipWrites { get; private set; }

        public void Seed(long id, string email, InvitationStatus status, DateTime? createdAt = null, DateTime? expiresAt = null)
        {
            _emails[id] = email;
            Invitations.Add(new MyInvitation(
                id, ProjectId, $"Project {ProjectId}", ProjectRole.Creator, status, "Owner",
                createdAt ?? Now.UtcDateTime.AddDays(-1), expiresAt ?? Now.UtcDateTime.AddDays(6), null));
        }

        public Task<IReadOnlyList<MyInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MyInvitation>>(Invitations
                .Where(i => string.Equals(_emails[i.InvitationId], email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.InvitationId).ToList());

        public Task<InvitationDecisionResult> DecideAsync(
            long invitationId, long userId, string email, InvitationStatus decision, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            int index = Invitations.FindIndex(i => i.InvitationId == invitationId
                && string.Equals(_emails[i.InvitationId], email, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return Task.FromResult(new InvitationDecisionResult(InvitationDecisionOutcome.NotFound));

            MyInvitation current = Invitations[index];
            if (current.Status != InvitationStatus.Pending)
                return Task.FromResult(new InvitationDecisionResult(InvitationDecisionOutcome.NotPending, CurrentStatus: current.Status));

            if (current.ExpiresAt <= nowUtc)
            {
                Invitations[index] = current with { Status = InvitationStatus.Expired };
                return Task.FromResult(new InvitationDecisionResult(InvitationDecisionOutcome.Expired));
            }

            if (decision == InvitationStatus.Accepted && Members.Add((current.ProjectId, userId))) MembershipWrites++;

            MyInvitation decided = current with { Status = decision, RespondedAt = nowUtc };
            Invitations[index] = decided;
            return Task.FromResult(new InvitationDecisionResult(InvitationDecisionOutcome.Decided, decided));
        }
    }
}
