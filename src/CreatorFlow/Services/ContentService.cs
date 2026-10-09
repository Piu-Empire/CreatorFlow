using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Nghiệp vụ tạo mới / chỉnh sửa thông tin Content (Status vẫn chỉ đổi qua WorkflowService).
/// Quy tắc:
///   - Phải là thành viên của Project.
///   - Tiêu đề bắt buộc (tối đa 200 ký tự), mô tả tối đa 2000 ký tự, kịch bản tối đa 100000 ký tự, thời lượng tối đa 30 ký tự.
///   - Loại nội dung phải nằm trong AvailableContentTypes (để trống = loại mặc định); ưu tiên phải hợp lệ.
///   - Ngày dự kiến đăng (tùy chọn) không được sớm hơn hạn hoàn thành (Deadline).
///   - Không tạo trực tiếp ở giai đoạn Review (Review phải đi qua Submit for Review).
///   - Sửa: Owner/Manager sửa mọi Content; Creator chỉ sửa Content do mình tạo; Content đã Published không được sửa.
///   - Platform: chọn nhiều, phải tồn tại và đang active trong bảng platforms; không được bỏ platform đã PUBLISHED.
/// </summary>
public class ContentService
{
    public static readonly string[] AvailableSprints = { "Sprint 24", "Sprint 25", "Sprint 26" };

    /// <summary>Các loại nội dung chọn được (cột contents.content_type là VARCHAR tự do nên danh sách do ứng dụng quản lý).</summary>
    public static readonly string[] AvailableContentTypes =
        { "Short video", "Long video", "Reel", "Livestream", "Image post", "Story", "Article" };

    public const string DefaultContentType = "Short video";

    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxScriptLength = ScriptService.MaxScriptLength; // dùng chung giới hạn với ScriptService (SCRUM-33)
    public const int MaxDurationLength = 30;

    private readonly IContentRepository _contentRepo;
    private readonly IContentDetailsRepository _detailsRepo;
    private readonly IContentStatusHistoryRepository _historyRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IPlatformRepository _platformRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMyTaskRepository _taskRepo;

    public ContentService(
        IContentRepository contentRepo,
        IContentDetailsRepository detailsRepo,
        IContentStatusHistoryRepository historyRepo,
        IProjectMemberRepository memberRepo,
        IPlatformRepository platformRepo,
        IUnitOfWork unitOfWork,
        IMyTaskRepository taskRepo)
    {
        _contentRepo = contentRepo;
        _detailsRepo = detailsRepo;
        _historyRepo = historyRepo;
        _memberRepo = memberRepo;
        _platformRepo = platformRepo;
        _unitOfWork = unitOfWork;
        _taskRepo = taskRepo;
    }


    /// <summary>
    /// Danh sách người có thể chọn làm người phụ trách:
    ///   - Owner/Manager: mọi Creator của Project.
    ///   - Creator: chỉ chính mình (tự nhận việc của nội dung mình tạo).
    ///   - Người ngoài Project: rỗng.
    /// </summary>
    public List<ProjectMemberInfo> GetAssignableMembers(long projectId, long actorUserId)
    {
        var role = _memberRepo.GetRole(projectId, actorUserId);
        var members = _memberRepo.GetMembers(projectId);

        if (TaskRules.CanAssign(role))
            return members.Where(m => TaskRules.CanBeAssignee(m.Role)).ToList();
        if (role == ProjectRole.Creator)
            return members.Where(m => m.UserId == actorUserId).ToList();
        return new List<ProjectMemberInfo>();
    }

    /// <summary>Mọi assignment đang hiệu lực (không Cancelled) của Content — dùng để nạp hộp thoại giao việc.</summary>
    public List<MyTaskItem> GetAssignments(long contentId) => _taskRepo.GetAssignments(contentId);

