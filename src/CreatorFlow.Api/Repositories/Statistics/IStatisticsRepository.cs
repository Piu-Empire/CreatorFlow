using CreatorFlow.Api.Models.Statistics;

namespace CreatorFlow.Api.Repositories.Statistics;

/// <summary>
/// Đọc thống kê content + hiệu suất của một Project theo Creator (SCRUM-55).
/// Luôn scope theo <c>project_id</c>: không có truy vấn xuyên project.
/// </summary>
public interface IStatisticsRepository
{
    /// <summary>
    /// Mỗi Creator còn active trong Project một dòng. Creator đã rời Project không xuất hiện.
    /// Không đọc/ghi gì ngoài Project <paramref name="projectId"/>.
    /// </summary>
    Task<IReadOnlyList<CreatorStatisticsRecord>> GetCreatorStatisticsAsync(
        long projectId, CancellationToken cancellationToken = default);
}
