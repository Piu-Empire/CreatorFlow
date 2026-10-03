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

    public CreateContentDialog(List<ProjectMemberInfo> members, ContentStatus initialStatus = ContentStatus.Idea)
    {
        InitializeComponent();
        _members = members;

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
            EstimatedDuration = _txtDuration.Text,
            AssigneeUserId = (_cboAssignee.SelectedItem as ProjectMemberInfo)?.UserId,
            Platforms = CollectPlatforms(),
        };

        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            _lblError.Text = "Vui lòng nhập tiêu đề!";
            _txtTitle.Focus();
            return;
        }

        Draft = draft;
        DialogResult = DialogResult.OK;
        Close();
    }

    private List<string> CollectPlatforms()
    {
        var list = new List<string>();
        if (_chkYouTube.Checked) list.Add("YouTube");
        if (_chkTikTok.Checked) list.Add("TikTok");
        if (_chkInstagram.Checked) list.Add("Instagram");
        if (_chkFacebook.Checked) list.Add("Facebook");
        return list;
    }
}