    /// <summary>
    /// Owner/Manager giao Content cho MỘT HOẶC NHIỀU Creator thuộc đúng Project của Content.
    /// <paramref name="assignments"/> là danh sách người nhận SAU KHI giao (mỗi người một deadline riêng, null = giữ nguyên):
    ///   - người mới trong danh sách  → tạo assignment mới (Assigned, 0%);
    ///   - người đã được giao từ trước → giữ nguyên tiến độ, chỉ cập nhật deadline nếu có thay đổi;
    ///   - người không còn trong danh sách → huỷ assignment (không huỷ được người đã hoàn thành).
    /// </summary>
    public void AssignContent(long contentId, IReadOnlyList<AssignmentRequest> assignments, long actorUserId)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        var content = _contentRepo.GetById(contentId)
            ?? throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");

        var role = EnsureIsProjectMember(content.ProjectId, actorUserId);
        if (!TaskRules.CanAssign(role))
            throw new UnauthorizedWorkflowActionException("Chỉ Owner/Manager mới được giao Content cho Creator.");

        if (content.Status is ContentStatus.Published or ContentStatus.Archived)
            throw new ContentValidationException("Nội dung đã Published/Archived nên không thể giao việc.");

        if (assignments.Count == 0)
            throw new ContentValidationException("Hãy chọn ít nhất một Creator để giao việc.");
        if (assignments.Select(a => a.UserId).Distinct().Count() != assignments.Count)
            throw new ContentValidationException("Một Creator chỉ được xuất hiện một lần trong danh sách giao việc.");

        var existing = _taskRepo.GetAssignments(contentId);
        DateTime today = DateTime.Today;

        // Kiểm tra toàn bộ trước khi ghi gì xuống.
        foreach (var request in assignments)
        {
            EnsureAssignable(content.ProjectId, request.UserId);
            var current = existing.FirstOrDefault(e => e.AssigneeUserId == request.UserId);
            TaskRules.ValidateNewDeadline(request.Deadline, current?.Deadline, today);
        }

        var removed = existing.Where(e => assignments.All(a => a.UserId != e.AssigneeUserId)).ToList();
        foreach (var gone in removed.Where(e => e.Status == AssignmentStatus.Completed))
        {
            string name = _memberRepo.GetMembers(content.ProjectId).FirstOrDefault(m => m.UserId == gone.AssigneeUserId)?.Name
                          ?? $"User #{gone.AssigneeUserId}";
            throw new ContentValidationException($"{name} đã hoàn thành công việc nên không thể bỏ giao.");
        }

