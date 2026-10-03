using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

partial class ReviewDecisionDialog
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label lblContentTitle;
    private System.Windows.Forms.Label lblReviewInfo;
    private System.Windows.Forms.Label lblFeedback;
    private System.Windows.Forms.TextBox txtFeedback;
    private System.Windows.Forms.Label lblError;
    private CreatorFlow.Controls.RoundedButton btnApprove;
    private CreatorFlow.Controls.RoundedButton btnReject;
    private CreatorFlow.Controls.RoundedButton btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.lblContentTitle = new System.Windows.Forms.Label();
        this.lblReviewInfo = new System.Windows.Forms.Label();
        this.lblFeedback = new System.Windows.Forms.Label();
        this.txtFeedback = new System.Windows.Forms.TextBox();
        this.lblError = new System.Windows.Forms.Label();
        this.btnApprove = new CreatorFlow.Controls.RoundedButton();
        this.btnReject = new CreatorFlow.Controls.RoundedButton();
        this.btnCancel = new CreatorFlow.Controls.RoundedButton();
        this.SuspendLayout();
        // 
        // lblContentTitle
        // 
        this.lblContentTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblContentTitle.ForeColor = UITheme.Ink;
        this.lblContentTitle.Location = new System.Drawing.Point(20, 18);
        this.lblContentTitle.Name = "lblContentTitle";
        this.lblContentTitle.Size = new System.Drawing.Size(440, 26);
        this.lblContentTitle.TabIndex = 0;
        this.lblContentTitle.Text = "CNT-000 — Tiêu đề Content";
        // 
        // lblReviewInfo
        // 
        this.lblReviewInfo.Font = UITheme.FontBody;
        this.lblReviewInfo.ForeColor = UITheme.Neutral600;
        this.lblReviewInfo.Location = new System.Drawing.Point(20, 48);
        this.lblReviewInfo.Name = "lblReviewInfo";
        this.lblReviewInfo.Size = new System.Drawing.Size(440, 22);
        this.lblReviewInfo.TabIndex = 1;
        this.lblReviewInfo.Text = "Review #1 — Submitted by ... at ...";
        // 
        // lblFeedback
        // 
        this.lblFeedback.Font = UITheme.FontLabelBold;
        this.lblFeedback.ForeColor = UITheme.Neutral700;
        this.lblFeedback.Location = new System.Drawing.Point(20, 80);
        this.lblFeedback.Name = "lblFeedback";
        this.lblFeedback.Size = new System.Drawing.Size(440, 20);
        this.lblFeedback.TabIndex = 2;
        this.lblFeedback.Text = "Feedback phản hồi (Bắt buộc nếu Từ chối / Reject):";
        // 
        // txtFeedback
        // 
        this.txtFeedback.Font = UITheme.FontBody;
        this.txtFeedback.Location = new System.Drawing.Point(20, 104);
        this.txtFeedback.Multiline = true;
        this.txtFeedback.Name = "txtFeedback";
        this.txtFeedback.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtFeedback.Size = new System.Drawing.Size(440, 100);
        this.txtFeedback.TabIndex = 3;
        // 
        // lblError
        // 
        this.lblError.Font = UITheme.FontLabelBold;
        this.lblError.ForeColor = UITheme.Danger;
        this.lblError.Location = new System.Drawing.Point(20, 210);
        this.lblError.Name = "lblError";
        this.lblError.Size = new System.Drawing.Size(440, 20);
        this.lblError.TabIndex = 4;
        this.lblError.Text = "";
        // 
        // btnApprove
        // 
        this.btnApprove.Text = "✓ Approve (Phê duyệt)";
        this.btnApprove.Style = RoundButtonStyle.Primary;
        this.btnApprove.Location = new System.Drawing.Point(20, 236);
        this.btnApprove.Name = "btnApprove";
        this.btnApprove.Size = new System.Drawing.Size(160, 36);
        this.btnApprove.TabIndex = 5;
        this.btnApprove.Click += new System.EventHandler(this.btnApprove_Click);
        // 
        // btnReject
        // 
        this.btnReject.Text = "✕ Reject (Từ chối)";
        this.btnReject.Style = RoundButtonStyle.Danger;
        this.btnReject.Location = new System.Drawing.Point(190, 236);
        this.btnReject.Name = "btnReject";
        this.btnReject.Size = new System.Drawing.Size(140, 36);
        this.btnReject.TabIndex = 6;
        this.btnReject.Click += new System.EventHandler(this.btnReject_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Text = "Đóng";
        this.btnCancel.Style = RoundButtonStyle.Secondary;
        this.btnCancel.Location = new System.Drawing.Point(340, 236);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(120, 36);
        this.btnCancel.TabIndex = 7;
        // 
        // ReviewDecisionDialog
        // 
        this.AcceptButton = this.btnApprove;
        this.CancelButton = this.btnCancel;
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = UITheme.White;
        this.ClientSize = new System.Drawing.Size(480, 290);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnReject);
        this.Controls.Add(this.btnApprove);
        this.Controls.Add(this.lblError);
        this.Controls.Add(this.txtFeedback);
        this.Controls.Add(this.lblFeedback);
        this.Controls.Add(this.lblReviewInfo);
        this.Controls.Add(this.lblContentTitle);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ReviewDecisionDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Kiểm duyệt nội dung — CreatorFlow";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}