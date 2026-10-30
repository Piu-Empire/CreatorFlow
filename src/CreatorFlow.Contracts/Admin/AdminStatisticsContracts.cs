namespace CreatorFlow.Contracts.Admin;

/// <summary>Một dòng đếm theo khoá (trạng thái, gói...).</summary>
public sealed record AdminCountByKey(string Key, int Count);

/// <summary>Số liệu người dùng toàn hệ thống.</summary>
public sealed record AdminUserStats(int Total, int Active, int Locked, int Disabled, int SystemAdmins);

/// <summary>Số liệu project; ActiveLast30Days = có Content Published trong 30 ngày gần nhất.</summary>
public sealed record AdminProjectStats(int Total, int ActiveLast30Days);

/// <summary>Mức sử dụng của một gói dịch vụ.</summary>
public sealed record AdminPlanUsage(long PlanId, string Code, string Name, int Total, int Active);

/// <summary>
/// Thống kê cơ bản toàn hệ thống cho System Admin (SCRUM-23).
/// Mọi giá trị đọc trực tiếp từ PostgreSQL, không cache.
/// </summary>
public sealed record AdminStatisticsResponse(
    AdminUserStats Users,
    AdminProjectStats Projects,
    IReadOnlyList<AdminCountByKey> ContentsByStatus,
    IReadOnlyList<AdminPlanUsage> SubscriptionsByPlan,
    IReadOnlyList<AdminCountByKey> ReportsByStatus,
    DateTimeOffset GeneratedAt);
