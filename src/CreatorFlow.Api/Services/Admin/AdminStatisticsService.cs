using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Repositories.Admin;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Admin;

namespace CreatorFlow.Api.Services.Admin;

/// <summary>
/// SCRUM-23: thống kê cơ bản toàn hệ thống, chỉ đọc. Chỉ System Admin; không dùng ProjectRole.
/// </summary>
public sealed class AdminStatisticsService(
    IAdminStatisticsRepository repository,
    CurrentAuthenticatedUser currentUser,
    ILogger<AdminStatisticsService> logger)
{
    public async Task<AuthOperationResult<AdminStatisticsResponse>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.User is not { IsSystemAdmin: true })
            return AuthOperationResult<AdminStatisticsResponse>.Fail("forbidden", "Bạn không có quyền quản trị hệ thống.", status: 403);

        try
        {
            AdminStatisticsRecord record = await repository.GetAsync(cancellationToken);
            logger.LogInformation("Admin loaded system statistics. Users: {Users}; Projects: {Projects}",
                record.UsersTotal, record.ProjectsTotal);
            return new(new AdminStatisticsResponse(
                new AdminUserStats(record.UsersTotal, record.UsersActive, record.UsersLocked,
                    record.UsersDisabled, record.SystemAdmins),
                new AdminProjectStats(record.ProjectsTotal, record.ProjectsActiveLast30Days),
                [.. record.ContentsByStatus.Select(row => new AdminCountByKey(row.Key, row.Count))],
                [.. record.SubscriptionsByPlan.Select(row => new AdminPlanUsage(row.PlanId, row.Code, row.Name, row.Total, row.Active))],
                [.. record.ReportsByStatus.Select(row => new AdminCountByKey(row.Key, row.Count))],
                DateTimeOffset.UtcNow));
        }
        catch (Exception exception) when (AuthRules.IsDatabaseError(exception, cancellationToken))
        {
            return AuthOperationResult<AdminStatisticsResponse>.Fail("service_unavailable", "Không thể tải thống kê hệ thống lúc này.", status: 503);
        }
    }
}
