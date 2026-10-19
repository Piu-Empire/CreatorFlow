using CreatorFlow.Contracts.ContentDetail;
using CreatorFlow.Models;

namespace CreatorFlow.Controls;

/// <summary>
/// Hiển thị Activity timeline của 1 Content (mục 6 UX spec).
/// Chỉ hiển thị, không tự query dữ liệu - Form cha (ContentDetailPanel) gọi LoadActivity().
/// Cột: Thời gian · Sự kiện · Trạng thái (old → new) · Người thực hiện · Ghi chú.
/// </summary>
public partial class ActivityTimelineControl : UserControl
{
    public ActivityTimelineControl()
    {
        InitializeComponent();
    }

    /// <summary>Nạp danh sách sự kiện theo thứ tự tăng dần.</summary>
    public void LoadActivity(IEnumerable<ActivityItem> items)
    {
        listViewActivity.BeginUpdate();
        listViewActivity.Items.Clear();

        foreach (var item in items.OrderBy(i => i.Timestamp))
        {
            var listItem = new ListViewItem(item.Timestamp.ToString("dd/MM/yyyy HH:mm"));
            listItem.SubItems.Add(EventLabel(item));
            listItem.SubItems.Add(StatusText(item));
            listItem.SubItems.Add(item.ActorName);
            listItem.SubItems.Add(string.IsNullOrWhiteSpace(item.Feedback) ? string.Empty : item.Feedback!);
            listViewActivity.Items.Add(listItem);
        }

        listViewActivity.EndUpdate();

        if (listViewActivity.Items.Count > 0)
            listViewActivity.Items[listViewActivity.Items.Count - 1].EnsureVisible();
    }

    public void Clear() => listViewActivity.Items.Clear();

    /// <summary>Nhãn ngắn cho cột Sự kiện; kèm số Review nếu có.</summary>
    private static string EventLabel(ActivityItem item)
    {
        string label = item.Kind switch
        {
            ActivityKinds.ReviewSubmitted => "Gửi duyệt",
            ActivityKinds.ReviewDecided => string.Equals(item.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                ? "Phê duyệt"
                : "Từ chối",
            _ => item.FromStatus is not null
                 && string.Equals(item.FromStatus, item.ToStatus, StringComparison.OrdinalIgnoreCase)
                ? "Tạo nội dung"
                : "Đổi trạng thái",
        };

        return item.ReviewNo is int reviewNo ? $"{label} #{reviewNo}" : label;
    }

    /// <summary>"Old → New"; dữ liệu cũ không có field cấu trúc thì dùng Description.</summary>
    private static string StatusText(ActivityItem item)
    {
        if (string.IsNullOrWhiteSpace(item.FromStatus) || string.Equals(item.FromStatus, item.ToStatus, StringComparison.OrdinalIgnoreCase))
            return item.ToStatus ?? item.Description;

        return $"{item.FromStatus} → {item.ToStatus}";
    }
}
