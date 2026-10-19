using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Board;
using CreatorFlow.Api.Repositories.Board;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Overdue;

namespace CreatorFlow.Api.Services.Overdue;

/// <summary>
/// Phát hiện Content quá deadline nhưng chưa hoàn thành (SCRUM-47). Quyền được kiểm tra Ở ĐÂY:
///   - phải đăng nhập;
///   - phải là thành viên Project (mọi vai trò đều xem được, giống Board).
/// Người xem luôn lấy từ JWT. Dữ liệu lấy từ chính nguồn của Board để Board và Overdue không lệch quy tắc;
/// "hôm nay" lấy theo UTC từ TimeProvider (giống TaskService).
/// </summary>
public sealed class OverdueService(
    IBoardCardRepository cards,
    ITaskRepository tasks,
    CurrentAuthenticatedUser current,
    TimeProvider timeProvider)
{
    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    public async Task<AuthOperationResult<OverdueResponse>> GetOverdueAsync(
        long projectId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized();

        if (await tasks.GetRoleAsync(projectId, user.UserId, token) is null)
            return Forbidden("Bạn không thuộc Project này.");

        DateOnly today = Today;
        IReadOnlyList<BoardCard> cardsOfProject = await cards.GetCardsAsync(projectId, token);
        var assignees = (await tasks.GetBoardAssigneesAsync(projectId, token))
            .GroupBy(a => a.ContentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<OverdueAssigneeResponse>)g
                    .Select(a => new OverdueAssigneeResponse(a.UserId, a.DisplayName))
                    .ToList());

        List<OverdueContentResponse> items = cardsOfProject
            .Where(card => OverdueRules.IsOverdue(card.Deadline, card.Status, today))
            .OrderBy(card => card.Deadline)
            .ThenBy(card => card.ContentId)
            .Select(card => new OverdueContentResponse(
                card.ContentId,
                card.ProjectId,
                card.Code,
                card.Title,
                card.Status,
                card.Deadline!.Value,
                OverdueRules.DaysLate(card.Deadline!.Value, today),
                assignees.TryGetValue(card.ContentId, out var list) ? list : [],
                card.Platforms))
            .ToList();

        return new(new OverdueResponse(projectId, today, items.Count, items));
    }

    private static AuthOperationResult<OverdueResponse> Unauthorized() =>
        AuthOperationResult<OverdueResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);

    private static AuthOperationResult<OverdueResponse> Forbidden(string message) =>
        AuthOperationResult<OverdueResponse>.Fail("forbidden", message, status: 403);
}
