using CreatorFlow.ApiClients;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Backends;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Board vẫn đọc thẻ từ repository hiện có (phần Board chưa chuyển sang API), nhưng Creator được giao trên mỗi thẻ
/// (avatar, "2/3 hoàn thành") lấy từ CreatorFlow.Api để quy tắc "assignment chưa huỷ" nằm ở Backend.
/// </summary>
public sealed class ApiAssigneeBoardRepository(IBoardRepository inner, ApiClient api, UserSession session) : IBoardRepository
{
    public List<ContentBoardCard> GetBoardCards(long projectId)
    {
        var cards = inner.GetBoardCards(projectId);

        var response = ApiTaskCall.Run(() => api.GetBoardAssigneesAsync(projectId, ApiTaskCall.BearerOf(session)));
        var byContent = response.Assignees.ToLookup(a => a.ContentId);

        foreach (var card in cards)
        {
            card.Assignees = byContent[card.ContentId].Select(ApiTaskMapper.ToCardAssignee).ToList();
            var first = card.Assignees.FirstOrDefault();
            card.AssigneeUserId = first?.UserId;
            card.AssigneeName = first?.Name;
        }
        return cards;
    }
}
