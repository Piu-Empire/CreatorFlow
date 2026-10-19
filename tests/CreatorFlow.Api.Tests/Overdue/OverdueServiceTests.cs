using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Board;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Board;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Overdue;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Contracts.Overdue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Overdue;

/// <summary>
/// Overdue Service (SCRUM-47): quyền kiểm tra ở backend (đăng nhập + thành viên Project, mọi vai trò xem được),
/// lọc đúng Content quá hạn chưa hoàn thành, kèm Assignees/Platforms cho Dashboard/Notification.
/// "Hôm nay" cố định qua FixedTimeProvider để test biên theo ngày.
/// </summary>
[TestClass]
public sealed class OverdueServiceTests
{
    private const long ProjectId = 1;
    private const long CreatorId = 2;
    private const long OutsiderId = 9; // Creator của Project khác

    private static readonly DateOnly Today = new(2026, 10, 8);

    private static BoardCard Card(long contentId, string status, DateOnly? deadline, string title = "Content") =>
        new(contentId, ProjectId, $"CNT-{contentId:000}", title, string.Empty, status, "Medium",
            deadline, null, ["YouTube"], [], false);

    [TestMethod]
    public async Task GetOverdue_NotSignedIn_Returns401_AndReadsNothing()
    {
        var f = new Fixture(actingUserId: null).WithMember(CreatorId, ProjectRole.Creator);

        var result = await f.Service.GetOverdueAsync(ProjectId);

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual("unauthorized", result.ErrorCode);
        Assert.AreEqual(0, f.Cards.Reads, "chưa đăng nhập thì không được đọc dữ liệu");
    }

    [TestMethod]
    public async Task GetOverdue_NotProjectMember_Returns403_AndReadsNothing()
    {
        var f = new Fixture(OutsiderId).WithMember(CreatorId, ProjectRole.Creator);

        var result = await f.Service.GetOverdueAsync(ProjectId);

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual("forbidden", result.ErrorCode);
        Assert.AreEqual(0, f.Cards.Reads, "ngoài Project thì không được đọc dữ liệu");
    }

    [TestMethod]
    public async Task GetOverdue_PlainCreator_CanRead()
    {
        var f = new Fixture(CreatorId).WithMember(CreatorId, ProjectRole.Creator);

        var result = await f.Service.GetOverdueAsync(ProjectId);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(1, f.Cards.Reads);
    }

    [TestMethod]
    public async Task GetOverdue_KeepsOnlyUnfinishedPastDeadline_AndCarriesDashboardData()
    {
        var f = new Fixture(CreatorId).WithMember(CreatorId, ProjectRole.Creator)
            .WithCard(Card(10, "Script", Today.AddDays(-3), "Hook TikTok"))
            .WithCard(Card(11, "Published", Today.AddDays(-5)))   // đã xuất bản
            .WithCard(Card(12, "Archived", Today.AddDays(-5)))    // đã lưu trữ
            .WithCard(Card(13, "Editing", Today))                 // hạn đúng hôm nay
            .WithCard(Card(14, "Ready", Today.AddDays(2)))        // hạn tương lai
            .WithCard(Card(15, "Production", null));              // không có hạn
        f.Tasks.BoardAssignees.Add(new BoardAssignee(10, 7, "Creator One", AssignmentStatus.InProgress, 40, null));
        f.Tasks.BoardAssignees.Add(new BoardAssignee(11, 8, "Creator Two", AssignmentStatus.Completed, 100, null));

        var result = await f.Service.GetOverdueAsync(ProjectId);

        Assert.IsTrue(result.Succeeded, result.Message);
        var body = result.Value!;
        Assert.AreEqual(ProjectId, body.ProjectId);
        Assert.AreEqual(Today, body.AsOfDate);
        Assert.AreEqual(1, body.Total);
        OverdueContentResponse item = body.Items.Single();
        Assert.AreEqual(10, item.ContentId);
        Assert.AreEqual("CNT-010", item.ContentCode);
        Assert.AreEqual("Hook TikTok", item.ContentTitle);
        Assert.AreEqual("Script", item.Status);
        Assert.AreEqual(Today.AddDays(-3), item.Deadline);
        Assert.AreEqual(3, item.DaysLate);
        Assert.AreEqual(7, item.Assignees.Single().UserId);
        Assert.AreEqual("Creator One", item.Assignees.Single().DisplayName);
        CollectionAssert.AreEqual(new[] { "YouTube" }, item.Platforms.ToArray());
    }

    [TestMethod]
    public async Task GetOverdue_OrdersByDeadlineThenContentId()
    {
        var f = new Fixture(CreatorId).WithMember(CreatorId, ProjectRole.Creator)
            .WithCard(Card(20, "Script", Today.AddDays(-1)))
            .WithCard(Card(19, "Script", Today.AddDays(-1)))
            .WithCard(Card(21, "Script", Today.AddDays(-7)));

        var result = await f.Service.GetOverdueAsync(ProjectId);

        CollectionAssert.AreEqual(new long[] { 21, 19, 20 },
            result.Value!.Items.Select(i => i.ContentId).ToArray());
        Assert.AreEqual(7, result.Value!.Items[0].DaysLate);
    }

    [TestMethod]
    public async Task GetOverdue_NoUnfinishedLateContent_ReturnsEmpty()
    {
        var f = new Fixture(CreatorId).WithMember(CreatorId, ProjectRole.Creator)
            .WithCard(Card(10, "Published", Today.AddDays(-4)))
            .WithCard(Card(11, "Editing", Today.AddDays(4)));

        var result = await f.Service.GetOverdueAsync(ProjectId);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(0, result.Value!.Total);
        Assert.AreEqual(0, result.Value!.Items.Count);
    }

    // ================================================================== test double

    private sealed class Fixture(long? actingUserId)
    {
        private OverdueService? _service;

        public BoardFake Cards { get; } = new();
        public MemberRolesFake Tasks { get; } = new();

        public OverdueService Service => _service ??= new OverdueService(
            Cards,
            Tasks,
            new CurrentAuthenticatedUser
            {
                User = actingUserId is long id
                    ? new User { UserId = id, Email = $"u{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                    : null,
            },
            new FixedTimeProvider(Today));

        public Fixture WithMember(long userId, ProjectRole role)
        {
            Tasks.Roles[(ProjectId, userId)] = role;
            return this;
        }

        public Fixture WithCard(BoardCard card)
        {
            Cards.Rows.Add(card);
            return this;
        }
    }

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(new TimeOnly(9, 30)), TimeSpan.Zero);
    }

    private sealed class BoardFake : IBoardCardRepository
    {
        public List<BoardCard> Rows { get; } = new();
        public int Reads { get; private set; }

        public Task<IReadOnlyList<BoardCard>> GetCardsAsync(long projectId, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult<IReadOnlyList<BoardCard>>(Rows.Where(r => r.ProjectId == projectId).ToList());
        }

        public Task<IReadOnlyList<BoardCard>> GetProjectContentsAsync(long projectId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
