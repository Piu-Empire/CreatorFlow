namespace CreatorFlow.Api.Models.Admin;

/// <summary>Một đăng ký gói (nội bộ API), đã join sẵn email người dùng và thông tin gói.</summary>
public sealed class AdminSubscriptionRecord
{
    public long SubscriptionId { get; init; }

    public long UserId { get; init; }

    public required string UserEmail { get; init; }

    public long PlanId { get; init; }

    public required string PlanCode { get; init; }

    public required string PlanName { get; init; }

    public required string Status { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
