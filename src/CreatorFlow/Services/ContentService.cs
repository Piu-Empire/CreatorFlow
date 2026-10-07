using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Nghiệp vụ tạo mới / chỉnh sửa thông tin Content (Status vẫn chỉ đổi qua WorkflowService).
/// Quy tắc:
///   - Phải là thành viên của Project.
///   - Tiêu đề bắt buộc (tối đa 200 ký tự), thời lượng tối đa 30 ký tự.
///   - Không tạo trực tiếp ở giai đoạn Review (Review phải đi qua Submit for Review).
///   - Sửa: Owner/Manager sửa mọi Content; Creator chỉ sửa Content do mình tạo; Content đã Published không được sửa.
/// </summary>
public class ContentService
{
    public static readonly string[] AvailableSprints = { "Sprint 24", "Sprint 25", "Sprint 26" };
    public static readonly string[] AvailablePlatforms = { "YouTube", "TikTok", "Instagram", "Facebook" };

    private readonly IContentRepository _contentRepo;
    private readonly IContentDetailsRepository _detailsRepo;
    private readonly IContentStatusHistoryRepository _historyRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMyTaskRepository _taskRepo;

    public ContentService(
        IContentRepository contentRepo,
        IContentDetailsRepository detailsRepo,
        IContentStatusHistoryRepository historyRepo,
        IProjectMemberRepository memberRepo,
        IUnitOfWork unitOfWork,
        IMyTaskRepository taskRepo)
    {
        _contentRepo = contentRepo;
        _detailsRepo = detailsRepo;
        _historyRepo = historyRepo;
        _memberRepo = memberRepo;
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

    public long Create(long projectId, ContentStatus initialStatus, ContentDraft draft, long userId)
    {
        Normalize(draft);
        Validate(draft);

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

    private static void Normalize(ContentDraft d)
    {
        d.Title = (d.Title ?? string.Empty).Trim();
        d.Description = (d.Description ?? string.Empty).Trim();
        d.EstimatedDuration = (d.EstimatedDuration ?? string.Empty).Trim();
        d.Sprint = string.IsNullOrWhiteSpace(d.Sprint) ? AvailableSprints[1] : d.Sprint.Trim();
        d.Platforms = (d.Platforms ?? new List<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Validate(ContentDraft d)
    {
        if (d.Title.Length == 0)
            throw new ContentValidationException("Vui lòng nhập tiêu đề.");
        if (d.Title.Length > 200)
            throw new ContentValidationException("Tiêu đề tối đa 200 ký tự.");
        if (d.EstimatedDuration.Length > 30)
            throw new ContentValidationException("Thời lượng dự kiến tối đa 30 ký tự (VD: 24 min).");
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


    private ProjectRole EnsureIsProjectMember(long projectId, long userId)
    {
        var role = _memberRepo.GetRole(projectId, userId);
        if (role == null)
            throw new UnauthorizedWorkflowActionException("User không phải thành viên của Project này.");
        return role.Value;
    }
}