using CreatorFlow.Models;
using CreatorFlow.Repositories;

namespace CreatorFlow.Api.Tests.Services;

internal sealed class AdminPlanRepositoryFake : IPlanRepository
{
    public List<Plan> Plans { get; } = new();
    public Exception? Failure { get; set; }

    public Task<Plan?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Plans.FirstOrDefault(plan => plan.PlanId == id));
    }

    public Task<Plan> AddAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        Plan saved = entity with
        {
            PlanId = Plans.Count == 0 ? 1 : Plans.Max(plan => plan.PlanId) + 1,
            CreatedAt = DateTime.UtcNow,
        };
        Plans.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult<IReadOnlyList<Plan>>(Plans);
    }

    public Task<Plan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Plans.FirstOrDefault(
            plan => string.Equals(plan.Code, code, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Plan?> UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        int index = Plans.FindIndex(plan => plan.PlanId == entity.PlanId);
        if (index < 0) return Task.FromResult<Plan?>(null);
        Plans[index] = entity;
        return Task.FromResult<Plan?>(entity);
    }
}
