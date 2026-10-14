using System.ComponentModel;
using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

/// <summary>
/// Dialog Creator xác nhận gửi Content sang REVIEW (mục 7 UX spec).
/// Layout nằm trong SubmitReviewDialog.Designer.cs (kéo thả được); file này chỉ có dữ liệu và logic.
/// Dùng <see cref="TrySubmit"/> để mở dialog + gọi WorkflowService.SubmitForReview.
/// </summary>
public partial class SubmitReviewDialog : Form
{
    /// <summary>Ghi chú tùy chọn gửi kèm (lưu vào lịch sử trạng thái), null nếu để trống.</summary>
    public string? Note => string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim();

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được form.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public SubmitReviewDialog()
    {
        InitializeComponent();

        // Các thuộc tính này không được Designer serialize (DesignerSerializationVisibility.Hidden) → đặt ở đây.
        pnlInfo.FillColor = UITheme.Neutral50;
        pnlInfo.Radius = 12; // Card/Dialog: bo góc xl = 12px (UI Style Guide mục 4)
        btnSubmit.Style = RoundButtonStyle.Primary;
        btnCancel.Style = RoundButtonStyle.Secondary;
    }

    public SubmitReviewDialog(string contentCode, string contentTitle, string submitterName) : this()
    {
        lblContentTitle.UseMnemonic = false; // tránh "&" trong tiêu đề bị nuốt
        lblContentTitle.Text = $"{contentCode} — {contentTitle}";
        lblSubmitterValue.Text = submitterName;
        lblTimeValue.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// Mở dialog xác nhận rồi gửi duyệt. Trả về true nếu đã gửi thành công
    /// (caller tải lại Board/Panel). Lỗi nghiệp vụ được hiện bằng MessageBox.
    /// Quyền và trạng thái hợp lệ do Service/Backend kiểm tra, dialog không tự quyết.
    /// </summary>
    public static bool TrySubmit(IWin32Window owner, WorkflowService workflowService, ContentBoardCard card)
    {
        try
        {
            using var dialog = new SubmitReviewDialog(card.Code, card.Title, CurrentSession.CurrentUserName);
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return false;

            workflowService.SubmitForReview(card.ContentId, CurrentSession.CurrentUserId, dialog.Note);
            return true;
        }
        catch (InvalidWorkflowTransitionException ex)
        {
            MessageBox.Show(owner, ex.Message, "Không thể gửi kiểm duyệt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(owner, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (ContentValidationException ex)
        {
            MessageBox.Show(owner, ex.Message, "Không thể gửi kiểm duyệt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(owner, ex.Message, "Không thể gửi kiểm duyệt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return false;
    }
}
