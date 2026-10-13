using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Board;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services.Backends;

/// <summary>Board chạy qua CreatorFlow.Api: client chỉ gọi REST bằng token của phiên đăng nhập và đổi DTO sang thẻ hiển thị.</summary>
public sealed class ApiBoardBackend(ApiClient api, UserSession session) : IBoardBackend
{
    public async Task<List<ContentBoardCard>> GetBoardCardsAsync(long projectId, CancellationToken token = default)
    {
        string bearer = ApiTaskCall.BearerOf(session);
        var response = await ApiTaskCall.RunAsync(() => api.GetBoardAsync(projectId, bearer, token), session);
        return response.Cards.Select(ToCard).ToList();
    }

    internal static ContentBoardCard ToCard(BoardCardResponse r)
    {
        var assignees = r.Assignees.Select(ApiTaskMapper.ToCardAssignee).ToList();
        return new ContentBoardCard
        {
            ContentId = r.ContentId,
            ProjectId = r.ProjectId,
            Code = r.Code,
            Title = r.Title,
            Description = r.Description,
            Status = Enum.Parse<ContentStatus>(r.Status, ignoreCase: true),
            Priority = Enum.Parse<Priority>(r.Priority, ignoreCase: true),
            Deadline = ApiTaskMapper.ToDateTime(r.Deadline),
            PlannedPublishAt = r.PlannedPublishAt,
            Platforms = r.Platforms.ToList(),
            Tags = r.Tags.ToList(),
            HasPendingReview = r.HasPendingReview,
            Assignees = assignees,
            AssigneeName = assignees.FirstOrDefault()?.Name,
            AssigneeUserId = assignees.FirstOrDefault()?.UserId,
        };
    }
}
