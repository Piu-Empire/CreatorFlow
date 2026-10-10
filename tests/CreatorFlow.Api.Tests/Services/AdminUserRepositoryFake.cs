using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Admin;

namespace CreatorFlow.Api.Tests.Services;

internal sealed class AdminUserRepositoryFake : IAdminUserRepository
{
    public List<AdminUserRecord> Rows { get; } = new();
    public int TotalCount { get; set; }
    public AdminStatusChange? UpdateResult { get; set; } =
        new(true, true, false, AccountStatus.Active, false, "target@example.test");
    public Exception? Failure { get; set; }
    public string? LastSearch { get; private set; }
    public string? LastStatus { get; private set; }
    public int LastLimit { get; private set; }
    public int LastOffset { get; private set; }

    public Task<IReadOnlyList<AdminUserRecord>> SearchAsync(string? search, string? status, int limit, int offset,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        LastSearch = search;
        LastStatus = status;
        LastLimit = limit;
        LastOffset = offset;
        return Task.FromResult<IReadOnlyList<AdminUserRecord>>(Rows);
    }

    public Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(TotalCount);
    }

    public Task<AdminStatusChange?> UpdateStatusAsync(long targetUserId, AccountStatus status, long actorUserId,
        string traceId, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(UpdateResult);
    }
}
