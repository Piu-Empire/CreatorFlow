namespace CreatorFlow.Contracts.Admin;

/// <summary>Một đăng ký gói của người dùng, kèm email và thông tin gói.</summary>
public sealed record AdminSubscriptionResponse(
    long SubscriptionId,
    long UserId,
    string UserEmail,
    long PlanId,
    string PlanCode,
    string PlanName,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? ExpiresAt);

/// <summary>Danh sách đăng ký gói kèm tổng số dòng khớp bộ lọc.</summary>
public sealed record AdminSubscriptionsListResponse(
    IReadOnlyList<AdminSubscriptionResponse> Items,
    int TotalCount);

/// <summary>Gán gói cho một người dùng; ExpiresAt null = không hết hạn.</summary>
public sealed record CreateSubscriptionRequest(long UserId, long PlanId, DateTimeOffset? ExpiresAt);

/// <summary>Đổi trạng thái hoặc gia hạn; field null = giữ nguyên.</summary>
public sealed record UpdateSubscriptionRequest(string? Status, DateTimeOffset? ExpiresAt);
