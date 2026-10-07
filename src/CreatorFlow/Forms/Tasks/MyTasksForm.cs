using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Tasks;

/// <summary>
/// Màn My Tasks cho Creator: danh sách công việc được giao (status, priority, deadline, progress).
///   - Chỉ hiện task của CurrentSession.CurrentUserId trong CurrentSession.CurrentProjectId (MyTaskService đảm bảo).
///   - Lọc theo trạng thái (Segmented Control) và khoảng deadline (ComboBox).
///   - Creator cập nhật progress; chỉ Owner/Manager đổi được deadline.
///   - "Mở Content": đóng form với DialogResult.OK và <see cref="RequestedContentId"/> — BoardForm mở drawer chi tiết.
/// Giao diện theo CreatorFlow UI Style Guide (Page Header, Card, bảng, Progress bar, Toast, validation dưới ô nhập).
/// Layout tĩnh nằm trong MyTasksForm.Designer.cs, file này chỉ chứa dữ liệu, sự kiện và logic.
/// </summary>
public partial class MyTasksForm : Form
{
    private static readonly (string Label, AssignmentStatus? Value)[] StatusOptions =
    {
        ("Tất cả", null),
        ("Được giao", AssignmentStatus.Assigned),
        ("Đang làm", AssignmentStatus.InProgress),
        ("Hoàn thành", AssignmentStatus.Completed),
    };

    private static readonly (string Label, MyTaskDeadlineFilter Value)[] DeadlineOptions =
    {
        ("Tất cả deadline", MyTaskDeadlineFilter.All),
        ("Quá hạn", MyTaskDeadlineFilter.Overdue),
        ("Hôm nay", MyTaskDeadlineFilter.Today),
        ("7 ngày tới", MyTaskDeadlineFilter.Next7Days),
        ("Chưa có deadline", MyTaskDeadlineFilter.NoDeadline),
    };

    // Chỉ số cột (khớp thứ tự Columns trong Designer)
    private const int ColCode = 0, ColTitle = 1, ColStatus = 2, ColPriority = 3, ColDeadline = 4, ColProgress = 5, ColTeam = 6, ColAssignedBy = 7;

    private const TextFormatFlags CellText =
        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
        TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

    private const TextFormatFlags CenterText =
        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
        TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

    /// <summary>Hover dòng bảng #F0F3FF (style guide mục 11).</summary>
    private static readonly Color RowHover = ColorTranslator.FromHtml("#F0F3FF");

    private readonly MyTaskService _taskService;
    private readonly ToastNotification? _toast;
    private List<MyTaskItem> _allTasks = new();
    private List<MyTaskItem> _visibleTasks = new();
    private bool _canChangeDeadline;
    private int _hoverIndex = -1;
    private string _emptyTitle = string.Empty;
    private string _emptySub = string.Empty;

    /// <summary>Content người dùng chọn mở (null nếu đóng form mà không chọn).</summary>
    public long? RequestedContentId { get; private set; }

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được form. Không dùng khi chạy thật.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public MyTasksForm()
    {
        InitializeComponent();
        _taskService = null!;
        InitFilters();
    }

    public MyTasksForm(MyTaskService taskService)
    {
        InitializeComponent();
        _taskService = taskService;
        InitFilters();

        _toast = new ToastNotification();
        Controls.Add(_toast);

        Load += (_, _) =>
        {
            ResizeColumns();
            _canChangeDeadline = _taskService.CanChangeDeadline(CurrentSession.CurrentProjectId, CurrentSession.CurrentUserId);
            ResetHint();
            ReloadTasks();
        };
    }

    private void InitFilters()
    {
        segStatus.Items.Clear();
        segStatus.Items.AddRange(StatusOptions.Select(o => o.Label));
        segStatus.Width = segStatus.MeasureTotalWidth();

        cboDeadline.Items.AddRange(DeadlineOptions.Select(o => (object)o.Label).ToArray());
        cboDeadline.SelectedIndex = 0;

        LayoutHeader();
    }

    private MyTaskItem? SelectedTask =>
        listViewTasks.SelectedItems.Count > 0 ? listViewTasks.SelectedItems[0].Tag as MyTaskItem : null;

