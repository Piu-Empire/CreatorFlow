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
        => await GetProjectContentsAsync(projectId, includeArchived: false, query: null, token);

    public async Task<AuthOperationResult<BoardResponse>> GetContentLibraryAsync(
        long projectId,
        ContentLibraryQuery? query = null,
        CancellationToken token = default)
        => await GetProjectContentsAsync(projectId, includeArchived: true, query, token);

    private async Task<AuthOperationResult<BoardResponse>> GetProjectContentsAsync(
        long projectId,
        bool includeArchived,
        ContentLibraryQuery? query,
        CancellationToken token)
    {
        if (current.User is not { } user)
            return AuthOperationResult<BoardResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
        if (await tasks.GetRoleAsync(projectId, user.UserId, token) is null)
            return AuthOperationResult<BoardResponse>.Fail("forbidden", "Bạn không thuộc Project này.", status: 403);

        if (query is not null && !ContentLibraryQueryRules.TryValidate(query, out string? queryField, out string? queryError))
            return AuthOperationResult<BoardResponse>.Fail("invalid_content_library_query", queryError!, queryField);

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

        IReadOnlyList<BoardCardResponse> resultCards = rows.Select(c => new BoardCardResponse(
            c.ContentId, c.ProjectId, c.Code, c.Title, c.Description, c.Status, c.Priority, c.Deadline, c.PlannedPublishAt,
            c.Platforms, c.Tags, c.HasPendingReview,
            assignees.TryGetValue(c.ContentId, out var list) ? list : [], c.Sprint)).ToList();

        if (includeArchived && query is not null)
            resultCards = ContentLibraryQueryRules.Apply(resultCards, query);

        return new(new BoardResponse(resultCards));
    }
}
