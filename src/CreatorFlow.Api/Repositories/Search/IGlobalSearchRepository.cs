using CreatorFlow.Api.Models.Search;

namespace CreatorFlow.Api.Repositories.Search;

public interface IGlobalSearchRepository
{
    Task<IReadOnlyList<GlobalSearchRecord>> SearchAsync(
        long userId, string query, int limit, CancellationToken cancellationToken = default);
}
