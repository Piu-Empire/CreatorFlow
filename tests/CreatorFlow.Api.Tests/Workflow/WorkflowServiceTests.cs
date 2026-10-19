using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Notifications;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Models.Workflow;
using CreatorFlow.Api.Repositories.Workflow;
using CreatorFlow.Api.Services.Notifications;
using CreatorFlow.Api.Services.Workflow;
using CreatorFlow.Api.Tests.Notifications;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Contracts.Notifications;
using CreatorFlow.Contracts.Workflow;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Workflow;

/// <summary>
/// SCRUM-50: WorkflowService gửi thông báo đúng người — gửi duyệt báo Owner/Manager (trừ người gửi),
/// duyệt/từ chối báo người gửi Review; thông báo là hiệu ứng phụ, lỗi không được làm hỏng nghiệp vụ.
/// </summary>
[TestClass]
public sealed class WorkflowServiceTests
{
    private const long Actor = 1;     // người gửi duyệt
    private const long Owner = 2;
    private const long Manager = 3;
    private const long Submitter = 4; // người gửi Review trong test duyệt
    private const long Reviewer = 5;  // người duyệt (Owner)
    private const long ContentId = 10;
    private const long ProjectId = 1;

    // ================================================================== gửi duyệt

    [TestMethod]
    public async Task SubmitReview_NotifiesActiveOwnerAndManager_NotIncludingActor_WithReviewNo()
    {
        var f = new Fixture(Actor);
        f.Roles.Roles[(ProjectId, Actor)] = ProjectRole.Creator;
        f.Roles.Roles[(ProjectId, Owner)] = ProjectRole.Owner;
        f.Roles.Roles[(ProjectId, Manager)] = ProjectRole.Manager;

        var result = await f.Service.SubmitReviewAsync(ContentId, new SubmitReviewRequest("note"));

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEquivalent(new long[] { Owner, Manager },
            f.Notifications.Insets.Select(i => i.RecipientUserId).ToArray());
        Assert.IsTrue(f.Notifications.Insets.All(i => i.Type == NotificationType.ReviewSubmitted));
        CollectionAssert.AreEquivalent(new[] { "REVIEW_SUBMITTED:10:2:1", "REVIEW_SUBMITTED:10:3:1" },
            f.Notifications.Insets.Select(i => i.DedupeKey).ToArray(), "mỗi người nhận một khoá, số lần gửi duyệt");
    }

