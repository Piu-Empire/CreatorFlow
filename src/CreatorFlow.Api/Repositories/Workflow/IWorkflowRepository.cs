using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Models.Workflow;

namespace CreatorFlow.Api.Repositories.Workflow;

public interface IWorkflowRepository
{
    Task<WorkflowContent?> GetContentAsync(long contentId, CancellationToken cancellationToken = default);
    Task<ProjectRole?> GetRoleAsync(long projectId, long userId, CancellationToken cancellationToken = default);

    /// <summary>Chuyển bước đi thẳng; trả về null nếu Content không còn ở trạng thái gốc (race).</summary>
    Task<WorkflowContent?> ChangeStatusAsync(
        long contentId, WorkflowContentStatus from, WorkflowContentStatus to,
        long changedBy, string? note, CancellationToken cancellationToken = default);

    /// <summary>EDITING→REVIEW: tạo Review mới (không ghi đè) + đổi trạng thái + ghi lịch sử, trong 1 transaction.</summary>
    Task<WorkflowContent?> SubmitReviewAsync(
        long contentId, long submittedBy, string? note, CancellationToken cancellationToken = default);

    /// <summary>Duyệt/từ chối Review đang PENDING + đổi trạng thái + ghi lịch sử, trong 1 transaction.</summary>
    Task<WorkflowContent?> DecideReviewAsync(
        long contentId, long reviewerId, bool approve, string? feedback,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StatusHistoryEntry>> GetStatusHistoryAsync(long contentId, CancellationToken cancellationToken = default);
}
