using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Nghiệp vụ màn My Tasks.
///   - Đọc: chỉ trả task của đúng User trong đúng Project (User phải là thành viên Project), có lọc/sắp xếp.
///   - Ghi progress: chỉ người được giao (mỗi Creator một assignment riêng), 0–100,
///     tự suy ra trạng thái (0 → Assigned, 1–99 → InProgress, 100 → Completed).
///   - Ghi deadline: chỉ Owner/Manager, đặt riêng cho từng Creator, không được đặt về quá khứ.
/// </summary>
public class MyTaskService
{
    private readonly IMyTaskRepository _taskRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Func<DateTime> _today;

    /// <param name="today">Chỉ để unit test cố định "hôm nay"; mặc định là DateTime.Today.</param>
    public MyTaskService(
        IMyTaskRepository taskRepo,
        IProjectMemberRepository memberRepo,
        IUnitOfWork unitOfWork,
        Func<DateTime>? today = null)
    {
        _taskRepo = taskRepo;
        _memberRepo = memberRepo;
        _unitOfWork = unitOfWork;
        _today = today ?? (() => DateTime.Today);
    }

    // ==========================================================
    // Đọc
    // ==========================================================

    /// <summary>Toàn bộ task của User trong Project (chưa lọc). Không phải thành viên → danh sách rỗng.</summary>
    public List<MyTaskItem> GetMyTasks(long projectId, long userId)
    {
        if (_memberRepo.GetRole(projectId, userId) is null)
            return new List<MyTaskItem>();

        return _taskRepo.GetMyTasks(projectId, userId);
    }

    /// <summary>Số task chưa hoàn thành (hiện trên badge Sidebar).</summary>
    public int CountOpenTasks(long projectId, long userId) =>
        GetMyTasks(projectId, userId).Count(t => t.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress);

    /// <summary>Số task đang quá hạn của User trong Project.</summary>
    public int CountOverdueTasks(long projectId, long userId)
    {
        DateTime today = _today();
        return GetMyTasks(projectId, userId).Count(t => t.IsOverdueOn(today));
    }

    /// <summary>true nếu User có quyền đổi deadline trong Project (Owner/Manager).</summary>
    public bool CanChangeDeadline(long projectId, long userId) =>
        TaskRules.CanChangeDeadline(_memberRepo.GetRole(projectId, userId));

    public static List<MyTaskItem> ApplyFilter(IEnumerable<MyTaskItem> tasks, MyTaskFilter filter, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(filter);

        DateTime day = today.Date;
        IEnumerable<MyTaskItem> query = tasks;

        if (filter.Status is AssignmentStatus status)
            query = query.Where(t => t.Status == status);

        query = filter.Deadline switch
        {
            MyTaskDeadlineFilter.Overdue => query.Where(t => t.IsOverdueOn(day)),
            MyTaskDeadlineFilter.Today => query.Where(t => t.Deadline.HasValue && t.Deadline.Value.Date == day),
            MyTaskDeadlineFilter.Next7Days => query.Where(t =>
                t.Deadline.HasValue && t.Deadline.Value.Date >= day && t.Deadline.Value.Date <= day.AddDays(6)),
            MyTaskDeadlineFilter.NoDeadline => query.Where(t => !t.Deadline.HasValue),
            _ => query,
        };

        return query
            .OrderBy(t => t.Status == AssignmentStatus.Completed ? 1 : 0)
            .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.ContentId)
            .ToList();
    }

    // ==========================================================
    // Ghi
    // ==========================================================

    /// <summary>
    /// Creator cập nhật tiến độ công việc của CHÍNH MÌNH (mỗi người một assignment). Trả về task sau khi cập nhật.
    /// Ném ContentValidationException (progress ngoài 0–100, Content đã Published) hoặc
    /// UnauthorizedWorkflowActionException (không phải thành viên / không được giao Content này).
    /// </summary>
    public MyTaskItem UpdateProgress(long contentId, int percent, long userId)
    {
        TaskRules.ValidateProgress(percent);

        MyTaskItem task = _taskRepo.GetAssignment(contentId, userId)
            ?? throw new UnauthorizedWorkflowActionException("Bạn không được giao công việc này.");
        EnsureIsProjectMember(task.ProjectId, userId);

        if (task.ContentStage == ContentStatus.Published)
            throw new ContentValidationException("Nội dung đã Published nên không thể đổi tiến độ.");

        AssignmentStatus newStatus = TaskRules.StatusForProgress(percent);
        if (task.ProgressPercent == percent && task.Status == newStatus)
            return task;

        _unitOfWork.Begin();
        try
        {
            _taskRepo.UpdateProgress(task.AssignmentId, percent, newStatus);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }

        task.ProgressPercent = percent;
        task.Status = newStatus;
        return task;
    }

    /// <summary>
    /// Đổi (hoặc bỏ, khi null) deadline RIÊNG của một Creator trên một Content.
    /// Chỉ Owner/Manager của Project chứa Content được phép.
    /// </summary>
    public MyTaskItem ChangeDeadline(long contentId, long assigneeUserId, DateTime? newDeadline, long actorUserId)
    {
        MyTaskItem task = _taskRepo.GetAssignment(contentId, assigneeUserId)
            ?? throw new InvalidOperationException($"Không tìm thấy công việc của User #{assigneeUserId} trên Content #{contentId}.");
        ProjectRole role = EnsureIsProjectMember(task.ProjectId, actorUserId);

        if (!TaskRules.CanChangeDeadline(role))
            throw new UnauthorizedWorkflowActionException("Chỉ Owner/Manager mới được đổi deadline.");

        if (task.ContentStage == ContentStatus.Published)
            throw new ContentValidationException("Nội dung đã Published nên không thể đổi deadline.");

        TaskRules.ValidateNewDeadline(newDeadline, task.Deadline, _today());

        DateTime? normalized = newDeadline?.Date;
        if (normalized == task.Deadline?.Date)
            return task;

        _unitOfWork.Begin();
        try
        {
            _taskRepo.UpdateAssignmentDeadline(task.AssignmentId, normalized);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }

        task.Deadline = normalized;
        return task;
    }

    private ProjectRole EnsureIsProjectMember(long projectId, long userId)
    {
        var role = _memberRepo.GetRole(projectId, userId);
        if (role is null)
            throw new UnauthorizedWorkflowActionException("User không phải thành viên của Project này.");
        return role.Value;
    }
}