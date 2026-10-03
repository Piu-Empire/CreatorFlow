using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;

namespace CreatorFlow.Repositories.InMemory;

public class InMemoryReviewRepository : IReviewRepository
{
    public int GetLatestReviewNo(long contentId) =>
        InMemoryDataStore.Reviews.Where(r => r.ContentId == contentId)
            .Select(r => r.ReviewNo).DefaultIfEmpty(0).Max();

    public Review GetPendingReview(long contentId) =>
        InMemoryDataStore.Reviews
            .Where(r => r.ContentId == contentId && r.Status == ReviewStatus.Pending)
            .OrderByDescending(r => r.ReviewNo)
            .FirstOrDefault()!;

    public void Add(Review review)
    {
        review.Id = InMemoryDataStore.NextReviewId++;
        InMemoryDataStore.Reviews.Add(review);
    }

    public void Update(Review review)
    {
        var existing = InMemoryDataStore.Reviews.First(r => r.Id == review.Id);
        existing.Status = review.Status;
        existing.ReviewerUserId = review.ReviewerUserId;
        existing.Feedback = review.Feedback;
        existing.DecidedAt = review.DecidedAt;
    }
}
