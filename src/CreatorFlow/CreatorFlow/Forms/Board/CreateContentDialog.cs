using CreatorFlow.Models;
using CreatorFlow.Repositories.InMemory;

namespace CreatorFlow.Forms.Board;

/// <summary>
/// Dialog tạo nội dung mới. Giao diện nằm trong CreateContentDialog.Designer.cs,
/// file này chỉ chứa dữ liệu nạp cho ComboBox và logic kiểm tra/đóng dialog.
/// </summary>
public partial class CreateContentDialog : Form
{
    public string CreatedTitle => _txtTitle.Text.Trim();
    public Priority CreatedPriority => (Priority)_cboPriority.SelectedItem!;
    public long? CreatedAssigneeId => _cboAssignee.SelectedValue as long?;
    public DateTime CreatedDeadline => _dtpDeadline.Value.Date;
    public List<string> CreatedPlatforms
    {
        get
        {
            var list = new List<string>();
            if (_chkYouTube.Checked) list.Add("YouTube");
            if (_chkTikTok.Checked) list.Add("TikTok");
            if (_chkInstagram.Checked) list.Add("Instagram");
            if (_chkFacebook.Checked) list.Add("Facebook");
            return list;
        }
    }

    public CreateContentDialog()
    {
        InitializeComponent();

        // Dữ liệu động (không đặt trong Designer để Designer không serialize dữ liệu/ngày cố định)
        _cboPriority.Items.AddRange(new object[] { Priority.High, Priority.Medium, Priority.Low });
        _cboPriority.SelectedIndex = 1; // Medium

        var assignees = InMemoryDataStore.UserNames.Select(kv => new { Id = (long?)kv.Key, Name = kv.Value }).ToList();
        _cboAssignee.DataSource = assignees;
        if (assignees.Count > 1) _cboAssignee.SelectedIndex = 1; // Creator A

        _dtpDeadline.Value = DateTime.Today.AddDays(7);
    }

    private void btnSubmit_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtTitle.Text))
        {
            _lblError.Text = "Vui lòng nhập tiêu đề!";
            _txtTitle.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
