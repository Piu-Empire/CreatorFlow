using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Repositories.Admin;
using CreatorFlow.Models;

namespace CreatorFlow.Api.Tests.Services;

internal sealed class AdminSubscriptionRepositoryFake(AdminPlanRepositoryFake plans) : ISubscriptionRepository
{
    public List<AdminSubscriptionRecord> Rows { get; } = new();
    public HashSet<long> UserIds { get; } = new();
    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<AdminSubscriptionRecord>> SearchAsync(string? status, long? planId, string? search,
        int limit, int offset, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        IReadOnlyList<AdminSubscriptionRecord> items = Filter(status, planId, search)
            .Skip(offset).Take(limit).ToList();
        return Task.FromResult(items);
    }

    public Task<int> CountAsync(string? status, long? planId, string? search,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Filter(status, planId, search).Count());
    }

    public Task<AdminSubscriptionRecord?> GetByIdAsync(long subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Rows.FirstOrDefault(row => row.SubscriptionId == subscriptionId));
    }

    public Task<AdminSubscriptionRecord?> GetActiveByUserAsync(long userId,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Rows.FirstOrDefault(row => row.UserId == userId && row.Status == "ACTIVE"));
    }

    public Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(UserIds.Contains(userId));
    }

    public Task<AdminSubscriptionRecord?> CreateAsync(long userId, long planId, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        Plan? plan = plans.Plans.FirstOrDefault(item => item.PlanId == planId);
        if (!UserIds.Contains(userId) || plan is null) return Task.FromResult<AdminSubscriptionRecord?>(null);

        var created = new AdminSubscriptionRecord
        {
            SubscriptionId = Rows.Count == 0 ? 1 : Rows.Max(row => row.SubscriptionId) + 1,
            UserId = userId,
            UserEmail = $"user{userId}@example.test",
            PlanId = planId,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            Status = "ACTIVE",
            StartedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
        };
        Rows.Add(created);
        return Task.FromResult<AdminSubscriptionRecord?>(created);
    }

    public Task<AdminSubscriptionRecord?> UpdateAsync(long subscriptionId, string status, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        int index = Rows.FindIndex(row => row.SubscriptionId == subscriptionId);
        if (index < 0) return Task.FromResult<AdminSubscriptionRecord?>(null);

        AdminSubscriptionRecord existing = Rows[index];
        var saved = new AdminSubscriptionRecord
        {
            SubscriptionId = existing.SubscriptionId,
            UserId = existing.UserId,
            UserEmail = existing.UserEmail,
            PlanId = existing.PlanId,
            PlanCode = existing.PlanCode,
            PlanName = existing.PlanName,
            Status = status,
            StartedAt = existing.StartedAt,
            ExpiresAt = expiresAt ?? existing.ExpiresAt,
        };
        Rows[index] = saved;
        return Task.FromResult<AdminSubscriptionRecord?>(saved);
    }

    private IEnumerable<AdminSubscriptionRecord> Filter(string? status, long? planId, string? search) =>
        Rows.Where(row => status is null || row.Status == status)
            .Where(row => planId is null || row.PlanId == planId)
            .Where(row => search is null || row.UserEmail.Contains(search, StringComparison.OrdinalIgnoreCase));
}
