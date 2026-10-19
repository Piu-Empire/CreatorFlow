using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Notifications;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Services.Notifications;
using CreatorFlow.Api.Services.Tasks;
using CreatorFlow.Api.Tests.Notifications;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Contracts.Notifications;
using CreatorFlow.Contracts.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Notifications;

/// <summary>
/// SCRUM-50: thông báo đúng người (chỉ người trực tiếp liên quan, không báo chính người thao tác),
/// không tạo trùng vô hạn (dedupe_key), deadline sinh lười khi đọc theo đúng ngưỡng 2 ngày,
/// email là hiệu ứng phụ chỉ gửi khi bản ghi thực sự được tạo.
/// </summary>
[TestClass]
public sealed class NotificationServiceTests
{
    private const long Actor = 1;
    private const long Creator = 2;
    private const long OtherCreator = 5;
    private const long Manager = 3;
    private const long Owner = 4;
    private const long ContentId = 10;
    private const long ProjectId = 1;

    private static readonly DateOnly Today = new(2026, 10, 8);

    private sealed class Fixture(long? actingUserId)
    {
        public FakeNotificationRepository Repo { get; } = new();
        public NotificationUserRepository Users { get; } = new();
        public TestEmailSender EmailSender { get; } = new();
        public MemberRolesFake Roles { get; } = new();

        public NotificationService Build() => new(
            Repo, Roles, Users, EmailSender, CurrentUser,
            new FixedTimeProvider(Today), NullLogger<NotificationService>.Instance);

        public CurrentAuthenticatedUser CurrentUser { get; } = new()
        {
            User = actingUserId is long id
                ? new User { UserId = id, Email = $"u{id}@x.vn", DisplayName = $"User {id}", PasswordHash = "x" }
                : null,
        };
    }

    private static NotificationService Create(Fixture fixture) => fixture.Build();

    // ================================================================== đọc / đánh dấu đọc

    [TestMethod]
    public async Task GetMine_NotSignedIn_Returns401()
    {
        var service = Create(new Fixture(null));

        var result = await service.GetMineAsync(unreadOnly: false);

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual("unauthorized", result.ErrorCode);
    }

    [TestMethod]
    public async Task GetMine_ReturnsOnlyOwnNotifications_LatestFirst_WithUnreadCount()
    {
        var f = new Fixture(Creator);
        f.Repo.Seed(Creator, NotificationType.Assignment, "Mới", "nội dung mới",
            createdAt: new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc));
        f.Repo.Seed(Creator, NotificationType.Overdue, "Trễ hạn", "nội dung trễ",
            isRead: true, createdAt: new DateTime(2026, 10, 9, 7, 0, 0, DateTimeKind.Utc));
        f.Repo.Seed(Owner, NotificationType.Assignment, "Của Owner", "không được thấy");

