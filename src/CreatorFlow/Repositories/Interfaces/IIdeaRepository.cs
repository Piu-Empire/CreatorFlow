using CreatorFlow.Models;

namespace CreatorFlow.Repositories.Interfaces;

/// <summary>
/// Truy cập kho ý tưởng (bảng ideas, idea_tags, tags). Mọi truy vấn danh sách đều theo ProjectId
/// để dữ liệu giữa các Project không lẫn nhau. Phân quyền do IdeaService kiểm tra, không ở đây.
/// </summary>
public interface IIdeaRepository
{
    /// <summary>Idea của Project, đã áp bộ lọc. Mới cập nhật nhất xếp trước.</summary>
    List<Idea> GetByProject(long projectId, IdeaFilter filter);

    /// <summary>Một Idea theo Id (kèm tag), hoặc null nếu không tồn tại.</summary>
    Idea? GetById(long ideaId);

    /// <summary>Tên mọi tag đã có trong Project (dùng gợi ý và lọc), sắp theo tên.</summary>
    List<string> GetProjectTags(long projectId);

    /// <summary>Tạo Idea mới của Project (kèm tag), trả về Id mới. Chạy trong transaction hiện hành.</summary>
    long Create(long projectId, IdeaDraft draft, long createdByUserId);

    /// <summary>Cập nhật tiêu đề, mô tả, ghi chú, trạng thái và đồng bộ tag. Chạy trong transaction hiện hành.</summary>
    void Update(long ideaId, IdeaDraft draft);

    /// <summary>Xóa Idea (idea_tags xóa theo; Content đã tạo từ Idea này giữ lại, source_idea_id về NULL).</summary>
    void Delete(long ideaId);
}
