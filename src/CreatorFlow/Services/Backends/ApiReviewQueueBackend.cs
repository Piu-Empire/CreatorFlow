using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Workflow;
using CreatorFlow.Models;

namespace CreatorFlow.Services.Backends;

/// <summary>Review Queue chạy qua CreatorFlow.Api (quyền và quyết định duyệt do Backend kiểm tra).</summary>
public sealed class ApiReviewQueueBackend(ApiClient api, UserSession session) : IReviewQueueBackend
{
    public List<ReviewQueueItem> GetPending(long projectId) =>
        ApiTaskCall.Run(() => api.GetPendingReviewsAsync(projectId, ApiTaskCall.BearerOf(session)), session).Items
            .Select(i => new ReviewQueueItem
            {
                ContentId = i.ContentId,
                ProjectId = i.ProjectId,
                ContentCode = i.ContentCode,
                ContentTitle = i.ContentTitle,
                SubmittedByName = i.SubmittedByName,
                SubmittedAt = i.SubmittedAt.ToLocalTime(),
                ReviewNo = i.ReviewNo,
            })
            .ToList();

    public void Decide(long contentId, bool approve, string? feedback) =>
        ApiTaskCall.Run(() => api.DecideReviewAsync(contentId, new ReviewDecisionRequest(approve, feedback), ApiTaskCall.BearerOf(session)), session);
}
