namespace CreatorFlow.Api.Models.Admin;

/// <summary>Số liệu thống kê cơ bản toàn hệ thống (SCRUM-23).</summary>
public sealed class AdminStatisticsRecord
{
    public int UsersTotal { get; init; }
    public int UsersActive { get; init; }
    public int UsersLocked { get; init; }
    public int UsersDisabled { get; init; }
    public int SystemAdmins { get; init; }

    public int ProjectsTotal { get; init; }
    public int ProjectsActiveLast30Days { get; init; }

    /// <summary>Khoá = trạng thái Content trong DB (IDEA, SCRIPT...), đã sắp theo workflow.</summary>
    public IReadOnlyList<(string Key, int Count)> ContentsByStatus { get; init; } = [];

    public IReadOnlyList<AdminPlanUsageRecord> SubscriptionsByPlan { get; init; } = [];

    /// <summary>Khoá = trạng thái report trong DB (OPEN, IN_REVIEW...).</summary>
    public IReadOnlyList<(string Key, int Count)> ReportsByStatus { get; init; } = [];
}

/// <summary>Mức sử dụng một gói: tổng số đăng ký và số đang ACTIVE.</summary>
public sealed record AdminPlanUsageRecord(long PlanId, string Code, string Name, int Total, int Active);
