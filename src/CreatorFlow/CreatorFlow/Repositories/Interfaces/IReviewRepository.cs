using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IReviewRepository
{
    /// <summary>Lấy ReviewNo lớn nhất đã dùng cho Content này (0 nếu chưa có Review nào).</summary>
    int GetLatestReviewNo(long contentId);

    /// <summary>Lấy Review đang PENDING gần nhất của Content (dùng khi Approve/Reject).</summary>
    Review GetPendingReview(long contentId);

    void Add(Review review);

    void Update(Review review);
}