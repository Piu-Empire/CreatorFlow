using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Dialog admin xử lý báo cáo: chọn trạng thái + ghi chú.</summary>
public sealed class HandleReportDialog : Form
{
    private readonly ComboBox _cboStatus = new();
    private readonly TextBox _txtNote = new();

    public string SelectedStatus => _cboStatus.SelectedItem?.ToString() ?? "IN_REVIEW";

    public string? Note => string.IsNullOrWhiteSpace(_txtNote.Text) ? null : _txtNote.Text.Trim();

    public HandleReportDialog(string reportedEmail)
    {
        Text = "Xử lý báo cáo";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 250);
        BackColor = UITheme.White;

        var info = new Label
        {
            Text = $"Báo cáo về: {reportedEmail}",
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(20, 16),
        };
        var statusLabel = new Label
        {
            Text = "Trạng thái xử lý",
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(20, 48),
        };
        _cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboStatus.Font = UITheme.FontBody;
        _cboStatus.Items.AddRange(["IN_REVIEW", "RESOLVED", "REJECTED"]);
        _cboStatus.SelectedIndex = 0;
        _cboStatus.Location = new Point(20, 68);
        _cboStatus.Width = 380;
        var noteLabel = new Label
        {
            Text = "Ghi chú xử lý (tối đa 1000 ký tự)",
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(20, 100),
        };
        _txtNote.Font = UITheme.FontBody;
        _txtNote.Multiline = true;
        _txtNote.ScrollBars = ScrollBars.Vertical;
        _txtNote.MaxLength = 1000;
        _txtNote.Location = new Point(20, 120);
        _txtNote.Size = new Size(380, 64);
        var btnOk = new RoundedButton
        {
            Text = "Xác nhận",
            Style = RoundButtonStyle.Primary,
            Size = new Size(110, 34),
            Location = new Point(180, 198),
            DialogResult = DialogResult.OK,
        };
        var btnCancel = new RoundedButton
        {
            Text = "Hủy",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(100, 34),
            Location = new Point(300, 198),
            DialogResult = DialogResult.Cancel,
        };
        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Controls.Add(info);
        Controls.Add(statusLabel);
        Controls.Add(_cboStatus);
        Controls.Add(noteLabel);
        Controls.Add(_txtNote);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
    }
}
