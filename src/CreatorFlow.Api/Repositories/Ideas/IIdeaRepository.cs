using CreatorFlow.Api.Models.Ideas;

namespace CreatorFlow.Api.Repositories.Ideas;

/// <summary>Truy cập kho ý tưởng (ideas, idea_tags, tags) bằng Npgsql. Chỉ Backend được chạm database.</summary>
public interface IIdeaRepository
{
    Task<IReadOnlyList<Idea>> GetByProjectAsync(long projectId, IdeaFilter filter, CancellationToken cancellationToken = default);

    Task<Idea?> GetByIdAsync(long ideaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetProjectTagsAsync(long projectId, CancellationToken cancellationToken = default);

    /// <summary>Tạo Idea + tag trong MỘT transaction, trả về Id mới.</summary>
    Task<long> CreateAsync(long projectId, IdeaDraft draft, long createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>Sửa Idea + đồng bộ tag trong MỘT transaction.</summary>
    Task UpdateAsync(long ideaId, IdeaDraft draft, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt Idea sang Converted, tạo Content (trạng thái Idea, giữ source_idea_id, nối tag) và ghi lịch sử trong MỘT transaction.
    /// Trả về null nếu Idea không còn ở Draft/Backlog (không ghi gì).
    /// </summary>
    Task<long?> ConvertToContentAsync(long ideaId, string contentType, long convertedByUserId, string historyNote, CancellationToken cancellationToken = default);

    Task DeleteAsync(long ideaId, CancellationToken cancellationToken = default);
}
