using CreatorFlow.Models;

namespace CreatorFlow.Controls;

/// <summary>
/// Hiển thị Activity timeline của 1 Content (mục 6 UX spec).
/// Chỉ hiển thị, không tự query dữ liệu - Form cha (ContentDetailPanel) gọi LoadActivity().
/// </summary>
public partial class ActivityTimelineControl : UserControl
{
    public ActivityTimelineControl()
    {
        InitializeComponent();
    }

    public void LoadActivity(IEnumerable<ActivityItem> items)
    {
        listViewActivity.BeginUpdate();
        listViewActivity.Items.Clear();

        foreach (var item in items.OrderBy(i => i.Timestamp))
        {
            var listItem = new ListViewItem(item.Timestamp.ToString("dd/MM HH:mm"));
            listItem.SubItems.Add(item.ActorName);

            var description = item.Description;
            if (!string.IsNullOrWhiteSpace(item.Feedback))
                description += $" — \"{item.Feedback}\"";

            listItem.SubItems.Add(description);
            listViewActivity.Items.Add(listItem);
        }

        listViewActivity.EndUpdate();

        if (listViewActivity.Items.Count > 0)
            listViewActivity.Items[listViewActivity.Items.Count - 1].EnsureVisible();
    }

    public void Clear() => listViewActivity.Items.Clear();
}