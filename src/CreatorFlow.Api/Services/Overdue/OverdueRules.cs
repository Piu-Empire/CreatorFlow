namespace CreatorFlow.Api.Services.Overdue;

/// <summary>
/// Quy tắc phát hiện Content quá hạn (SCRUM-47), tách thành hàm thuần để test không cần DB/HTTP.
/// "Hoàn thành" = đã xuất bản hoặc đã lưu trữ (theo CAL-05/CAL-15 của Content Calendar).
/// </summary>
public static class OverdueRules
{
    /// <summary>Trạng thái coi là đã hoàn thành: Published (đã xuất bản) và Archived (đã lưu trữ).</summary>
    public static bool IsFinished(string status) => status is "Published" or "Archived";

    /// <summary>Quá hạn = có deadline, deadline theo NGÀY đã qua so với hôm nay, và công việc chưa hoàn thành.</summary>
    public static bool IsOverdue(DateOnly? deadline, string status, DateOnly today) =>
        deadline is { } value && !IsFinished(status) && value < today;

    /// <summary>Số ngày đã trễ; 0 nếu chưa quá hạn.</summary>
    public static int DaysLate(DateOnly deadline, DateOnly today) =>
        Math.Max(0, today.DayNumber - deadline.DayNumber);
}
