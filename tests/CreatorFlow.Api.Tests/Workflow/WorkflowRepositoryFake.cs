using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Models.Workflow;
using CreatorFlow.Api.Repositories.Workflow;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Services.Notifications;
using CreatorFlow.Api.Services.Workflow;
using CreatorFlow.Api.Tests.Notifications;
using CreatorFlow.Api.Tests.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CreatorFlow.Api.Tests.Workflow;

internal sealed class WorkflowRepositoryFake : IWorkflowRepository
{
    public static NotificationService CreateNotifications(CurrentAuthenticatedUser current) => new(
        new FakeNotificationRepository(), new MemberRolesFake(), new NotificationUserRepository(),
        new TestEmailSender(), current, TimeProvider.System, NullLogger<NotificationService>.Instance);

    public WorkflowService CreateService(CurrentAuthenticatedUser current) => new(this, current, CreateNotifications(current));

    public Dictionary<long, WorkflowContent> Contents { get; } = new()
    {
        [10] = new(10, 8, WorkflowContentStatus.Review, 55, DateTime.UtcNow),
        [20] = new(20, 9, WorkflowContentStatus.Review, 55, DateTime.UtcNow)
    };
    public Dictionary<(long Project, long User), ProjectRole> Roles { get; } = new();
    public List<(long Project, long User)> RoleReads { get; } = new();
    public List<(long Content, long Actor)> Writes { get; } = new();

    public Task<WorkflowContent?> GetContentAsync(long contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Contents.GetValueOrDefault(contentId));

    public Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default)
    {
        RoleReads.Add((projectId, userId));
        return Task.FromResult<ProjectRole?>(Roles.TryGetValue((projectId, userId), out var role) ? role : null);
    }

    private Task<WorkflowContent?> Save(long contentId, long actor, WorkflowContentStatus target)
    {
        Writes.Add((contentId, actor));
        var content = Contents[contentId] with { Status = target };
        Contents[contentId] = content;
        return Task.FromResult<WorkflowContent?>(content);
    }

    public Task<WorkflowContent?> ChangeStatusAsync(long contentId, WorkflowContentStatus from, WorkflowContentStatus to,
        long changedBy, string? note, CancellationToken cancellationToken = default) => Save(contentId, changedBy, to);
    public Task<WorkflowContent?> SubmitReviewAsync(long contentId, long submittedBy, string? note,
        CancellationToken cancellationToken = default) => Save(contentId, submittedBy, WorkflowContentStatus.Review);
    public Task<WorkflowContent?> DecideReviewAsync(long contentId, long reviewerId, bool approve, string? feedback,
        CancellationToken cancellationToken = default) => Save(contentId, reviewerId, approve ? WorkflowContentStatus.Ready : WorkflowContentStatus.Editing);
    public Task<IReadOnlyList<StatusHistoryEntry>> GetStatusHistoryAsync(long contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StatusHistoryEntry>>([]);

    // Fixture phân quyền không mô phỏng Review pending; notification được kiểm chứng ở bộ test riêng.
    public Task<PendingReviewInfo?> GetPendingReviewInfoAsync(long contentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PendingReviewInfo?>(null);
}
