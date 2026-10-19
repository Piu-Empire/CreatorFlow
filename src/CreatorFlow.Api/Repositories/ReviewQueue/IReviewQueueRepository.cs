using CreatorFlow.Api.Models.ReviewQueue;

namespace CreatorFlow.Api.Repositories.ReviewQueue;

/// <summary>Đọc danh sách Review đang chờ duyệt của một Project.</summary>
public interface IReviewQueueRepository
{
    /// <summary>Review PENDING của Project, cũ nhất trước (theo submitted_at).</summary>
    Task<IReadOnlyList<PendingReviewRecord>> GetPendingAsync(
        long projectId, CancellationToken cancellationToken = default);
}
