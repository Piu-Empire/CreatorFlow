using System.Text.RegularExpressions;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Nghiệp vụ kho ý tưởng (Idea Bank): thêm / sửa / xóa theo quyền, tag, trạng thái, ghi chú, tìm kiếm và lọc.
/// Quy tắc:
///   - Chỉ thành viên của Project mới xem/ghi được Idea của Project đó (dữ liệu tách theo Project).
///   - Quyền thêm/sửa/xóa theo <see cref="IdeaRules"/> (Owner/Manager mọi Idea; Creator chỉ Idea của mình).
///   - Tiêu đề bắt buộc (tối đa 250 ký tự); mô tả và ghi chú tối đa 2000 ký tự; tối đa 10 tag, mỗi tag tối đa 80 ký tự.
///   - Tag được chuẩn hóa (bỏ '#', gộp khoảng trắng, loại trùng không phân biệt hoa/thường) và dùng lại đúng cách viết
///     của tag đã có trong Project.
///   - Ghi nhiều bảng (ideas + tags + idea_tags) luôn nằm trong một transaction.
///   - Chuyển Idea thành Content (SCRUM-31): tạo Content ở trạng thái Idea mang tiêu đề/mô tả/tag, giữ liên kết nguồn
///     (contents.source_idea_id), đặt Idea sang Converted và ghi lịch sử Content — tất cả trong một transaction.
///     Idea gốc không bị xóa hay mất dữ liệu.
/// </summary>
public class IdeaService
{
    public const int MaxTitleLength = 250;
    public const int MaxDescriptionLength = 2000;
    public const int MaxNoteLength = 2000;
    public const int MaxTagCount = 10;
    public const int MaxTagLength = 80;

    private readonly IIdeaRepository _ideaRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IContentStatusHistoryRepository _historyRepo;
    private readonly IUnitOfWork _unitOfWork;

    public IdeaService(
        IIdeaRepository ideaRepo,
        IProjectMemberRepository memberRepo,
        IContentStatusHistoryRepository historyRepo,
        IUnitOfWork unitOfWork)
    {
        _ideaRepo = ideaRepo;
        _memberRepo = memberRepo;
        _historyRepo = historyRepo;
        _unitOfWork = unitOfWork;
    }

    // ---------- Đọc ----------

    /// <summary>Idea của Project theo bộ lọc. Người ngoài Project bị từ chối.</summary>
    public List<Idea> GetIdeas(long projectId, long userId, IdeaFilter? filter = null)
    {
        EnsureIsProjectMember(projectId, userId);
        return _ideaRepo.GetByProject(projectId, filter ?? new IdeaFilter());
    }

    /// <summary>Các tag đã có trong Project (cho ô lọc và gợi ý khi nhập).</summary>
    public List<string> GetProjectTags(long projectId, long userId)
    {
        EnsureIsProjectMember(projectId, userId);
        return _ideaRepo.GetProjectTags(projectId);
    }

    /// <summary>Số Idea đang ở Backlog của Project (hiện ở badge sidebar). 0 nếu người dùng không thuộc Project.</summary>
    public int CountBacklog(long projectId, long userId)
    {
        if (_memberRepo.GetRole(projectId, userId) is null) return 0;
        return _ideaRepo.GetByProject(projectId, new IdeaFilter { Status = IdeaStatus.Backlog }).Count;
    }

    // ---------- Quyền (UI dùng để bật/tắt nút) ----------

    public bool CanCreate(long projectId, long userId) =>
        IdeaRules.CanCreate(_memberRepo.GetRole(projectId, userId));

    public bool CanEdit(Idea idea, long userId) =>
        IdeaRules.CanEdit(_memberRepo.GetRole(idea.ProjectId, userId), idea, userId);

    public bool CanDelete(Idea idea, long userId) =>
        IdeaRules.CanDelete(_memberRepo.GetRole(idea.ProjectId, userId), idea, userId);

