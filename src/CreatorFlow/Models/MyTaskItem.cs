using CreatorFlow.Models.Enums;
using CreatorFlow.Services;

namespace CreatorFlow.Models;

/// <summary>
/// View-model 1 dòng trong màn My Tasks (công việc được giao cho Creator).
/// Gộp dữ liệu từ ContentAssignments (status/progress/deadline giao việc) + Contents (mã, tiêu đề, giai đoạn, ưu tiên).
/// </summary>
public class MyTaskItem
{
    public long AssignmentId { get; set; }
    public long ContentId { get; set; }
    public long ProjectId { get; set; }

    /// <summary>Người được giao việc (chỉ người này được cập nhật progress).</summary>
    public long AssigneeUserId { get; set; }

    /// <summary>Số Creator đang được giao Content này (assignment chưa Cancelled), kể cả người xem.</summary>
    public int TeamTotal { get; set; }

    /// <summary>Số Creator đã Completed trong nhóm.</summary>
    public int TeamDone { get; set; }

    /// <summary>Content hoàn thành khi mọi Creator được giao đều Completed.</summary>
    public bool IsTeamCompleted => TeamTotal > 0 && TeamDone == TeamTotal;

    /// <summary>VD: "CNT-021".</summary>
    public string ContentCode { get; set; } = string.Empty;

    public string ContentTitle { get; set; } = string.Empty;

    /// <summary>Giai đoạn hiện tại của Content trên Board (Idea … Published).</summary>
    public ContentStatus ContentStage { get; set; }

    /// <summary>Trạng thái của chính công việc được giao (Assigned / InProgress / Completed / Cancelled).</summary>
    public AssignmentStatus Status { get; set; }

    public Priority Priority { get; set; }
    public DateTime? Deadline { get; set; }

    /// <summary>0–100.</summary>
    public int ProgressPercent { get; set; }

    public string? AssignedByName { get; set; }
    public List<string> Platforms { get; set; } = new();

    public bool IsOverdue => IsOverdueOn(DateTime.Today);

    /// <summary>Quá hạn = có deadline đã qua và công việc chưa Completed/Cancelled (quy tắc chung ở TaskRules).</summary>
    public bool IsOverdueOn(DateTime today) =>
        TaskRules.IsOverdue(Deadline, Status is AssignmentStatus.Completed or AssignmentStatus.Cancelled, today);
}