    [TestMethod]
    public async Task SubmitReview_NoOwnerOrManagerInProject_NobodyNotified()
    {
        var f = new Fixture(Actor); // chỉ có Actor (Creator)
        f.Roles.Roles[(ProjectId, Actor)] = ProjectRole.Creator;

        var result = await f.Service.SubmitReviewAsync(ContentId, new SubmitReviewRequest());

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, f.Notifications.Insets.Count);
    }

    [TestMethod]
    public async Task SubmitReview_SubmitterIsOwner_OtherOwnerStillNotified_AndActorNotNotified()
    {
        var f = new Fixture(Owner); // Owner tự gửi duyệt
        f.Roles.Roles[(ProjectId, Owner)] = ProjectRole.Owner;
        f.Roles.Roles[(ProjectId, Manager)] = ProjectRole.Manager;
        var result = await f.Service.SubmitReviewAsync(ContentId, new SubmitReviewRequest());

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new long[] { Manager }, f.Notifications.Insets.Select(i => i.RecipientUserId).ToArray());
    }

    // ================================================================== duyệt / từ chối

    [TestMethod]
    public async Task ApproveReview_NotifiesSubmitter_NotReviewer()
    {
        var f = new Fixture(Reviewer);
        f.Roles.Roles[(ProjectId, Reviewer)] = ProjectRole.Owner;
        _ = f.Service; // Build() thêm Content mẫu
        f.Repo.Reviews.Add(new FakeReview(ContentId, 1, Submitter));
        f.Repo.Contents[ContentId].Status = WorkflowContentStatus.Review;

        var result = await f.Service.DecideReviewAsync(ContentId, new ReviewDecisionRequest(true));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(WorkflowContentStatus.Ready, f.Repo.Contents[ContentId].Status);
        Assert.AreEqual(1, f.Notifications.Insets.Count);
        NotificationInsert row = f.Notifications.Insets[0];
        Assert.AreEqual(NotificationType.ReviewApproved, row.Type);
        Assert.AreEqual(Submitter, row.RecipientUserId);
        Assert.AreEqual("REVIEW_APPROVED:10:4:1", row.DedupeKey);
    }

    [TestMethod]
    public async Task RejectReview_MessageCarriesFeedback_AndNotifiesSubmitter()
    {
        var f = new Fixture(Reviewer);
        f.Roles.Roles[(ProjectId, Reviewer)] = ProjectRole.Owner;
        _ = f.Service; // Build() thêm Content mẫu
        f.Repo.Reviews.Add(new FakeReview(ContentId, 2, Submitter));
        f.Repo.Contents[ContentId].Status = WorkflowContentStatus.Review;

        var result = await f.Service.DecideReviewAsync(ContentId, new ReviewDecisionRequest(false, "Viết lại hook"));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(WorkflowContentStatus.Editing, f.Repo.Contents[ContentId].Status);
        NotificationInsert row = f.Notifications.Insets.Single();
        Assert.AreEqual(NotificationType.ReviewRejected, row.Type);
        StringAssert.Contains(row.Message, "Viết lại hook");
        Assert.AreEqual("REVIEW_REJECTED:10:4:2", row.DedupeKey);
    }

    [TestMethod]
    public async Task DecideReview_ReviewerIsSubmitter_NotNotified()
    {
        var f = new Fixture(Owner);
        f.Roles.Roles[(ProjectId, Owner)] = ProjectRole.Owner;
        _ = f.Service; // Build() thêm Content mẫu
        f.Repo.Reviews.Add(new FakeReview(ContentId, 1, Owner));
        f.Repo.Contents[ContentId].Status = WorkflowContentStatus.Review;

        var result = await f.Service.DecideReviewAsync(ContentId, new ReviewDecisionRequest(true));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, f.Notifications.Insets.Count);
    }

    // ================================================================== ngoại lệ

    [TestMethod]
    public async Task SubmitReview_MemberReadFails_SubmitStillSucceeds()
    {
        var f = new Fixture(Actor);
        f.Roles.Roles[(ProjectId, Actor)] = ProjectRole.Creator;
        f.Roles.ThrowOnMembers = true;

        var result = await f.Service.SubmitReviewAsync(ContentId, new SubmitReviewRequest());

        Assert.IsTrue(result.Succeeded, "đọc thành viên lỗi không được làm hỏng gửi duyệt");
        Assert.AreEqual(0, f.Notifications.Insets.Count);
    }

    [TestMethod]
    public async Task SubmitReview_NotMember_Still403_AndNoNotification()
    {
        var f = new Fixture(99);

        var result = await f.Service.SubmitReviewAsync(ContentId, new SubmitReviewRequest());

        Assert.AreEqual(403, result.Status);
        Assert.AreEqual(0, f.Notifications.Insets.Count);
    }

    // ================================================================== helpers

    private sealed class Fixture(long actingUserId)
    {
        public FakeWorkflowRepository Repo { get; } = new();
        public FakeNotificationRepository Notifications { get; } = new();
        public NotificationUserRepository Users { get; } = new();
        public TestEmailSender EmailSender { get; } = new();
        public MemberRolesFake Roles { get; } = new();
        public CurrentAuthenticatedUser Current => _current ??= new()
        {
            User = new User { UserId = actingUserId, Email = $"u{actingUserId}@x.vn", DisplayName = $"User {actingUserId}", PasswordHash = "x" },
        };
        private CurrentAuthenticatedUser? _current;

        private WorkflowService? _service;

        public WorkflowService Service => _service ??= Build();

        public WorkflowService Build()
        {
            // Đồng bộ vai trò từ MemberRolesFake sang FakeWorkflowRepository để kiểm tra quyền và gửi thông báo dùng cùng nguồn vai trò.
            foreach (var role in Roles.Roles)
                Repo.Roles[role.Key] = role.Value;

            if (!Repo.Contents.ContainsKey(ContentId))
                Repo.Contents[ContentId] = new FakeContent(ContentId, ProjectId, "CNT-010", "Video 10") { Status = WorkflowContentStatus.Editing };
            return new WorkflowService(Repo, Current, new NotificationService(
                Notifications, Roles, Users, EmailSender, Current,
                new FixedTimeProvider(new DateOnly(2026, 10, 8)), NullLogger<NotificationService>.Instance));
        }
    }

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(new TimeOnly(9, 30)), TimeSpan.Zero);
    }

    private sealed class FakeContent(long contentId, long projectId, string code, string title)
    {
        public long ContentId { get; } = contentId;
        public long ProjectId { get; } = projectId;
        public string Code { get; } = code;
        public string Title { get; } = title;
        public WorkflowContentStatus Status { get; set; }
    }

    private sealed class FakeReview(long contentId, int reviewNo, long submittedBy)
    {
        public long ContentId { get; } = contentId;
        public int ReviewNo { get; } = reviewNo;
        public long SubmittedBy { get; } = submittedBy;
        public string Status { get; set; } = "PENDING";
    }

    /// <summary>Repository giả lập transaction PENDING của WorkflowRepository: gửi duyệt thêm Review mới, quyết định Review mới nhất.</summary>
    private sealed class FakeWorkflowRepository : IWorkflowRepository
    {
        public Dictionary<long, FakeContent> Contents { get; } = new();
        public Dictionary<(long Project, long User), ProjectRole> Roles { get; } = new();
        public List<FakeReview> Reviews { get; } = new();

        public Task<WorkflowContent?> GetContentAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<WorkflowContent?>(Contents.TryGetValue(contentId, out var content)
                ? new WorkflowContent(contentId, content.ProjectId, content.Status, 0,
                    new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc))
                : null);

        public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken ct = default) =>
            Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out var role) ? role : null);

        public Task<WorkflowContent?> ChangeStatusAsync(long contentId, WorkflowContentStatus from, WorkflowContentStatus to,
            long changedBy, string? note, CancellationToken ct = default)
        {
            if (!Contents.TryGetValue(contentId, out var content) || content.Status != from)
                return Task.FromResult<WorkflowContent?>(null);
            content.Status = to;
            return Task.FromResult<WorkflowContent?>(ToWorkflow(content));
        }

        public Task<WorkflowContent?> SubmitReviewAsync(long contentId, long submittedBy, string? note, CancellationToken ct = default)
        {
            if (!Contents.TryGetValue(contentId, out var content) || content.Status != WorkflowContentStatus.Editing)
                return Task.FromResult<WorkflowContent?>(null);
            int reviewNo = Reviews.Count(r => r.ContentId == contentId) + 1;
            Reviews.Add(new FakeReview(contentId, reviewNo, submittedBy));
            content.Status = WorkflowContentStatus.Review;
            return Task.FromResult<WorkflowContent?>(ToWorkflow(content));
        }

        public Task<WorkflowContent?> DecideReviewAsync(long contentId, long reviewerId, bool approve, string? feedback,
            CancellationToken ct = default)
        {
            if (!Contents.TryGetValue(contentId, out var content) || content.Status != WorkflowContentStatus.Review)
                return Task.FromResult<WorkflowContent?>(null);
            FakeReview? review = Reviews
                .Where(r => r.ContentId == contentId && r.Status == "PENDING")
                .OrderByDescending(r => r.ReviewNo).FirstOrDefault();
            if (review is null) return Task.FromResult<WorkflowContent?>(null);
            review.Status = approve ? "APPROVED" : "REJECTED";
            content.Status = approve ? WorkflowContentStatus.Ready : WorkflowContentStatus.Editing;
            return Task.FromResult<WorkflowContent?>(ToWorkflow(content));
        }

        public Task<IReadOnlyList<StatusHistoryEntry>> GetStatusHistoryAsync(long contentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<StatusHistoryEntry>>(Array.Empty<StatusHistoryEntry>());

        public Task<PendingReviewInfo?> GetPendingReviewInfoAsync(long contentId, CancellationToken ct = default)
        {
            FakeReview? review = Reviews
                .Where(r => r.ContentId == contentId && r.Status == "PENDING")
                .OrderByDescending(r => r.ReviewNo).FirstOrDefault();
            if (review is null || !Contents.TryGetValue(contentId, out var content)) return Task.FromResult<PendingReviewInfo?>(null);
            return Task.FromResult<PendingReviewInfo?>(new PendingReviewInfo(
                contentId, content.ProjectId, content.Code, content.Title, review.ReviewNo, review.SubmittedBy));
        }

        private static WorkflowContent ToWorkflow(FakeContent content) =>
            content is { } c
                ? new WorkflowContent(c.ContentId, c.ProjectId, c.Status, 0, default)
                : throw new InvalidOperationException();
    }
}

