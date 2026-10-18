using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Repositories.Search;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Search;

namespace CreatorFlow.Api.Services.Search;

public sealed class GlobalSearchService(IGlobalSearchRepository repository, CurrentAuthenticatedUser current)
{
    public async Task<AuthOperationResult<GlobalSearchResponse>> SearchAsync(
        string? query, int limit, CancellationToken cancellationToken = default)
    {
        if (current.User is not { } user)
            return AuthOperationResult<GlobalSearchResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);

        string normalized = (query ?? string.Empty).Trim();
        if (normalized.Length > 100)
            return AuthOperationResult<GlobalSearchResponse>.Fail("validation", "Từ khóa tìm kiếm tối đa 100 ký tự.", "q");

        if (normalized.Length < 2)
            return new(new GlobalSearchResponse(Array.Empty<GlobalSearchItemResponse>()));

        int safeLimit = Math.Clamp(limit, 1, 50);
        IReadOnlyList<GlobalSearchItemResponse> results = (await repository.SearchAsync(
                user.UserId, normalized, safeLimit, cancellationToken))
            .Select(item => new GlobalSearchItemResponse(item.Type, item.EntityId, item.ProjectId,
                item.ProjectName, item.Title, item.Subtitle, item.Status))
            .ToArray();

        return new(new GlobalSearchResponse(results));
    }
}
