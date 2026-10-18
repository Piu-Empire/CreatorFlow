namespace CreatorFlow.Services;

/// <summary>
/// Giữ tạm thông tin User/Project đang thao tác trong phiên làm việc.
/// ProjectContext đồng bộ snapshot đã được API xác minh để tương thích module legacy.
/// Hiện tại BoardForm/ReviewQueueForm đọc trực tiếp từ đây để biết ProjectId/UserId hiện hành.
/// </summary>
public static class CurrentSession
{
    public static long CurrentUserId { get; set; }
    public static string CurrentUserName { get; set; } = string.Empty;
    public static long CurrentProjectId { get; set; }
    public static string CurrentProjectName { get; set; } = string.Empty;
    public static CreatorFlow.Models.Enums.ProjectRole? CurrentUserRole { get; internal set; }
}