        _unitOfWork.Begin();
        try
        {
            foreach (var gone in removed)
                _taskRepo.CancelAssignment(gone.AssignmentId);

            foreach (var request in assignments)
            {
                var current = existing.FirstOrDefault(e => e.AssigneeUserId == request.UserId);
                if (current is null)
                {
                    _taskRepo.AddAssignment(contentId, request.UserId, actorUserId, request.Deadline?.Date);
                }
                else if (request.Deadline.HasValue && request.Deadline.Value.Date != current.Deadline?.Date)
                {
                    _taskRepo.UpdateAssignmentDeadline(current.AssignmentId, request.Deadline.Value.Date);
                }
            }

            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    /// <summary>Tên các nền tảng đang bật trong bảng platforms — nguồn cho checkbox chọn platform.</summary>
    public List<string> GetAvailablePlatforms() => _platformRepo.GetActive().Select(p => p.Name).ToList();

    /// <summary>
    /// Đọc đầy đủ thông tin lập kế hoạch của Content để hiển thị ở Content Detail.
    /// Chỉ thành viên của Project chứa Content mới được xem.
    /// </summary>
    public Content GetDetail(long contentId, long userId)
    {
        var content = _detailsRepo.GetDetail(contentId)
            ?? throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");

        EnsureIsProjectMember(content.ProjectId, userId);
        return content;
    }

    /// <summary>
    /// Kiểm tra dữ liệu nhập, trả về thông báo lỗi đầu tiên (hoặc null nếu hợp lệ) mà không ném exception.
    /// Form dùng hàm này để báo lỗi ngay trong dialog và giữ nguyên dữ liệu người dùng đã nhập.
    /// Hàm này KHÔNG sửa <paramref name="draft"/>: gọi <see cref="Normalize"/> trước nếu draft đến từ ô nhập của người dùng.
    /// </summary>
    public static string? GetValidationError(ContentDraft draft)
    {
        if (draft.Title.Length == 0)
            return "Vui lòng nhập tiêu đề.";
        if (draft.Title.Length > MaxTitleLength)
            return $"Tiêu đề tối đa {MaxTitleLength} ký tự.";
        if (draft.Description.Length > MaxDescriptionLength)
            return $"Mô tả tối đa {MaxDescriptionLength} ký tự.";
        if (draft.Script.Length > MaxScriptLength)
            return $"Kịch bản tối đa {MaxScriptLength} ký tự.";
        if (!AvailableContentTypes.Contains(draft.ContentType, StringComparer.Ordinal))
            return $"Loại nội dung '{draft.ContentType}' không hợp lệ.";
        if (!Enum.IsDefined(draft.Priority))
            return "Mức độ ưu tiên không hợp lệ.";
        if (draft.EstimatedDuration.Length > MaxDurationLength)
            return $"Thời lượng dự kiến tối đa {MaxDurationLength} ký tự (VD: 24 min).";
        if (draft.Deadline.HasValue && draft.PlannedPublishAt.HasValue && draft.PlannedPublishAt.Value < draft.Deadline.Value)
            return "Ngày dự kiến đăng không được sớm hơn hạn hoàn thành.";

        return null;
    }

    public long Create(long projectId, ContentStatus initialStatus, ContentDraft draft, long userId)
    {
        Normalize(draft);
        Validate(draft);
        ResolvePlatforms(draft);

        if (initialStatus == ContentStatus.Review)
            throw new ContentValidationException("Không thể tạo trực tiếp ở giai đoạn Review. Hãy tạo ở Editing rồi Submit Review.");

        var role = EnsureIsProjectMember(projectId, userId);

        if (draft.AssigneeUserId is long assigneeId)
        {
            // Creator chỉ được tự nhận việc của nội dung mình tạo; giao cho người khác là việc của Owner/Manager.
            if (!TaskRules.CanAssign(role) && assigneeId != userId)
                throw new UnauthorizedWorkflowActionException("Chỉ Owner/Manager mới được giao Content cho người khác.");

            EnsureAssignable(projectId, assigneeId);
        }

        _unitOfWork.Begin();
        try
        {
            long id = _detailsRepo.Create(projectId, initialStatus, draft, userId);

            _historyRepo.Add(new ContentStatusHistory
            {
                ContentId = id,
                FromStatus = initialStatus,
                ToStatus = initialStatus,
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow,
                Note = "Tạo nội dung mới trên Production Board",
            });

            _unitOfWork.Commit();
            return id;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public void Update(long contentId, ContentDraft draft, long userId)
    {
        Normalize(draft);
        Validate(draft);
        ResolvePlatforms(draft);

        var content = _contentRepo.GetById(contentId)
            ?? throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");

        var role = EnsureIsProjectMember(content.ProjectId, userId);

        if (content.Status == ContentStatus.Published)
            throw new ContentValidationException("Nội dung đã Published nên không thể chỉnh sửa.");

        if (role == ProjectRole.Creator && content.CreatedByUserId != userId)
            throw new UnauthorizedWorkflowActionException("Creator chỉ được sửa nội dung do chính mình tạo.");

        // Phân quyền deadline: chỉ Owner/Manager được đổi (so theo ngày). Creator gửi lại đúng deadline cũ thì vẫn lưu được.
        if (draft.Deadline?.Date != content.Deadline?.Date)
        {
            if (!TaskRules.CanChangeDeadline(role))
                throw new UnauthorizedWorkflowActionException("Chỉ Owner/Manager mới được đổi deadline.");

            TaskRules.ValidateNewDeadline(draft.Deadline, content.Deadline, DateTime.Today);
        }
        EnsurePublishedPlatformsKept(contentId, draft);

        _unitOfWork.Begin();
        try
        {
            _detailsRepo.Update(contentId, draft);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    /// <summary>Chuẩn hóa dữ liệu nhập trực tiếp trên draft: trim chữ, chọn loại mặc định, bỏ phần giờ của ngày, loại trùng platform.</summary>
    public static void Normalize(ContentDraft d)
    {
        d.Title = (d.Title ?? string.Empty).Trim();
        d.Description = (d.Description ?? string.Empty).Trim();
        d.Script = (d.Script ?? string.Empty).Trim();
        d.ContentType = ResolveContentType(d.ContentType);
        d.Deadline = d.Deadline?.Date;
        d.PlannedPublishAt = d.PlannedPublishAt?.Date;
        d.EstimatedDuration = (d.EstimatedDuration ?? string.Empty).Trim();
        d.Sprint = string.IsNullOrWhiteSpace(d.Sprint) ? AvailableSprints[1] : d.Sprint.Trim();
        d.Platforms = (d.Platforms ?? new List<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Trống → loại mặc định; khớp không phân biệt hoa/thường thì đổi về đúng cách viết trong danh sách; không khớp thì giữ nguyên để Validate báo lỗi.</summary>
    private static string ResolveContentType(string? contentType)
    {
        string trimmed = (contentType ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return DefaultContentType;

        return AvailableContentTypes.FirstOrDefault(t => t.Equals(trimmed, StringComparison.OrdinalIgnoreCase)) ?? trimmed;
    }

    private static void Validate(ContentDraft d)
    {
        string? error = GetValidationError(d);
        if (error != null)
            throw new ContentValidationException(error);
    }

    /// <summary>Người nhận phải thuộc đúng Project và có vai trò Creator.</summary>
    private void EnsureAssignable(long projectId, long assigneeUserId)
    {
        var assigneeRole = _memberRepo.GetRole(projectId, assigneeUserId);
        if (assigneeRole is null)
            throw new ContentValidationException("Người được giao không thuộc Project này.");
        if (!TaskRules.CanBeAssignee(assigneeRole))
            throw new ContentValidationException("Chỉ có thể giao Content cho thành viên có vai trò Creator.");
    }

    /// <summary>
    /// Mỗi platform phải tồn tại và đang active trong bảng platforms. Đổi tên về đúng cách viết trong DB
    /// (VD "youtube" → "YouTube") để Repository so khớp chính xác.
    /// </summary>
    private void ResolvePlatforms(ContentDraft d)
    {
        var active = _platformRepo.GetActive();
        var resolved = new List<string>();

        foreach (string name in d.Platforms)
        {
            var platform = active.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new ContentValidationException($"Nền tảng '{name}' không tồn tại hoặc đã ngừng hoạt động.");
            resolved.Add(platform.Name);
        }

        d.Platforms = resolved;
    }

    /// <summary>Platform đã PUBLISHED thì không được bỏ chọn (sẽ mất post_url, published_at và metrics của nó).</summary>
    private void EnsurePublishedPlatformsKept(long contentId, ContentDraft d)
    {
        var removed = _platformRepo.GetByContentId(contentId)
            .Where(cp => cp.PublicationStatus == PublicationStatus.Published
                         && !d.Platforms.Contains(cp.PlatformName, StringComparer.OrdinalIgnoreCase))
            .Select(cp => cp.PlatformName)
            .ToList();

        if (removed.Count > 0)
            throw new ContentValidationException(
                $"Không thể bỏ nền tảng đã đăng: {string.Join(", ", removed)}.");
    }

    private ProjectRole EnsureIsProjectMember(long projectId, long userId)
    {
        var role = _memberRepo.GetRole(projectId, userId);
        if (role == null)
            throw new UnauthorizedWorkflowActionException("User không phải thành viên của Project này.");
        return role.Value;
    }
}