        var result = await Create(f).GetMineAsync(unreadOnly: false);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(1, result.Value!.UnreadCount);
        Assert.AreEqual(2, result.Value.Items.Count);
        Assert.AreEqual("Mới", result.Value.Items[0].Title);
        Assert.AreEqual("ASSIGNMENT", result.Value.Items[0].Type);
        Assert.AreEqual("Trễ hạn", result.Value.Items[1].Title);
        Assert.IsTrue(result.Value.Items[1].IsRead);
    }

    [TestMethod]
    public async Task GetMine_UnreadOnly_FiltersReadNotifications()
    {
        var f = new Fixture(Creator);
        f.Repo.Seed(Creator, NotificationType.Assignment, "Đã đọc", "x", isRead: true);
        f.Repo.Seed(Creator, NotificationType.Overdue, "Chưa đọc", "x");

        var result = await Create(f).GetMineAsync(unreadOnly: true);

        Assert.AreEqual(1, result.Value!.Items.Count);
        Assert.AreEqual("Chưa đọc", result.Value.Items[0].Title);
    }

    [TestMethod]
    public async Task MarkRead_OnlyOwnRow_OtherPeopleRowIs404_AlreadyReadIsIdempotent()
    {
        var f = new Fixture(Creator);
        f.Repo.Seed(Creator, NotificationType.Assignment, "Của tôi", "x");
        f.Repo.Seed(Owner, NotificationType.Assignment, "Của Owner", "x");

        var service = Create(f);
        var mine = await service.GetMineAsync(unreadOnly: false);
        long ownId = mine.Value!.Items.First().NotificationId;

        var marked = await service.MarkReadAsync(ownId);
        Assert.IsTrue(marked.Succeeded);
        Assert.IsTrue(marked.Value!.IsRead);

        var again = await service.MarkReadAsync(ownId);
        Assert.IsTrue(again.Succeeded, "đánh dấu lại không được lỗi");

        var ownerService = new NotificationService(f.Repo, f.Roles, f.Users, f.EmailSender,
            new CurrentAuthenticatedUser
            {
                User = new User { UserId = Owner, Email = "owner@x.vn", DisplayName = "Owner", PasswordHash = "x" },
            },
            new FixedTimeProvider(Today), NullLogger<NotificationService>.Instance);
        var otherId = (await ownerService.GetMineAsync(unreadOnly: false)).Value!.Items.Single().NotificationId;
        Assert.AreEqual(404, (await Create(f).MarkReadAsync(otherId)).Status, "không được đọc thông báo của người khác");
        Assert.AreEqual(404, (await Create(f).MarkReadAsync(9999)).Status);
        Assert.AreEqual(2, f.Repo.MarkReadCalls.Count, "hai lần đánh dấu của chính mình đều ghi; của người khác thì không");
    }

    [TestMethod]
    public async Task MarkAllRead_MarksOnlyOwnUnreadRows()
    {
        var f = new Fixture(Creator);
        f.Repo.Seed(Creator, NotificationType.Assignment, "1", "x");
        f.Repo.Seed(Creator, NotificationType.Overdue, "2", "x", isRead: true);
        f.Repo.Seed(Owner, NotificationType.Assignment, "Của Owner", "x");

        var result = await Create(f).MarkAllReadAsync();

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(1, result.Value!.MarkedCount);
        Assert.AreEqual(1, f.Repo.MarkAllReadCalls);
    }

    // ================================================================== giao việc

    [TestMethod]
    public async Task NotifyAssignment_NotifiesAssignee_WithStableDedupeKey()
    {
        var f = new Fixture(Actor);
        f.Users.Emails[Creator] = "creator@x.vn";
        var service = Create(f);

        await service.NotifyAssignmentAsync(ContentId, ProjectId, "CNT-010", "Video 1", assignmentId: 55,
            recipientUserId: Creator, actorUserId: Actor);
        await service.NotifyAssignmentAsync(ContentId, ProjectId, "CNT-010", "Video 1", assignmentId: 55,
            recipientUserId: Creator, actorUserId: Actor);

        Assert.AreEqual(1, f.Repo.Insets.Count, "cùng dedupe_key không được tạo lần hai");
        NotificationInsert row = f.Repo.Insets[0];
        Assert.AreEqual(NotificationType.Assignment, row.Type);
        Assert.AreEqual(Creator, row.RecipientUserId);
        Assert.AreEqual("ASSIGNMENT:10:2:55", row.DedupeKey);
        Assert.AreEqual(1, f.EmailSender.Notifications.Count, "email chỉ gửi khi bản ghi được tạo thật");
        Assert.AreEqual("creator@x.vn", f.EmailSender.Notifications[0].Email);
    }

    [TestMethod]
    public async Task NotifyAssignment_ActorIsRecipient_NotNotified()
    {
        var f = new Fixture(Creator);

        await Create(f).NotifyAssignmentAsync(ContentId, ProjectId, "CNT-010", "Video 1", 55, Creator, Creator);

        Assert.AreEqual(0, f.Repo.Insets.Count);
        Assert.AreEqual(0, f.EmailSender.Notifications.Count);
    }

    // ================================================================== gửi duyệt / kết quả duyệt

    [TestMethod]
    public async Task NotifyReviewSubmitted_ActiveOwnersAndManagers_NotIncludingActor_OncePerRecipient()
    {
        var f = new Fixture(Creator);
        f.Roles.Roles[(ProjectId, Owner)] = ProjectRole.Owner;
        f.Roles.Roles[(ProjectId, Manager)] = ProjectRole.Manager;
        f.Roles.Roles[(ProjectId, OtherCreator)] = ProjectRole.Creator;
        f.Users.Emails[Owner] = "owner@x.vn";
        f.Users.Emails[Manager] = "manager@x.vn";

        await Create(f).NotifyReviewSubmittedAsync(ContentId, ProjectId, "CNT-010", "Video 1", reviewNo: 3, actorUserId: Creator);

        CollectionAssert.AreEquivalent(new long[] { Owner, Manager }, f.Repo.Insets.Select(i => i.RecipientUserId).ToArray());
        Assert.IsTrue(f.Repo.Insets.All(i => i.Type == NotificationType.ReviewSubmitted));
        CollectionAssert.AreEquivalent(
            new[] { "REVIEW_SUBMITTED:10:4:3", "REVIEW_SUBMITTED:10:3:3" },
            f.Repo.Insets.Select(i => i.DedupeKey).ToArray());
        Assert.AreEqual(2, f.EmailSender.Notifications.Count);
    }

    [TestMethod]
    public async Task NotifyReviewDecided_NotifiesSubmitter_RejectCarriesFeedback()
    {
        var f = new Fixture(Manager);
        f.Users.Emails[Creator] = "creator@x.vn";

        await Create(f).NotifyReviewDecidedAsync(ContentId, ProjectId, "CNT-010", "Video 1", 3,
            submitterUserId: Creator, approved: false, feedback: "Viết lại hook", actorUserId: Manager);

        Assert.AreEqual(1, f.Repo.Insets.Count);
        NotificationInsert row = f.Repo.Insets[0];
        Assert.AreEqual(NotificationType.ReviewRejected, row.Type);
        Assert.AreEqual(Creator, row.RecipientUserId);
        StringAssert.Contains(row.Message, "Viết lại hook");
        Assert.AreEqual(1, f.EmailSender.Notifications.Count);
    }

    [TestMethod]
    public async Task NotifyReviewDecided_ApproverIsSubmitter_NotNotified()
    {
        var f = new Fixture(Owner);

        await Create(f).NotifyReviewDecidedAsync(ContentId, ProjectId, "CNT-010", "Video 1", 1,
            submitterUserId: Owner, approved: true, feedback: null, actorUserId: Owner);

        Assert.AreEqual(0, f.Repo.Insets.Count);
        Assert.AreEqual(0, f.EmailSender.Notifications.Count);
    }

    // ================================================================== deadline (sinh lười khi đọc)

    [TestMethod]
    public async Task GetMine_EnsureDeadline_CreatesOverdueAndNearOnce_PerDay()
    {
        var f = new Fixture(Creator);
        f.Repo.DueAssignments.Add((new DueAssignment(10, ProjectId, "CNT-010", "Trễ", Today.AddDays(-1)), Creator));
        f.Repo.DueAssignments.Add((new DueAssignment(11, ProjectId, "CNT-011", "Sắp tới", Today.AddDays(2)), Creator));
        f.Repo.DueAssignments.Add((new DueAssignment(12, ProjectId, "CNT-012", "Chưa tới", Today.AddDays(3)), Creator));

        var service = Create(f);
        await service.GetMineAsync(unreadOnly: false);
        await service.GetMineAsync(unreadOnly: false);

        Assert.AreEqual(2, f.Repo.Insets.Count, "chỉ trễ hạn và tới hạn trong 2 ngày mới có thông báo");
        CollectionAssert.AreEquivalent(
            new[] { "OVERDUE:10:2:2026-10-08", "DEADLINE_NEAR:11:2:2026-10-08" },
            f.Repo.Insets.Select(i => i.DedupeKey).ToArray());
        Assert.AreEqual(0, f.EmailSender.Notifications.Count, "deadline chỉ in-app");
    }

    [TestMethod]
    public async Task GetMine_EnsureDeadline_SameDay_DoesNotDuplicateReminder()
    {
        var f = new Fixture(Creator);
        f.Repo.DueAssignments.Add((new DueAssignment(10, ProjectId, "CNT-010", "Trễ", Today.AddDays(-1)), Creator));

        await Create(f).GetMineAsync(unreadOnly: false);
        await Create(f).GetMineAsync(unreadOnly: false);

        Assert.AreEqual(1, f.Repo.Insets.Count, "cùng ngày không nhắc lại");
    }

    [TestMethod]
    public async Task GetMine_DeadlineSweepFailure_DoesNotBreakList()
    {
        var broken = new BrokenNotificationRepository();
        broken.Seed(Creator, NotificationType.Assignment, "Sẵn có", "x");

        var result = await new NotificationService(
            broken, new MemberRolesFake(), new NotificationUserRepository(), new TestEmailSender(),
            new CurrentAuthenticatedUser
            {
                User = new User { UserId = Creator, Email = "creator@x.vn", DisplayName = "Creator", PasswordHash = "x" },
            },
            new FixedTimeProvider(Today), NullLogger<NotificationService>.Instance)
            .GetMineAsync(unreadOnly: false);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(1, result.Value!.Items.Count, "lỗi quét deadline không được làm hỏng đọc danh sách");
    }

    // ================================================================== email

    [TestMethod]
    public async Task Notification_EmailSkipped_WhenSenderNotConfigured()
    {
        var f = new Fixture(Actor);
        f.Users.Emails[Creator] = "creator@x.vn";
        f.EmailSender.Configured = false;

        await Create(f).NotifyAssignmentAsync(ContentId, ProjectId, "CNT-010", "Video 1", 55, Creator, Actor);

        Assert.AreEqual(1, f.Repo.Insets.Count, "in-app vẫn ghi bình thường");
        Assert.AreEqual(0, f.EmailSender.Notifications.Count);
    }

    [TestMethod]
    public async Task Notification_NeverBreaksBusinessFlow_WhenEmailFails()
    {
        var f = new Fixture(Actor);
        f.Users.Emails[Creator] = "creator@x.vn";
        f.EmailSender.Fail = true;

        await Create(f).NotifyAssignmentAsync(ContentId, ProjectId, "CNT-010", "Video 1", 55, Creator, Actor);

        Assert.AreEqual(1, f.Repo.Insets.Count);
    }

    // ================================================================== helpers

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(new TimeOnly(9, 30)), TimeSpan.Zero);
    }

    /// <summary>Repository giả ném khi quét deadline để kiểm tra đọc danh sách không bị vỡ.</summary>
    private sealed class BrokenNotificationRepository : FakeNotificationRepository
    {
        public new Task<IReadOnlyList<DueAssignment>> GetDueAssignmentsForUserAsync(
            long userId, DateOnly dueOn, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("db down");
    }
}