    // ==========================================================
    // Tải & lọc dữ liệu
    // ==========================================================
    private void ReloadTasks()
    {
        if (_taskService is null) return;

        _allTasks = _taskService.GetMyTasks(CurrentSession.CurrentProjectId, CurrentSession.CurrentUserId);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        long? selectedContentId = SelectedTask?.ContentId;

        var filter = new MyTaskFilter
        {
            Status = StatusOptions[Math.Max(0, segStatus.SelectedIndex)].Value,
            Deadline = DeadlineOptions[Math.Max(0, cboDeadline.SelectedIndex)].Value,
        };
        _visibleTasks = MyTaskService.ApplyFilter(_allTasks, filter, DateTime.Today);

        listViewTasks.BeginUpdate();
        listViewTasks.Items.Clear();
        foreach (var task in _visibleTasks)
        {
            var item = new ListViewItem(task.ContentCode) { Tag = task };
            item.SubItems.Add(task.ContentTitle);
            item.SubItems.Add(GetStatusStyle(task.Status).Label);
            item.SubItems.Add(GetPriorityBadge(task.Priority).Label);
            item.SubItems.Add(FormatDeadline(task));
            item.SubItems.Add($"{task.ProgressPercent}%");
            item.SubItems.Add(FormatTeam(task));
            item.SubItems.Add(task.AssignedByName ?? "—");
            item.Selected = selectedContentId == task.ContentId;
            listViewTasks.Items.Add(item);
        }
        listViewTasks.EndUpdate();
        _hoverIndex = -1;

        int overdue = _allTasks.Count(t => t.IsOverdue);
        lblSummary.Text = $"Hiển thị {_visibleTasks.Count}/{_allTasks.Count} công việc • {overdue} quá hạn";
        // Phát hiện overdue: số quá hạn của TOÀN BỘ task (không phụ thuộc bộ lọc) được tô đỏ để dễ thấy.
        lblSummary.ForeColor = overdue > 0 ? UITheme.OverdueText : UITheme.Neutral600;

        bool hasRows = _visibleTasks.Count > 0;
        listViewTasks.Visible = hasRows;
        pnlEmpty.Visible = !hasRows;
        if (_allTasks.Count == 0)
        {
            _emptyTitle = "Chưa có công việc nào";
            _emptySub = "Khi Owner/Manager giao Content cho bạn, công việc sẽ xuất hiện tại đây.";
        }
        else
        {
            _emptyTitle = "Không có công việc nào khớp bộ lọc";
            _emptySub = "Thử đổi trạng thái hoặc deadline để xem thêm.";
        }
        pnlEmpty.Invalidate();

        UpdateEditPanel();
    }

    /// <summary>Đồng bộ khu vực sửa Progress/Deadline và nút Mở Content theo dòng đang chọn + quyền của User.</summary>
    private void UpdateEditPanel()
    {
        var task = SelectedTask;
        bool hasTask = task != null;

        btnOpenContent.Enabled = hasTask;
        nudProgress.Enabled = hasTask;
        btnSaveProgress.Enabled = hasTask;
        dtpDeadline.Enabled = hasTask && _canChangeDeadline;
        btnSaveDeadline.Enabled = hasTask && _canChangeDeadline;

        if (task is null) return;

        nudProgress.Value = Math.Clamp(task.ProgressPercent, 0, 100);
        if (task.Deadline.HasValue)
        {
            dtpDeadline.Value = task.Deadline.Value.Date;
            dtpDeadline.Checked = true;
        }
        else
        {
            dtpDeadline.Value = DateTime.Today;
            dtpDeadline.Checked = false; // bỏ tick = không có deadline
        }
    }

    // ==========================================================
    // Gợi ý / báo lỗi (ngay dưới ô nhập) và Toast thành công
    // ==========================================================
    private string DefaultHint => _canChangeDeadline
        ? "Nhấp đúp một dòng (hoặc chọn rồi bấm “Mở Content”) để xem chi tiết Content trên Board."
        : "Chỉ Owner/Manager được đổi deadline. Nhấp đúp một dòng để mở Content.";

    private void ShowHint(string text, Color color, bool bold = false)
    {
        lblHint.Text = text;
        lblHint.ForeColor = color;
        lblHint.Font = bold ? UITheme.FontLabelBold : UITheme.FontLabel;
    }

    private void ShowError(string message) => ShowHint(message, UITheme.Danger, bold: true);

    private void ResetHint() => ShowHint(DefaultHint, UITheme.Neutral600);

    private void ShowSuccess(string message)
    {
        ResetHint();
        _toast?.ShowToast(this, message);
    }

    // ==========================================================
    // Sự kiện
    // ==========================================================
    private void segStatus_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilter();

    private void cboDeadline_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilter();

    private void btnRefresh_Click(object? sender, EventArgs e) => ReloadTasks();

