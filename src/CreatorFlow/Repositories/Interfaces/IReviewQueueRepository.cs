using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IReviewQueueRepository
{
    /// <summary>Lấy toàn bộ Content đang có Review PENDING trong Project (Status = REVIEW).</summary>
    List<ReviewQueueItem> GetPendingReviews(long projectId);
}