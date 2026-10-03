using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IBoardRepository
{
    /// <summary>Lấy toàn bộ card của Project để hiển thị trên Board, BoardForm tự nhóm theo Status.</summary>
    List<ContentBoardCard> GetBoardCards(long projectId);
}