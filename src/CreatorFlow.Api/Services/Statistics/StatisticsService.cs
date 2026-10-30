using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Statistics;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Statistics;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Services.Metrics;
using CreatorFlow.Contracts.Statistics;

namespace CreatorFlow.Api.Services.Statistics;

/// <summary>
/// SCRUM-55: thống kê số Content + hiệu suất theo Creator trong MỘT Project.
/// Quyền đọc giống Metrics (mọi thành viên active của Project); người ngoài Project bị chặn tại đây,
/// không chỉ ẩn trên UI. Không có nhánh nào cộng dồn dữ liệu của project khác.
/// </summary>
public sealed class StatisticsService(
    IStatisticsRepository statistics, ITaskRepository tasks, CurrentAuthenticatedUser current)
{
    public async Task<AuthOperationResult<CreatorStatisticsResponse>> GetCreatorStatisticsAsync(
        long projectId, long? creatorId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized();

        ProjectRole? role = await tasks.GetRoleAsync(projectId, user.UserId, token);
        if (role is null) return NotMember();

        // creatorId <= 0 coi như "tất cả"; UI không bao giờ gửi id thật <= 0.
        long? filter = creatorId is > 0 ? creatorId : null;
        IReadOnlyList<CreatorStatisticsRecord> records =
            await statistics.GetCreatorStatisticsAsync(projectId, token);

        List<CreatorStatisticsRow> all = records.Select(ToRow).ToList();
        List<CreatorStatisticsRow> rows = filter is { } id
            ? all.Where(r => r.CreatorId == id).ToList()
            : all;

        // Dòng "Toàn Project" luôn là tổng của TOÀN BỘ creator trong project, không đổi khi lọc creator.
        return new(new CreatorStatisticsResponse(projectId, SumProject(all), rows));
    }

    private static CreatorStatisticsRow ToRow(CreatorStatisticsRecord r) => new(
        r.CreatorId, r.Name, r.Total, r.InProgress, r.Published, r.Overdue,
        r.Views, r.Likes, r.Comments, r.Shares,
        MetricEngagement.Percent(r.Views, r.Likes, r.Comments, r.Shares));

    /// <summary>
    /// Dòng "Toàn Project" = tổng các dòng Creator của chính Project này (đã lọc theo creatorId nếu có).
    /// Engagement tính trên tổng, không lấy trung bình cộng của các dòng.
    /// </summary>
    private static CreatorStatisticsRow SumProject(IReadOnlyList<CreatorStatisticsRow> rows)
    {
        long views = rows.Sum(r => r.Views);
        long likes = rows.Sum(r => r.Likes);
        long comments = rows.Sum(r => r.Comments);
        long shares = rows.Sum(r => r.Shares);
        return new CreatorStatisticsRow(
            0, "Toàn Project",
            rows.Sum(r => r.Total), rows.Sum(r => r.InProgress), rows.Sum(r => r.Published), rows.Sum(r => r.Overdue),
            views, likes, comments, shares,
            MetricEngagement.Percent(views, likes, comments, shares));
    }

    private static AuthOperationResult<CreatorStatisticsResponse> Unauthorized() =>
        AuthOperationResult<CreatorStatisticsResponse>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);

    private static AuthOperationResult<CreatorStatisticsResponse> NotMember() =>
        AuthOperationResult<CreatorStatisticsResponse>.Fail("forbidden", "Bạn không thuộc Project này.", status: 403);
}
