using CreatorFlow.Models.Enums;
using CreatorFlow.Models;
using CreatorFlow.Services;

namespace CreatorFlow.Forms.Board;

/// <summary>
/// Dialog tạo nội dung mới. Giao diện nằm trong CreateContentDialog.Designer.cs,
/// file này chỉ chứa dữ liệu nạp cho ComboBox và đóng gói kết quả thành ContentDraft.
/// </summary>
public partial class CreateContentDialog : Form
{
    private readonly List<ProjectMemberInfo> _members;

    /// <summary>Kết quả nhập, đọc sau khi ShowDialog() trả về DialogResult.OK.</summary>
    public ContentDraft Draft { get; private set; } = new();

    /// <param name="platforms">Tên các nền tảng đang bật (lấy từ ContentService.GetAvailablePlatforms) — mỗi tên là 1 checkbox.</param>
    public CreateContentDialog(List<ProjectMemberInfo> members, List<string> platforms, ContentStatus initialStatus = ContentStatus.Idea)
    {
        InitializeComponent();
        _members = members;

        foreach (string name in platforms)
            _pnlPlatforms.Controls.Add(new CheckBox { Text = name, AutoSize = true, Margin = new Padding(0, 0, 24, 0) });

        // Label coi "&" là ký tự phím tắt và ẩn nó đi (VD "Ideas & Discovery" → "Ideas  Discovery") — tắt để hiện đúng.
        lblHeader.UseMnemonic = false;
        lblHeader.Text = $"Tạo nội dung mới — {CreatorFlow.Theme.UITheme.GetStageDisplayName(initialStatus)}";

        _cboPriority.Items.AddRange(new object[] { Priority.High, Priority.Medium, Priority.Low });
        _cboPriority.SelectedIndex = 1; // Medium

        _cboSprint.Items.AddRange(ContentService.AvailableSprints);
        _cboSprint.SelectedIndex = Array.IndexOf(ContentService.AvailableSprints, "Sprint 25");
        if (_cboSprint.SelectedIndex < 0) _cboSprint.SelectedIndex = 0;

        _cboAssignee.DataSource = _members;
        if (_members.Count > 0) _cboAssignee.SelectedIndex = 0;

        _dtpDeadline.Value = DateTime.Today.AddDays(7);

        // Ngày dự kiến đăng là tùy chọn: để bỏ tick = chưa lên lịch đăng. Gán Value trước rồi mới bỏ tick.
        _dtpPlannedPublish.Value = DateTime.Today.AddDays(10);
        _dtpPlannedPublish.Checked = false;

        _cboContentType.Items.AddRange(ContentService.AvailableContentTypes);
        _cboContentType.SelectedIndex = Math.Max(0, Array.IndexOf(ContentService.AvailableContentTypes, ContentService.DefaultContentType));

        AcceptButton = btnSubmit;
    }

    private void btnSubmit_Click(object? sender, EventArgs e)
    {
        var draft = new ContentDraft
        {
            Title = _txtTitle.Text,
            Description = _txtHook.Text,
            Priority = (Priority)_cboPriority.SelectedItem!,
            Sprint = _cboSprint.SelectedItem as string ?? "",
            Deadline = _dtpDeadline.Value.Date,
            PlannedPublishAt = _dtpPlannedPublish.Checked ? _dtpPlannedPublish.Value.Date : null,
            ContentType = _cboContentType.SelectedItem as string ?? string.Empty,
            EstimatedDuration = _txtDuration.Text,
            AssigneeUserId = (_cboAssignee.SelectedItem as ProjectMemberInfo)?.UserId,
            Platforms = CollectPlatforms(),
        };

        // Dùng cùng bộ quy tắc với ContentService để báo lỗi ngay trong dialog (không đóng dialog, không mất dữ liệu đã nhập).
        ContentService.Normalize(draft);
        string? error = ContentService.GetValidationError(draft);
        if (error != null)
        {
            _lblError.Text = error;
            if (draft.Title.Length == 0 || draft.Title.Length > ContentService.MaxTitleLength)
                _txtTitle.Focus();
            return;
        }

        _lblError.Text = string.Empty;

        Draft = draft;
        DialogResult = DialogResult.OK;
        Close();
    }

    private List<string> CollectPlatforms()
    {
        return _pnlPlatforms.Controls.OfType<CheckBox>()
            .Where(c => c.Checked)
            .Select(c => c.Text)
            .ToList();
    }
}