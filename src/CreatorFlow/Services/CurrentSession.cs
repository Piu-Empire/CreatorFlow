namespace CreatorFlow.Services;

/// <summary>
/// Giữ tạm thông tin User/Project đang thao tác trong phiên làm việc.
/// TODO: thay bằng AuthService/Session thật khi có màn Login + chọn Project (mục 18 UX spec: Login → Select Project).
/// Hiện tại BoardForm/ReviewQueueForm đọc trực tiếp từ đây để biết ProjectId/UserId hiện hành.
/// </summary>
public static class CurrentSession
{
    public static long CurrentUserId { get; set; }
    public static string CurrentUserName { get; set; } = string.Empty;
    public static long CurrentProjectId { get; set; }
    public static string CurrentProjectName { get; set; } = string.Empty;
}