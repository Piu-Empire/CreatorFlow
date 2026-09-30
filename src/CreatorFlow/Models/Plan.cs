namespace CreatorFlow.Models;

public sealed record Plan
{
    public long PlanId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public decimal PriceMonthly { get; init; }

    public int? MaxProjects { get; init; }

    public int? MaxMembers { get; init; }

    public int? AiRequestLimit { get; init; }

    public bool IsActive { get; init; } = true;

    public DateTime CreatedAt { get; init; }
}
