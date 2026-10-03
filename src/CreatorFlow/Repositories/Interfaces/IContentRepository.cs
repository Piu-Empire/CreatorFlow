using CreatorFlow.Models.Enums;
using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

public interface IContentRepository
{
    Content GetById(long contentId);

    /// <summary>Cập nhật Status + UpdatedAt của Content. Phải chạy trong cùng transaction với UnitOfWork.</summary>
    void UpdateStatus(long contentId, ContentStatus newStatus);
}