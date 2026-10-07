using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

/// <summary>
/// Dialog "Giao việc cho Creator": Owner/Manager tick MỘT HOẶC NHIỀU Creator của Project và đặt deadline riêng cho từng người.
/// Giao diện tĩnh nằm trong AssignContentDialog.Designer.cs; các dòng Creator được dựng trong code vì số lượng thay đổi theo Project.
/// Việc kiểm tra quyền / đúng Project / deadline hợp lệ do ContentService.AssignContent đảm nhiệm
/// (dialog chỉ kiểm tra nhanh để báo lỗi ngay dưới danh sách).
/// </summary>
public partial class AssignContentDialog : Form
{
    private const int RowHeight = 44;

    private sealed class CreatorRow
    {
        public required ProjectMemberInfo Member { get; init; }
        public required CheckBox Check { get; init; }
        public required DateTimePicker Deadline { get; init; }
        public MyTaskItem? Existing { get; init; }
    }

    private readonly List<CreatorRow> _rows = new();

    /// <summary>Danh sách người nhận sau khi giao (đọc sau khi ShowDialog() trả về DialogResult.OK).</summary>
    public List<AssignmentRequest> Assignments { get; private set; } = new();

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được form.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public AssignContentDialog()
    {
        InitializeComponent();
    }

    /// <param name="creators">Creator có thể giao (thuộc đúng Project).</param>
    /// <param name="currentAssignments">Những người đang được giao Content này (để tick sẵn, giữ tiến độ/deadline).</param>
    /// <param name="contentDeadline">Deadline chung của Content, dùng làm gợi ý khi người mới chưa có deadline riêng.</param>
    public AssignContentDialog(
        string contentCode,
        string contentTitle,
        List<ProjectMemberInfo> creators,
        List<MyTaskItem> currentAssignments,
        DateTime? contentDeadline)
    {
        InitializeComponent();

        lblSub.Text = $"{contentCode} — {contentTitle}";

        if (creators.Count == 0)
        {
            btnSubmit.Enabled = false;
            ShowError("Project chưa có Creator nào để giao việc.");
        }

        int y = 0;
        foreach (var member in creators)
        {
            var existing = currentAssignments.FirstOrDefault(a => a.AssigneeUserId == member.UserId);
            var row = BuildRow(member, existing, contentDeadline, y);
            _rows.Add(row);
            y += RowHeight;
        }

        AcceptButton = btnSubmit;
        CancelButton = btnCancel;
    }

    private CreatorRow BuildRow(ProjectMemberInfo member, MyTaskItem? existing, DateTime? contentDeadline, int y)
    {
        bool isDone = existing?.Status == AssignmentStatus.Completed;

        string label = member.Name;
        if (existing != null)
            label += isDone ? "  (đã hoàn thành)" : $"  ({existing.ProgressPercent}%)";

        var check = new CheckBox
        {
            Text = label,
            AutoSize = false,
            Size = new Size(240, 28),
            Location = new Point(12, y + 8),
            Font = UITheme.FontBody,
            ForeColor = UITheme.Ink,
            Checked = existing != null,
            Enabled = !isDone, // người đã hoàn thành luôn nằm trong danh sách, không bỏ giao được
        };

        var deadline = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Font = UITheme.FontBody,
            Size = new Size(150, 28),
            Location = new Point(262, y + 8),
            Value = (existing?.Deadline ?? contentDeadline ?? DateTime.Today.AddDays(7)).Date,
            // Tick = đặt deadline riêng (hiện giá trị hiện tại nếu đã có); bỏ tick = giữ nguyên / chưa đặt.
            Checked = existing?.Deadline.HasValue == true,
            Enabled = !isDone && existing != null,
        };

        // Chỉ bật ô ngày khi Creator đó được tick.
        check.CheckedChanged += (_, _) =>
        {
            deadline.Enabled = check.Checked;
            ShowError(null);
        };

        // Đường kẻ mảnh giữa các dòng.
        var line = new Panel
        {
            BackColor = UITheme.Neutral100,
            Size = new Size(440, 1),
            Location = new Point(0, y + RowHeight - 1),
        };

        pnlCreators.Controls.Add(check);
        pnlCreators.Controls.Add(deadline);
        pnlCreators.Controls.Add(line);

        return new CreatorRow { Member = member, Check = check, Deadline = deadline, Existing = existing };
    }

    private void btnSubmit_Click(object? sender, EventArgs e)
    {
        ShowError(null);

        var chosen = _rows.Where(r => r.Check.Checked).ToList();
        if (chosen.Count == 0)
        {
            ShowError("Hãy chọn ít nhất một Creator.");
            return;
        }

        // Validation hiện ngay dưới danh sách (style guide mục 13); service vẫn kiểm tra lại khi lưu.
        var result = new List<AssignmentRequest>();
        foreach (var row in chosen)
        {
            DateTime? deadline = row.Deadline.Enabled && row.Deadline.Checked ? row.Deadline.Value.Date : null;
            try
            {
                TaskRules.ValidateNewDeadline(deadline, row.Existing?.Deadline, DateTime.Today);
            }
            catch (ContentValidationException ex)
            {
                ShowError($"{row.Member.Name}: {ex.Message}");
                row.Deadline.Focus();
                return;
            }
            result.Add(new AssignmentRequest(row.Member.UserId, deadline));
        }

        Assignments = result;
        DialogResult = DialogResult.OK;
    }

    private void ShowError(string? message) => _lblError.Text = message ?? string.Empty;
}