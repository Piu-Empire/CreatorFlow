using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;

namespace CreatorFlow.Controls;

/// <summary>Chế độ xem của Calendar.</summary>
public enum CalendarView
{
    Month,
    Week,
    Day,
}

/// <summary>Một chip Content trên một ô ngày (suy từ deadline hoặc publish date).</summary>
public sealed record CalendarChip(
    long ContentId,
    string Code,
    string Title,
    DateTime Date,
    bool IsPublish,
    ContentStatus Status,
    bool IsOverdue)
{
    /// <summary>Nhãn chip theo quy tắc AC: tiền tố chữ, không đoán bằng màu.</summary>
    public string Label => (IsPublish ? "[Đăng] " : "[Hạn] ") + Code + " · " + Title;
}

/// <summary>Hàm ngày thuần cho Calendar, không chạm UI/DB để unit test được.</summary>
public static class CalendarDateLogic
{
    /// <summary>Thứ 2 đầu tuần chứa ngày cho trước (tuần T2–CN).</summary>
    public static DateTime StartOfWeek(DateTime day)
    {
        int offset = ((int)day.DayOfWeek + 6) % 7;
        return day.Date.AddDays(-offset);
    }

    /// <summary>Lưới tháng luôn 42 ô, bắt đầu từ Thứ 2 của tuần chứa ngày 1.</summary>
    public static List<DateTime> MonthGrid(int year, int month)
    {
        DateTime start = StartOfWeek(new DateTime(year, month, 1));
        return Enumerable.Range(0, 42).Select(i => start.AddDays(i)).ToList();
    }

    /// <summary>7 ngày của tuần chứa mốc (T2–CN).</summary>
    public static List<DateTime> WeekDays(DateTime anchor) =>
        Enumerable.Range(0, 7).Select(i => StartOfWeek(anchor).AddDays(i)).ToList();

    /// <summary>
    /// Lọc card theo Platform / Status / Creator / Project / từ khóa trước khi dựng lịch (thuần client-side).
    /// Tham số null (hoặc chuỗi rỗng với <paramref name="search"/>) nghĩa là không lọc theo tiêu chí đó.
    /// </summary>
    public static List<ContentBoardCard> ApplyFilters(
        IEnumerable<ContentBoardCard> cards,
        string? platform,
        ContentStatus? status,
        long? creatorUserId,
        long? projectId,
        string? search = null) =>
        cards.Where(c =>
                (platform is null || c.Platforms.Any(p => string.Equals(p, platform, StringComparison.OrdinalIgnoreCase))) &&
                (status is null || c.Status == status) &&
                (creatorUserId is null || c.Assignees.Any(a => a.UserId == creatorUserId)) &&
                (projectId is null || c.ProjectId == projectId) &&
                (string.IsNullOrWhiteSpace(search) ||
                    c.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Title.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    /// <summary>Dựng chip từ cards: deadline → marker sản xuất, publish → marker xuất bản.</summary>
    public static List<CalendarChip> BuildChips(IEnumerable<ContentBoardCard> cards, DateTime today)
    {
        var chips = new List<CalendarChip>();
        foreach (ContentBoardCard card in cards)
        {
            if (card.Deadline.HasValue)
            {
                bool finished = card.Status is ContentStatus.Published or ContentStatus.Archived;
                chips.Add(new CalendarChip(card.ContentId, card.Code, card.Title,
                    card.Deadline.Value.Date, false, card.Status,
                    TaskRules.IsOverdue(card.Deadline, finished, today)));
            }

            if (card.PlannedPublishAt.HasValue)
            {
                chips.Add(new CalendarChip(card.ContentId, card.Code, card.Title,
                    card.PlannedPublishAt.Value.Date, true, card.Status, false));
            }
        }

        return chips;
    }

    /// <summary>Content không có ngày nào thì vào khu "Chưa xếp lịch".</summary>
    public static List<ContentBoardCard> Unscheduled(IEnumerable<ContentBoardCard> cards) =>
        cards.Where(c => !c.Deadline.HasValue && !c.PlannedPublishAt.HasValue)
            .OrderBy(c => c.ContentId).ToList();

    /// <summary>Gom chip theo ngày, mỗi ngày sắp deadline trước, publish sau.</summary>
    public static Dictionary<DateTime, List<CalendarChip>> GroupByDate(IEnumerable<CalendarChip> chips) =>
        chips.GroupBy(c => c.Date)
            .ToDictionary(g => g.Key,
                g => g.OrderBy(c => c.IsPublish).ThenBy(c => c.ContentId).ToList());
}
