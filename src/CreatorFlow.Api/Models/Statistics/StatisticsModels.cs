namespace CreatorFlow.Api.Models.Statistics;

/// <summary>Một dòng thống kê theo Creator đọc từ SQL (chưa tính Engagement) — SCRUM-55.</summary>
public sealed record CreatorStatisticsRecord(
    long CreatorId, string Name, int Total, int InProgress, int Published, int Overdue,
    long Views, long Likes, long Comments, long Shares);
