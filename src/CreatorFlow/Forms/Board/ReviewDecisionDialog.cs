namespace CreatorFlow.Forms.Board;

/// <summary>
/// Dialog dùng chung cho Approve/Reject 1 Review (mục 7 UX spec).
/// Reject bắt buộc phải nhập Feedback trước khi cho phép đóng dialog với kết quả Reject.
/// Caller đọc <see cref="Approved"/> và <see cref="Feedback"/> sau khi ShowDialog() trả về DialogResult.OK.
/// </summary>
public partial class ReviewDecisionDialog : Form
{
    public bool Approved { get; private set; }
    public string Feedback => txtFeedback.Text.Trim();

    public ReviewDecisionDialog(string contentCodeAndTitle, string reviewInfo)
    {
        InitializeComponent();
        lblContentTitle.Text = contentCodeAndTitle;
        lblReviewInfo.Text = reviewInfo;
    }

    private void btnApprove_Click(object? sender, EventArgs e)
    {
        Approved = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnReject_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtFeedback.Text))
        {
            lblError.Text = "Reject bắt buộc phải nhập Feedback.";
            txtFeedback.Focus();
            return;
        }

        Approved = false;
        DialogResult = DialogResult.OK;
        Close();
    }
}