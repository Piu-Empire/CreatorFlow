using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Repositories.Board;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Board;
using CreatorFlow.Contracts.Tasks;

namespace CreatorFlow.Api.Services.Board;

/// <summary>
/// Nghiệp vụ đọc Board. Quyền kiểm tra Ở ĐÂY (không tin giao diện): chỉ thành viên đang hoạt động của Project mới xem được thẻ.
/// Người thao tác luôn lấy từ JWT. Vai trò lấy qua ITaskRepository.GetRoleAsync (giống IdeaService / MetricService).
/// </summary>
public sealed class BoardService(IBoardCardRepository cards, ITaskRepository tasks, CurrentAuthenticatedUser current)
{
    public async Task<AuthOperationResult<BoardResponse>> GetBoardAsync(long projectId, CancellationToken token = default)
        => await GetProjectContentsAsync(projectId, includeArchived: false, token);

    public async Task<AuthOperationResult<BoardResponse>> GetContentLibraryAsync(long projectId, CancellationToken token = default)
        => await GetProjectContentsAsync(projectId, includeArchived: true, token);

    private async Task<AuthOperationResult<BoardResponse>> GetProjectContentsAsync(long projectId, bool includeArchived, CancellationToken token)
    {
        if (current.User is not { } user)
            return AuthOperationResult<BoardResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
        if (await tasks.GetRoleAsync(projectId, user.UserId, token) is null)
            return AuthOperationResult<BoardResponse>.Fail("forbidden", "Bạn không thuộc Project này.", status: 403);

        var rows = includeArchived
            ? await cards.GetProjectContentsAsync(projectId, token)
            : await cards.GetCardsAsync(projectId, token);
        var assignees = (await tasks.GetBoardAssigneesAsync(projectId, token))
            .GroupBy(a => a.ContentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BoardAssigneeResponse>)g
                    .Select(a => new BoardAssigneeResponse(a.ContentId, a.UserId, a.DisplayName, a.Status.ToString(), a.ProgressPercent, a.Deadline))
                    .ToList());

        return new(new BoardResponse(rows.Select(c => new BoardCardResponse(
            c.ContentId, c.ProjectId, c.Code, c.Title, c.Description, c.Status, c.Priority, c.Deadline, c.PlannedPublishAt,
            c.Platforms, c.Tags, c.HasPendingReview,
            assignees.TryGetValue(c.ContentId, out var list) ? list : [])).ToList()));
    }
}
