namespace CreatorFlow.Contracts.Statistics;

/// <summary>
/// Một dòng thống kê của một Creator trong Project (SCRUM-55).
/// Total/InProgress/Published/Overdue đếm trên <c>contents</c> (không tính Archived);
/// Views/Likes/Comments/Shares cộng trên snapshot mới nhất của mỗi ContentPlatform.
/// <see cref="EngagementPercent"/> = (Likes + Comments + Shares) / Views × 100, làm tròn 2 chữ số.
/// </summary>
public sealed record CreatorStatisticsRow(
    long CreatorId, string Name, int Total, int InProgress, int Published, int Overdue,
    long Views, long Likes, long Comments, long Shares, double EngagementPercent);

/// <summary>
/// Thống kê theo Creator và Project (SCRUM-55). <see cref="Project"/> là tổng của các dòng
/// <see cref="Creators"/> của CHÍNH project này — không cộng dồn project khác.
/// </summary>
public sealed record CreatorStatisticsResponse(
    long ProjectId, CreatorStatisticsRow Project, IReadOnlyList<CreatorStatisticsRow> Creators);
