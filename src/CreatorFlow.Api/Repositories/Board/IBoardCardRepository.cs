using CreatorFlow.Api.Models.Board;

namespace CreatorFlow.Api.Repositories.Board;

/// <summary>Đọc các thẻ Content của Board bằng Npgsql (chỉ Backend được chạm database).</summary>
public interface IBoardCardRepository
{
    /// <summary>Mọi Content của Project trừ Content đã Archived, theo ContentId.</summary>
    Task<IReadOnlyList<BoardCard>> GetCardsAsync(long projectId, CancellationToken cancellationToken = default);
}