    private void panelHeader_Resize(object? sender, EventArgs e)
    {
        LayoutHeader();
        panelHeader.Invalidate();
    }

    private void listViewTasks_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ResetHint();
        UpdateEditPanel();
    }

    private void listViewTasks_DoubleClick(object? sender, EventArgs e) => OpenSelectedContent();

    private void listViewTasks_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        e.Handled = e.SuppressKeyPress = true;
        OpenSelectedContent();
    }

    private void listViewTasks_MouseMove(object? sender, MouseEventArgs e)
    {
        int index = listViewTasks.HitTest(e.Location).Item?.Index ?? -1;
        if (index == _hoverIndex) return;

        int previous = _hoverIndex;
        _hoverIndex = index;
        InvalidateRow(previous);
        InvalidateRow(index);
    }

    private void listViewTasks_MouseLeave(object? sender, EventArgs e)
    {
        int previous = _hoverIndex;
        _hoverIndex = -1;
        InvalidateRow(previous);
    }

    private void InvalidateRow(int index)
    {
        if (index >= 0 && index < listViewTasks.Items.Count)
            listViewTasks.Invalidate(listViewTasks.Items[index].Bounds);
    }

    private void btnOpenContent_Click(object? sender, EventArgs e) => OpenSelectedContent();

    private void listViewTasks_SizeChanged(object? sender, EventArgs e) => ResizeColumns();

    private void nudProgress_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        e.Handled = e.SuppressKeyPress = true;
        btnSaveProgress_Click(sender, EventArgs.Empty);
    }

    private void btnSaveProgress_Click(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        if (task is null) return;

        int percent = (int)nudProgress.Value;
        bool wasOverdue = task.IsOverdue;
        int daysLate = TaskRules.DaysLate(task.Deadline, DateTime.Today);

        try
        {
            var updated = _taskService.UpdateProgress(task.ContentId, percent, CurrentSession.CurrentUserId);
            ReloadTasks();

            ShowSuccess(updated.Status == AssignmentStatus.Completed && wasOverdue
                ? $"Đã hoàn thành — trễ {daysLate} ngày so với deadline"
                : $"Đã cập nhật tiến độ {percent}% — {GetStatusStyle(updated.Status).Label}");
        }
        catch (ContentValidationException ex)
        {
            ShowError(ex.Message);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void btnSaveDeadline_Click(object? sender, EventArgs e)
    {
        var task = SelectedTask;
        if (task is null) return;

        DateTime? newDeadline = dtpDeadline.Checked ? dtpDeadline.Value.Date : null;

        try
        {
            var updated = _taskService.ChangeDeadline(task.ContentId, task.AssigneeUserId, newDeadline, CurrentSession.CurrentUserId);
            ReloadTasks();

            ShowSuccess(updated.Deadline.HasValue
                ? $"Đã đổi deadline thành {updated.Deadline.Value:dd/MM/yyyy}"
                : "Đã bỏ deadline");
        }
        catch (ContentValidationException ex)
        {
            ShowError(ex.Message);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenSelectedContent()
    {
        var task = SelectedTask;
        if (task is null) return;

        RequestedContentId = task.ContentId;
        DialogResult = DialogResult.OK; // đóng form (ShowDialog) — BoardForm đọc RequestedContentId để mở Content
    }

    /// <summary>Cột Tiêu đề co giãn lấp phần còn lại, các cột khác giữ chiều rộng cố định.</summary>
    private void ResizeColumns()
    {
        int others = 0;
        foreach (ColumnHeader c in listViewTasks.Columns)
            if (c != columnTitle) others += c.Width;

        int width = Math.Max(200, listViewTasks.ClientSize.Width - others);
        if (columnTitle.Width != width) columnTitle.Width = width;
    }

    /// <summary>Thanh công cụ canh phải theo thứ tự: bộ lọc nhanh → ComboBox → nút phụ (style guide mục 26).</summary>
    private void LayoutHeader()
    {
        int width = panelHeader.ClientSize.Width;
        if (width <= 0) return;

        int right = width;
        btnRefresh.Location = new Point(right - btnRefresh.Width, 0);
        right -= btnRefresh.Width + 12;

        cboDeadline.Location = new Point(right - cboDeadline.Width, Math.Max(0, (btnRefresh.Height - cboDeadline.Height) / 2));
        right -= cboDeadline.Width + 12;

        segStatus.Location = new Point(right - segStatus.Width, Math.Max(0, (btnRefresh.Height - segStatus.Height) / 2));

        lblSummary.Location = new Point(width - lblSummary.Width, 48);
    }

    // ==========================================================
    // Vẽ tay (Page Header, empty state, header bảng, dòng bảng, badge, thanh tiến độ)
    // ==========================================================
    private void panelHeader_Paint(object? sender, PaintEventArgs e)
    {
        const string title = "My Tasks";
        const string badge = "Được giao cho tôi";
        const string description = "Công việc được giao cho bạn: theo dõi trạng thái, tiến độ và deadline.";

        var g = e.Graphics;
        TextRenderer.DrawText(g, title, UITheme.FontPageTitle, new Point(0, 0), UITheme.Ink, TextFormatFlags.NoPadding);

        int titleWidth = TextRenderer.MeasureText(g, title, UITheme.FontPageTitle, new Size(int.MaxValue, 40), TextFormatFlags.NoPadding).Width;
        int badgeWidth = TextRenderer.MeasureText(g, badge, UITheme.FontLabelBold, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding).Width + 24;
        UITheme.DrawBadge(g, badge, UITheme.Neutral200, UITheme.Neutral700, new Rectangle(titleWidth + 12, 8, badgeWidth, 24));

        TextRenderer.DrawText(g, description, UITheme.FontBody, new Point(0, 50), UITheme.Neutral600, TextFormatFlags.NoPadding);
    }

    private void pnlEmpty_Paint(object? sender, PaintEventArgs e)
    {
        var area = pnlEmpty.ClientRectangle;
        const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

        TextRenderer.DrawText(e.Graphics, _emptyTitle, UITheme.FontH3,
            new Rectangle(0, area.Height / 2 - 30, area.Width, 28), UITheme.Ink, flags);
        TextRenderer.DrawText(e.Graphics, _emptySub, UITheme.FontBody,
            new Rectangle(0, area.Height / 2 + 2, area.Width, 24), UITheme.Neutral600, flags);
    }

    private void listViewTasks_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        var g = e.Graphics;
        using (var back = new SolidBrush(UITheme.Neutral50))
            g.FillRectangle(back, e.Bounds);
        using (var pen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

        // Header bảng: nền #FAFAFA, chữ viết hoa 11px đậm (mục 11).
        string text = (e.Header?.Text ?? string.Empty).ToUpperInvariant();
        TextRenderer.DrawText(g, text, UITheme.FontLabelBold, Rectangle.Inflate(e.Bounds, -10, 0), UITheme.Neutral600, CellText);
    }

    private void listViewTasks_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item?.Tag is not MyTaskItem task) return;

        var g = e.Graphics;
        var b = e.Bounds;

        // Dòng chẵn tô #FAFAFA, hover/đang chọn tô #F0F3FF; đường kẻ giữa các dòng 1px #E5E5E5 (mục 7, 11).
        Color rowBack = e.Item.Selected || e.ItemIndex == _hoverIndex ? RowHover
                      : e.ItemIndex % 2 == 1 ? UITheme.Neutral50
                      : UITheme.White;
        using (var back = new SolidBrush(rowBack))
            g.FillRectangle(back, b);
        using (var pen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(pen, b.Left, b.Bottom - 1, b.Right, b.Bottom - 1);

        var textRect = Rectangle.Inflate(b, -10, 0);
        bool done = task.Status == AssignmentStatus.Completed;

        switch (e.ColumnIndex)
        {
            case ColCode:
                TextRenderer.DrawText(g, task.ContentCode, UITheme.FontMono, textRect, UITheme.Neutral700, CellText);
                break;

            case ColTitle:
                TextRenderer.DrawText(g, task.ContentTitle, UITheme.FontBodyBold, textRect,
                    done ? UITheme.Neutral600 : UITheme.Ink, CellText);
                break;

            case ColStatus:
                {
                    var (bg, fg, label) = GetStatusStyle(task.Status);
                    DrawBadgeInCell(g, b, label, bg, fg);
                    break;
                }

            case ColPriority:
                {
                    var (bg, fg, label) = GetPriorityBadge(task.Priority);
                    DrawBadgeInCell(g, b, label, bg, fg);
                    break;
                }

            case ColDeadline:
                {
                    Color color = task.IsOverdue ? UITheme.OverdueText
                                : task.Deadline.HasValue ? UITheme.Neutral700
                                : UITheme.Neutral400;
                    var font = task.IsOverdue ? UITheme.FontBodyBold : UITheme.FontBody;
                    TextRenderer.DrawText(g, FormatDeadline(task), font, textRect, color, CellText);
                    break;
                }

            case ColProgress:
                DrawProgress(g, b, task);
                break;

            case ColTeam:
                // "2/3" = số Creator đã hoàn thành / tổng số Creator cùng làm Content này; Content xong khi đủ 3/3.
                TextRenderer.DrawText(g, FormatTeam(task), task.IsTeamCompleted ? UITheme.FontBodyBold : UITheme.FontBody, textRect,
                    task.IsTeamCompleted ? UITheme.Ink : UITheme.Neutral700, CellText);
                break;

            case ColAssignedBy:
                TextRenderer.DrawText(g, task.AssignedByName ?? "—", UITheme.FontBody, textRect, UITheme.Neutral600, CellText);
                break;
        }
    }

    /// <summary>Badge trong ô bảng: bo pill, padding ngang 8px (mục 7, 9).</summary>
    private static void DrawBadgeInCell(Graphics g, Rectangle cell, string text, Color bg, Color fg)
    {
        int w = TextRenderer.MeasureText(g, text, UITheme.FontMicro, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding).Width + 16;
        var rect = new Rectangle(cell.Left + 10, cell.Top + (cell.Height - 20) / 2, w, 20);
        UITheme.DrawBadge(g, text, bg, fg, rect);
    }

    /// <summary>Progress bar (mục 19): cao 8px, nền #F5F5F5 bo pill, phần đã đầy màu đen mặc định.</summary>
    private static void DrawProgress(Graphics g, Rectangle cell, MyTaskItem task)
    {
        const int percentW = 44;
        int percent = Math.Clamp(task.ProgressPercent, 0, 100);

        var track = new Rectangle(cell.Left + 10, cell.Top + (cell.Height - 8) / 2, cell.Width - 20 - percentW, 8);
        if (track.Width < 8) return;

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using (var trackPath = UITheme.CreateRoundedRectanglePath(track, 4))
        using (var trackBrush = new SolidBrush(UITheme.Neutral100))
            g.FillPath(trackBrush, trackPath);

        int fillW = track.Width * percent / 100;
        if (fillW > 0)
        {
            var fill = new Rectangle(track.X, track.Y, Math.Max(fillW, 8), track.Height);
            using var fillPath = UITheme.CreateRoundedRectanglePath(fill, 4);
            using var fillBrush = new SolidBrush(UITheme.Black);
            g.FillPath(fillBrush, fillPath);
        }

        TextRenderer.DrawText(g, $"{percent}%", UITheme.FontLabelBold,
            new Rectangle(track.Right + 6, cell.Top, percentW - 6, cell.Height), UITheme.Neutral700, CellText);
    }

    // ==========================================================
    // Định dạng hiển thị
    // ==========================================================
    private static (Color Bg, Color Text, string Label) GetStatusStyle(AssignmentStatus status) => status switch
    {
        AssignmentStatus.Assigned => (UITheme.Neutral100, UITheme.Neutral700, "ĐƯỢC GIAO"),
        AssignmentStatus.InProgress => (Color.FromArgb(219, 234, 254), UITheme.FacebookText, "ĐANG LÀM"),
        AssignmentStatus.Completed => (Color.FromArgb(209, 250, 229), Color.FromArgb(4, 120, 87), "HOÀN THÀNH"),
        _ => (UITheme.Neutral100, UITheme.Neutral600, "ĐÃ HUỶ"),
    };

    // UITheme.GetPriorityStyle chưa có màu riêng cho Urgent (rơi vào LOW) nên xử lý Urgent ở đây.
    private static (Color Bg, Color Text, string Label) GetPriorityBadge(Priority priority) =>
        priority == Priority.Urgent
            ? (UITheme.Danger, UITheme.White, "URGENT")
            : UITheme.GetPriorityStyle(priority);

    private static string FormatTeam(MyTaskItem task) =>
        task.TeamTotal > 1 ? $"{task.TeamDone}/{task.TeamTotal}" : "—";

    private static string FormatDeadline(MyTaskItem task)
    {
        if (!task.Deadline.HasValue) return "—";

        string date = task.Deadline.Value.ToString("dd/MM/yyyy");
        if (task.IsOverdue)
            return $"{date} (trễ {(DateTime.Today - task.Deadline.Value.Date).Days} ngày)";
        if (task.Deadline.Value.Date == DateTime.Today && task.Status != AssignmentStatus.Completed)
            return $"{date} (hôm nay)";
        return date;
    }
}