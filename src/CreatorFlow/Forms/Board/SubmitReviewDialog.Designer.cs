using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

partial class SubmitReviewDialog
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label lblHeader;
    private System.Windows.Forms.Label lblSub;
    private System.Windows.Forms.Panel pnlDivider;
    private System.Windows.Forms.Label lblContentTitle;
    private CreatorFlow.Controls.RoundedPanel pnlInfo;
    private System.Windows.Forms.Label lblStatusCaption;
    private System.Windows.Forms.Label lblStatusValue;
    private System.Windows.Forms.Label lblSubmitterCaption;
    private System.Windows.Forms.Label lblSubmitterValue;
    private System.Windows.Forms.Label lblTimeCaption;
    private System.Windows.Forms.Label lblTimeValue;
    private System.Windows.Forms.Label lblNote;
    private System.Windows.Forms.TextBox txtNote;
    private System.Windows.Forms.Panel pnlDivider2;
    private CreatorFlow.Controls.RoundedButton btnCancel;
    private CreatorFlow.Controls.RoundedButton btnSubmit;

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
        this.lblHeader = new System.Windows.Forms.Label();
        this.lblSub = new System.Windows.Forms.Label();
        this.pnlDivider = new System.Windows.Forms.Panel();
        this.lblContentTitle = new System.Windows.Forms.Label();
        this.pnlInfo = new CreatorFlow.Controls.RoundedPanel();
        this.lblStatusCaption = new System.Windows.Forms.Label();
        this.lblStatusValue = new System.Windows.Forms.Label();
        this.lblSubmitterCaption = new System.Windows.Forms.Label();
        this.lblSubmitterValue = new System.Windows.Forms.Label();
        this.lblTimeCaption = new System.Windows.Forms.Label();
        this.lblTimeValue = new System.Windows.Forms.Label();
        this.lblNote = new System.Windows.Forms.Label();
        this.txtNote = new System.Windows.Forms.TextBox();
        this.pnlDivider2 = new System.Windows.Forms.Panel();
        this.btnCancel = new CreatorFlow.Controls.RoundedButton();
        this.btnSubmit = new CreatorFlow.Controls.RoundedButton();
        this.pnlInfo.SuspendLayout();
        this.SuspendLayout();
        // 
        // lblHeader
        // 
        this.lblHeader.AutoSize = true;
        this.lblHeader.Font = UITheme.FontH2;
        this.lblHeader.ForeColor = UITheme.Ink;
        this.lblHeader.Location = new System.Drawing.Point(28, 24);
        this.lblHeader.Name = "lblHeader";
        this.lblHeader.TabIndex = 0;
        this.lblHeader.Text = "Gửi kiểm duyệt";
        // 
        // lblSub
        // 
        this.lblSub.AutoSize = true;
        this.lblSub.Font = UITheme.FontBody;
        this.lblSub.ForeColor = UITheme.Neutral600;
        this.lblSub.Location = new System.Drawing.Point(28, 52);
        this.lblSub.Name = "lblSub";
        this.lblSub.TabIndex = 1;
        this.lblSub.Text = "Content sẽ chuyển sang REVIEW để Owner/Manager duyệt.";
        // 
        // pnlDivider
        // 
        this.pnlDivider.BackColor = UITheme.Neutral200;
        this.pnlDivider.Location = new System.Drawing.Point(28, 86);
        this.pnlDivider.Name = "pnlDivider";
        this.pnlDivider.Size = new System.Drawing.Size(424, 1);
        this.pnlDivider.TabIndex = 2;
        // 
        // lblContentTitle
        // 
        this.lblContentTitle.AutoSize = true;
        this.lblContentTitle.Font = UITheme.FontBodyBold;
        this.lblContentTitle.ForeColor = UITheme.Ink;
        this.lblContentTitle.Location = new System.Drawing.Point(28, 104);
        this.lblContentTitle.Name = "lblContentTitle";
        this.lblContentTitle.TabIndex = 3;
        this.lblContentTitle.Text = "CNT-000 — Tiêu đề Content";
        // 
        // pnlInfo
        // 
        this.pnlInfo.Controls.Add(this.lblTimeValue);
        this.pnlInfo.Controls.Add(this.lblTimeCaption);
        this.pnlInfo.Controls.Add(this.lblSubmitterValue);
        this.pnlInfo.Controls.Add(this.lblSubmitterCaption);
        this.pnlInfo.Controls.Add(this.lblStatusValue);
        this.pnlInfo.Controls.Add(this.lblStatusCaption);
        this.pnlInfo.Location = new System.Drawing.Point(28, 132);
        this.pnlInfo.Name = "pnlInfo";
        this.pnlInfo.Size = new System.Drawing.Size(424, 88);
        this.pnlInfo.TabIndex = 4;
        // 
        // lblStatusCaption
        // 
        this.lblStatusCaption.AutoSize = true;
        this.lblStatusCaption.Font = UITheme.FontLabelBold;
        this.lblStatusCaption.ForeColor = UITheme.Neutral600;
        this.lblStatusCaption.Location = new System.Drawing.Point(16, 12);
        this.lblStatusCaption.Name = "lblStatusCaption";
        this.lblStatusCaption.TabIndex = 0;
        this.lblStatusCaption.Text = "Trạng thái";
        // 
        // lblStatusValue
        // 
        this.lblStatusValue.AutoSize = true;
        this.lblStatusValue.Font = UITheme.FontBody;
        this.lblStatusValue.ForeColor = UITheme.Ink;
        this.lblStatusValue.Location = new System.Drawing.Point(140, 12);
        this.lblStatusValue.Name = "lblStatusValue";
        this.lblStatusValue.TabIndex = 1;
        this.lblStatusValue.Text = "EDITING → REVIEW";
        // 
        // lblSubmitterCaption
        // 
        this.lblSubmitterCaption.AutoSize = true;
        this.lblSubmitterCaption.Font = UITheme.FontLabelBold;
        this.lblSubmitterCaption.ForeColor = UITheme.Neutral600;
        this.lblSubmitterCaption.Location = new System.Drawing.Point(16, 36);
        this.lblSubmitterCaption.Name = "lblSubmitterCaption";
        this.lblSubmitterCaption.TabIndex = 2;
        this.lblSubmitterCaption.Text = "Người gửi";
        // 
        // lblSubmitterValue
        // 
        this.lblSubmitterValue.AutoSize = true;
        this.lblSubmitterValue.Font = UITheme.FontBody;
        this.lblSubmitterValue.ForeColor = UITheme.Ink;
        this.lblSubmitterValue.Location = new System.Drawing.Point(140, 36);
        this.lblSubmitterValue.Name = "lblSubmitterValue";
        this.lblSubmitterValue.TabIndex = 3;
        this.lblSubmitterValue.Text = "—";
        // 
        // lblTimeCaption
        // 
        this.lblTimeCaption.AutoSize = true;
        this.lblTimeCaption.Font = UITheme.FontLabelBold;
        this.lblTimeCaption.ForeColor = UITheme.Neutral600;
        this.lblTimeCaption.Location = new System.Drawing.Point(16, 60);
        this.lblTimeCaption.Name = "lblTimeCaption";
        this.lblTimeCaption.TabIndex = 4;
        this.lblTimeCaption.Text = "Thời gian gửi";
        // 
        // lblTimeValue
        // 
        this.lblTimeValue.AutoSize = true;
        this.lblTimeValue.Font = UITheme.FontBody;
        this.lblTimeValue.ForeColor = UITheme.Ink;
        this.lblTimeValue.Location = new System.Drawing.Point(140, 60);
        this.lblTimeValue.Name = "lblTimeValue";
        this.lblTimeValue.TabIndex = 5;
        this.lblTimeValue.Text = "—";
        // 
        // lblNote
        // 
        this.lblNote.AutoSize = true;
        this.lblNote.Font = UITheme.FontLabelBold;
        this.lblNote.ForeColor = UITheme.Neutral700;
        this.lblNote.Location = new System.Drawing.Point(28, 236);
        this.lblNote.Name = "lblNote";
        this.lblNote.TabIndex = 5;
        this.lblNote.Text = "Ghi chú cho người duyệt (không bắt buộc)";
        // 
        // txtNote
        // 
        this.txtNote.AcceptsReturn = true;
        this.txtNote.Font = UITheme.FontBody;
        this.txtNote.Location = new System.Drawing.Point(28, 256);
        this.txtNote.MaxLength = 500;
        this.txtNote.Multiline = true;
        this.txtNote.Name = "txtNote";
        this.txtNote.PlaceholderText = "Ví dụ: đã cắt lại phần mở đầu theo góp ý lần trước...";
        this.txtNote.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtNote.Size = new System.Drawing.Size(424, 84);
        this.txtNote.TabIndex = 6;
        // 
        // pnlDivider2
        // 
        this.pnlDivider2.BackColor = UITheme.Neutral200;
        this.pnlDivider2.Location = new System.Drawing.Point(28, 356);
        this.pnlDivider2.Name = "pnlDivider2";
        this.pnlDivider2.Size = new System.Drawing.Size(424, 1);
        this.pnlDivider2.TabIndex = 7;
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(192, 372);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(90, 36);
        this.btnCancel.Style = RoundButtonStyle.Secondary;
        this.btnCancel.TabIndex = 8;
        this.btnCancel.Text = "Huỷ";
        // 
        // btnSubmit
        // 
        this.btnSubmit.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.btnSubmit.Location = new System.Drawing.Point(292, 372);
        this.btnSubmit.Name = "btnSubmit";
        this.btnSubmit.Size = new System.Drawing.Size(160, 36);
        this.btnSubmit.Style = RoundButtonStyle.Primary;
        this.btnSubmit.TabIndex = 9;
        this.btnSubmit.Text = "Gửi kiểm duyệt";
        // 
        // SubmitReviewDialog
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.BackColor = UITheme.White;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(480, 424);
        this.Controls.Add(this.lblHeader);
        this.Controls.Add(this.lblSub);
        this.Controls.Add(this.pnlDivider);
        this.Controls.Add(this.lblContentTitle);
        this.Controls.Add(this.pnlInfo);
        this.Controls.Add(this.lblNote);
        this.Controls.Add(this.txtNote);
        this.Controls.Add(this.pnlDivider2);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSubmit);
        this.Font = UITheme.FontBody;
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "SubmitReviewDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Gửi kiểm duyệt — CreatorFlow";
        this.pnlInfo.ResumeLayout(false);
        this.pnlInfo.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
