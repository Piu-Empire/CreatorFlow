namespace CreatorFlow.Data;

/// <summary>
/// Cấu hình kết nối PostgreSQL (mục 5.7 tài liệu). Đây là bản đơn giản để chạy demo/đồ án —
/// SỬA LẠI Username/Password/Database theo máy bạn trước khi build. Nếu sau này cần đổi theo
/// môi trường (dev/máy khác) mà không sửa code, có thể chuyển sang đọc từ file appsettings.json.
/// </summary>
public static class DbConfig
{
    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=creatorflow;Username=creatorflow_app;Password=CHANGE_ME";
}