using System.Text.RegularExpressions;
using CreatorFlow.Api.Models.Ideas;
using CreatorFlow.Api.Models.Tasks;

namespace CreatorFlow.Api.Services.Ideas;

/// <summary>
/// Quy tắc Idea Bank phía Backend (cùng quy tắc với Services/IdeaRules.cs + IdeaService.cs của app):
/// Owner/Manager thêm/sửa/xóa mọi Idea; Creator thêm Idea mới và chỉ sửa/xóa Idea của mình; Converted không xóa/đổi tay.
/// Tiêu đề ≤ 250, mô tả/ghi chú ≤ 2000, tối đa 10 tag, mỗi tag ≤ 80 ký tự.
/// </summary>
public static class IdeaRules
{
    public const int MaxTitleLength = 250;
    public const int MaxDescriptionLength = 2000;
    public const int MaxNoteLength = 2000;
    public const int MaxTagCount = 10;
    public const int MaxTagLength = 80;

    // Giới hạn của Content chặt hơn của Idea (contents: tiêu đề 200).
    public const int MaxContentTitleLength = 200;
    public const int MaxContentDescriptionLength = 2000;
    public const string DefaultContentType = "Short video";

    public static bool CanEdit(ProjectRole? role, Idea idea, long userId) => role switch
    {
        ProjectRole.Owner or ProjectRole.Manager => true,
        ProjectRole.Creator => idea.CreatedByUserId == userId,
        _ => false,
    };

    /// <summary>
    /// Đọc tên trạng thái Idea. Chỉ nhận chữ cái ("Draft", "backlog"): Enum.TryParse còn nhận số ("1" → Backlog)
    /// và danh sách ("Draft,Backlog" → một giá trị hợp lệ khác) nên không dùng trực tiếp cho dữ liệu từ client.
    /// </summary>
    public static bool TryParseStatus(string? value, out IdeaStatus status)
    {
        status = default;
        string text = value?.Trim() ?? string.Empty;
        return text.Length > 0 && text.All(char.IsLetter)
            && Enum.TryParse(text, ignoreCase: true, out status) && Enum.IsDefined(status);
    }

    public static string NormalizeTag(string? tag) =>
        Regex.Replace((tag ?? string.Empty).Trim().TrimStart('#').Trim(), @"\s+", " ");

    /// <summary>Trim chữ, chuẩn hóa tag (bỏ '#', gộp khoảng trắng, loại trùng không phân biệt hoa/thường).</summary>
    public static string[] NormalizeTags(IEnumerable<string>? tags) =>
        (tags ?? []).Select(NormalizeTag).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string? Validate(string title, string description, string note, string[] tags) =>
        title.Length == 0 ? "Vui lòng nhập tiêu đề."
        : title.Length > MaxTitleLength ? $"Tiêu đề tối đa {MaxTitleLength} ký tự."
        : description.Length > MaxDescriptionLength ? $"Mô tả tối đa {MaxDescriptionLength} ký tự."
        : note.Length > MaxNoteLength ? $"Ghi chú tối đa {MaxNoteLength} ký tự."
        : tags.Length > MaxTagCount ? $"Tối đa {MaxTagCount} tag cho mỗi Idea."
        : tags.Any(t => t.Length > MaxTagLength) ? $"Mỗi tag tối đa {MaxTagLength} ký tự."
        : null;

    /// <summary>Kiểm tra đổi trạng thái thủ công; current = null khi tạo mới. Trả về thông báo lỗi hoặc null.</summary>
    public static string? ValidateStatusChange(IdeaStatus? current, IdeaStatus requested)
    {
        if (current == IdeaStatus.Converted)
            return requested == IdeaStatus.Converted ? null : "Idea đã chuyển thành Content nên không đổi được trạng thái.";
        return requested == IdeaStatus.Converted ? "Trạng thái Converted chỉ được đặt khi chuyển Idea thành Content." : null;
    }

    public static string? ValidateConvertible(IdeaStatus current) => current switch
    {
        IdeaStatus.Converted => "Idea này đã được chuyển thành Content.",
        IdeaStatus.Archived => "Idea đã lưu trữ. Hãy chuyển về Nháp hoặc Backlog trước khi chuyển thành Content.",
        _ => null,
    };
}