    public bool CanConvert(Idea idea, long userId) =>
        IdeaRules.CanConvert(_memberRepo.GetRole(idea.ProjectId, userId), idea, userId);

    // ---------- Validation ----------

    /// <summary>Chuẩn hóa dữ liệu nhập trực tiếp trên draft: trim chữ, chuẩn hóa tag (bỏ '#', gộp khoảng trắng, loại trùng).</summary>
    public static void Normalize(IdeaDraft d)
    {
        d.Title = (d.Title ?? string.Empty).Trim();
        d.Description = (d.Description ?? string.Empty).Trim();
        d.Note = (d.Note ?? string.Empty).Trim();
        d.Tags = (d.Tags ?? new List<string>())
            .Select(NormalizeTag)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Trả về thông báo lỗi đầu tiên (hoặc null nếu hợp lệ) mà không ném exception — Form dùng để báo lỗi ngay trong dialog.
    /// Hàm này KHÔNG sửa <paramref name="draft"/>: gọi <see cref="Normalize"/> trước nếu dữ liệu đến từ ô nhập.
    /// </summary>
    public static string? GetValidationError(IdeaDraft draft)
    {
        if (draft.Title.Length == 0)
            return "Vui lòng nhập tiêu đề.";
        if (draft.Title.Length > MaxTitleLength)
            return $"Tiêu đề tối đa {MaxTitleLength} ký tự.";
        if (draft.Description.Length > MaxDescriptionLength)
            return $"Mô tả tối đa {MaxDescriptionLength} ký tự.";
        if (draft.Note.Length > MaxNoteLength)
            return $"Ghi chú tối đa {MaxNoteLength} ký tự.";
        if (draft.Tags.Count > MaxTagCount)
            return $"Tối đa {MaxTagCount} tag cho mỗi Idea.";
        string? longTag = draft.Tags.FirstOrDefault(t => t.Length > MaxTagLength);
        if (longTag is not null)
            return $"Mỗi tag tối đa {MaxTagLength} ký tự.";
        if (!Enum.IsDefined(draft.Status))
            return "Trạng thái Idea không hợp lệ.";

        return null;
    }

    // ---------- Ghi ----------

    public long Create(long projectId, IdeaDraft draft, long userId)
    {
        Normalize(draft);
        Validate(draft);

        var role = EnsureIsProjectMember(projectId, userId);
        if (!IdeaRules.CanCreate(role))
            throw new UnauthorizedWorkflowActionException("Bạn không có quyền thêm Idea vào Project này.");

        IdeaRules.ValidateStatusChange(null, draft.Status);
        draft.Tags = ReuseExistingTagNames(projectId, draft.Tags);

        _unitOfWork.Begin();
        try
        {
            long id = _ideaRepo.Create(projectId, draft, userId);
            _unitOfWork.Commit();
            return id;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public void Update(long ideaId, IdeaDraft draft, long userId)
    {
        Normalize(draft);
        Validate(draft);

        var idea = GetIdeaOrThrow(ideaId);
        var role = EnsureIsProjectMember(idea.ProjectId, userId);
        if (!IdeaRules.CanEdit(role, idea, userId))
            throw new UnauthorizedWorkflowActionException("Bạn chỉ được sửa Idea do chính mình tạo.");

        IdeaRules.ValidateStatusChange(idea.Status, draft.Status);
        draft.Tags = ReuseExistingTagNames(idea.ProjectId, draft.Tags);

        _unitOfWork.Begin();
        try
        {
            _ideaRepo.Update(ideaId, draft);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Chuyển Idea thành Content mới (trạng thái Idea trên Production Board), trả về Id Content mới.
    /// Content mang tiêu đề, mô tả và tag của Idea; ghi chú nội bộ của Idea ở lại Idea. Idea gốc được giữ lại,
    /// chuyển sang Converted và truy ngược được từ Content qua source_idea_id và dòng lịch sử "Tạo từ IDEA-xxx".
    /// </summary>
    public long ConvertToContent(long ideaId, long userId)
    {
        var idea = GetIdeaOrThrow(ideaId);
        var role = EnsureIsProjectMember(idea.ProjectId, userId);

        if (!IdeaRules.CanEdit(role, idea, userId))
            throw new UnauthorizedWorkflowActionException("Bạn chỉ được chuyển Idea do chính mình tạo thành Content.");

        IdeaRules.ValidateConvertible(idea.Status);

        // Giới hạn của Content chặt hơn của Idea (tiêu đề 200 < 250): báo rõ thay vì cắt bớt âm thầm.
        if (idea.Title.Length > ContentService.MaxTitleLength)
            throw new IdeaValidationException(
                $"Tiêu đề Idea dài {idea.Title.Length} ký tự, vượt giới hạn {ContentService.MaxTitleLength} ký tự của Content. Hãy rút gọn tiêu đề rồi chuyển lại.");
        if (idea.Description.Length > ContentService.MaxDescriptionLength)
            throw new IdeaValidationException(
                $"Mô tả Idea dài {idea.Description.Length} ký tự, vượt giới hạn {ContentService.MaxDescriptionLength} ký tự của Content. Hãy rút gọn mô tả rồi chuyển lại.");

        _unitOfWork.Begin();
        try
        {
            long contentId = _ideaRepo.ConvertToContent(ideaId, ContentService.DefaultContentType, userId)
                ?? throw new IdeaValidationException("Idea này vừa được chuyển hoặc không còn ở trạng thái có thể chuyển. Hãy tải lại danh sách.");

            _historyRepo.Add(new ContentStatusHistory
            {
                ContentId = contentId,
                FromStatus = ContentStatus.Idea,
                ToStatus = ContentStatus.Idea,
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow,
                Note = $"Tạo từ {idea.Code} — {idea.Title}",
            });

            _unitOfWork.Commit();
            return contentId;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public void Delete(long ideaId, long userId)
    {
        var idea = GetIdeaOrThrow(ideaId);
        var role = EnsureIsProjectMember(idea.ProjectId, userId);

        if (!IdeaRules.CanEdit(role, idea, userId))
            throw new UnauthorizedWorkflowActionException("Bạn chỉ được xóa Idea do chính mình tạo.");
        if (idea.Status == IdeaStatus.Converted)
            throw new IdeaValidationException("Idea đã chuyển thành Content nên không thể xóa. Hãy chuyển sang Lưu trữ nếu không dùng nữa.");

        _unitOfWork.Begin();
        try
        {
            _ideaRepo.Delete(ideaId);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ---------- Helpers ----------

    private static void Validate(IdeaDraft d)
    {
        string? error = GetValidationError(d);
        if (error != null)
            throw new IdeaValidationException(error);
    }

    private static string NormalizeTag(string? tag)
    {
        string text = (tag ?? string.Empty).Trim().TrimStart('#').Trim();
        return Regex.Replace(text, @"\s+", " ");
    }

    /// <summary>Tag trùng (không phân biệt hoa/thường) với tag đã có trong Project thì dùng đúng cách viết của tag đó.</summary>
    private List<string> ReuseExistingTagNames(long projectId, List<string> tags)
    {
        var existing = _ideaRepo.GetProjectTags(projectId);
        return tags
            .Select(t => existing.FirstOrDefault(e => e.Equals(t, StringComparison.OrdinalIgnoreCase)) ?? t)
            .ToList();
    }

    private Idea GetIdeaOrThrow(long ideaId) =>
        _ideaRepo.GetById(ideaId) ?? throw new InvalidOperationException($"Không tìm thấy Idea #{ideaId}.");

    private ProjectRole EnsureIsProjectMember(long projectId, long userId)
    {
        var role = _memberRepo.GetRole(projectId, userId);
        if (role == null)
            throw new UnauthorizedWorkflowActionException("User không phải thành viên của Project này.");
        return role.Value;
    }
}