using CreatorFlow.Contracts.Statistics;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Thống kê số Content và hiệu suất theo Creator trong Project (SCRUM-55) qua CreatorFlow.Api.
/// Client chỉ hiển thị; quyền và phạm vi project do Backend quyết định.
/// </summary>
public interface IStatisticsBackend
{
    /// <summary><paramref name="creatorId"/> null = tất cả Creator của Project.</summary>
    CreatorStatisticsResponse GetCreatorStatistics(long projectId, long? creatorId);
}
