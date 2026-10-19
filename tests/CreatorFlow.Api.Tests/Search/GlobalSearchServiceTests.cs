using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Models.Search;
using CreatorFlow.Api.Repositories.Search;
using CreatorFlow.Api.Services.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Search;

[TestClass]
[TestCategory("GlobalSearchApiUnit")]
public sealed class GlobalSearchServiceTests
{
    [TestMethod]
    public async Task Search_RequiresAuthenticatedUser_WithoutCallingRepository()
    {
        var repository = new SearchRepositoryFake();
        var service = new GlobalSearchService(repository, new CurrentAuthenticatedUser());

        var result = await service.SearchAsync("content", 30);

        Assert.AreEqual(401, result.Status);
        Assert.AreEqual("unauthorized", result.ErrorCode);
        Assert.AreEqual(0, repository.CallCount);
    }

    [TestMethod]
    public async Task Search_TooShortQueryReturnsEmptyWithoutCallingRepository()
    {
        var repository = new SearchRepositoryFake();
        var service = Create(repository);

        var result = await service.SearchAsync(" x ", 30);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, result.Value!.Items.Count);
        Assert.AreEqual(0, repository.CallCount);
    }

    [TestMethod]
    public async Task Search_TrimsQueryClampsLimitAndMapsResults()
    {
        var repository = new SearchRepositoryFake
        {
            Results =
            [
                new GlobalSearchRecord("Content", 42, 7, "Demo project", "Bán người sang cam",
                    "CNT-042 · IDEA · MEDIUM", "IDEA"),
            ],
        };
        var service = Create(repository);

        var result = await service.SearchAsync("  bán người sang cam  ", 500);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("bán người sang cam", repository.LastQuery);
        Assert.AreEqual(50, repository.LastLimit);
        Assert.AreEqual(1, result.Value!.Items.Count);
        Assert.AreEqual(42, result.Value.Items[0].EntityId);
        Assert.AreEqual("Bán người sang cam", result.Value.Items[0].Title);
    }

    [TestMethod]
    public async Task Search_RejectsQueryLongerThanOneHundredCharacters()
    {
        var repository = new SearchRepositoryFake();
        var service = Create(repository);

        var result = await service.SearchAsync(new string('a', 101), 30);

        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("validation", result.ErrorCode);
        Assert.AreEqual("q", result.ErrorField);
        Assert.AreEqual(0, repository.CallCount);
    }

    private static GlobalSearchService Create(SearchRepositoryFake repository)
    {
        var current = new CurrentAuthenticatedUser
        {
            User = new User
            {
                UserId = 19,
                Email = "search@example.test",
                DisplayName = "Search User",
                PasswordHash = "unused",
            },
        };
        return new GlobalSearchService(repository, current);
    }

    private sealed class SearchRepositoryFake : IGlobalSearchRepository
    {
        public IReadOnlyList<GlobalSearchRecord> Results { get; init; } = Array.Empty<GlobalSearchRecord>();
        public int CallCount { get; private set; }
        public long LastUserId { get; private set; }
        public string? LastQuery { get; private set; }
        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<GlobalSearchRecord>> SearchAsync(
            long userId, string query, int limit, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastUserId = userId;
            LastQuery = query;
            LastLimit = limit;
            return Task.FromResult(Results);
        }
    }
}
