using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Quy tắc phân quyền và trạng thái cho Idea Bank. Service và UI cùng gọi vào đây để không mỗi nơi tự định nghĩa một kiểu.
///   - Mọi thành viên của Project xem được Idea của Project.
///   - Owner/Manager thêm, sửa, xóa mọi Idea; Creator thêm Idea mới và chỉ sửa/xóa Idea do chính mình tạo.
///   - Idea đã Converted (đã tạo Content) không xóa được và không đổi trạng thái thủ công;
///     trạng thái Converted chỉ được đặt bởi chức năng "Chuyển Idea thành Content", không chọn tay.
/// </summary>
public static class IdeaRules
{
    /// <summary>Các trạng thái người dùng được chọn tay khi thêm/sửa Idea.</summary>
    public static readonly IdeaStatus[] SelectableStatuses = { IdeaStatus.Draft, IdeaStatus.Backlog, IdeaStatus.Archived };

    public static bool CanCreate(ProjectRole? role) => role is not null;

    public static bool CanEdit(ProjectRole? role, Idea idea, long userId) => role switch
    {
        ProjectRole.Owner or ProjectRole.Manager => true,
        ProjectRole.Creator => idea.CreatedByUserId == userId,
        _ => false,
    };

    public static bool CanDelete(ProjectRole? role, Idea idea, long userId) =>
        idea.Status != IdeaStatus.Converted && CanEdit(role, idea, userId);

    /// <summary>
    /// Kiểm tra việc đổi trạng thái thủ công. <paramref name="current"/> = null khi tạo mới.
    /// Không cho đặt tay Converted, và không cho đưa Idea đã Converted về trạng thái khác.
    /// </summary>
    public static void ValidateStatusChange(IdeaStatus? current, IdeaStatus requested)
    {
        if (!Enum.IsDefined(requested))
            throw new IdeaValidationException("Trạng thái Idea không hợp lệ.");

        if (current == IdeaStatus.Converted)
        {
            if (requested != IdeaStatus.Converted)
                throw new IdeaValidationException("Idea đã chuyển thành Content nên không đổi được trạng thái.");
            return;
        }

        if (requested == IdeaStatus.Converted)
            throw new IdeaValidationException("Trạng thái Converted chỉ được đặt khi chuyển Idea thành Content.");
    }

    /// <summary>Tên trạng thái hiển thị trên giao diện.</summary>
    public static string StatusLabel(IdeaStatus status) => status switch
    {
        IdeaStatus.Draft => "Nháp",
        IdeaStatus.Backlog => "Backlog",
        IdeaStatus.Converted => "Đã chuyển Content",
        IdeaStatus.Archived => "Lưu trữ",
        _ => status.ToString(),
    };
}
