using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services.Backends;

/// <summary>
/// Nơi thực thi nghiệp vụ của drawer Content Detail khi chạy theo mô hình Client–Server:
/// quyền, validation và workflow nằm ở CreatorFlow.Api, client chỉ gọi qua REST.
/// Người thao tác luôn là người đang đăng nhập. Giao việc dùng <see cref="IAssignmentBackend"/>, Metrics dùng <see cref="IMetricBackend"/>.
/// </summary>
public interface IContentDetailBackend
{
    /// <summary>Vai trò của người đang đăng nhập trong Project (null nếu không phải thành viên).</summary>
    ProjectRole? GetMyRole(long projectId);

    /// <summary>Tạo Content mới ở giai đoạn <paramref name="initialStatus"/>, trả về Id mới. Sprint / thời lượng dự kiến chưa có cột trong DB nên chưa được lưu.</summary>
    long Create(long projectId, ContentStatus initialStatus, ContentDraft draft);

    Content GetDetail(long contentId);

    /// <summary>Tên các nền tảng đang bật — nguồn cho menu chọn platform.</summary>
    List<string> GetAvailablePlatforms();

    /// <summary>Lưu thông tin lập kế hoạch (không đổi Status). Sprint / thời lượng dự kiến chưa có cột trong DB nên chưa được lưu.</summary>
    void Update(long contentId, ContentDraft draft);

    ScriptDocument OpenScript(long contentId);

    /// <summary>Lưu script; ném <c>ScriptConflictException</c> nếu script đã bị người khác sửa sau <paramref name="expectedVersion"/>.</summary>
    ScriptDocument SaveScript(long contentId, string? text, string expectedVersion);

    List<ActivityItem> GetActivity(long contentId);

    void ChangeStatus(long contentId, ContentStatus next);

    void SubmitForReview(long contentId);

    void DecideReview(long contentId, bool approve, string? feedback);
}
