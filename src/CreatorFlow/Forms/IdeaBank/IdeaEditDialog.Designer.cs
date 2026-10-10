using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.IdeaBank;

partial class IdeaEditDialog
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label lblHeader;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.TextBox txtTitle;
    private System.Windows.Forms.Label lblDescription;
    private System.Windows.Forms.TextBox txtDescription;
    private System.Windows.Forms.Label lblTags;
    private System.Windows.Forms.TextBox txtTags;
    private System.Windows.Forms.Label lblTagHint;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.ComboBox cboStatus;
    private System.Windows.Forms.Label lblNote;
    private System.Windows.Forms.TextBox txtNote;
    private System.Windows.Forms.Label lblError;
    private CreatorFlow.Controls.RoundedButton btnSave;
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
        this.lblHeader = new System.Windows.Forms.Label();
        this.lblTitle = new System.Windows.Forms.Label();
        this.txtTitle = new System.Windows.Forms.TextBox();
        this.lblDescription = new System.Windows.Forms.Label();
        this.txtDescription = new System.Windows.Forms.TextBox();
        this.lblTags = new System.Windows.Forms.Label();
        this.txtTags = new System.Windows.Forms.TextBox();
        this.lblTagHint = new System.Windows.Forms.Label();
        this.lblStatus = new System.Windows.Forms.Label();
        this.cboStatus = new System.Windows.Forms.ComboBox();
        this.lblNote = new System.Windows.Forms.Label();
        this.txtNote = new System.Windows.Forms.TextBox();
        this.lblError = new System.Windows.Forms.Label();
        this.btnSave = new CreatorFlow.Controls.RoundedButton();
        this.btnCancel = new CreatorFlow.Controls.RoundedButton();
        this.SuspendLayout();
        //
        // lblHeader
        //
        this.lblHeader.Font = UITheme.FontH2;
        this.lblHeader.ForeColor = UITheme.Ink;
        this.lblHeader.Location = new System.Drawing.Point(24, 18);
        this.lblHeader.Name = "lblHeader";
        this.lblHeader.Size = new System.Drawing.Size(512, 30);
        this.lblHeader.TabIndex = 0;
        this.lblHeader.Text = "Thêm ý tưởng";
        //
        // lblTitle
        //
        this.lblTitle.AutoSize = true;
        this.lblTitle.Font = UITheme.FontLabelBold;
        this.lblTitle.ForeColor = UITheme.Neutral700;
        this.lblTitle.Location = new System.Drawing.Point(24, 60);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Text = "Tiêu đề / chủ đề *";
        //
        // txtTitle
        //
        this.txtTitle.Font = UITheme.FontBody;
        this.txtTitle.Location = new System.Drawing.Point(24, 80);
        this.txtTitle.MaxLength = 250;
        this.txtTitle.Name = "txtTitle";
        this.txtTitle.Size = new System.Drawing.Size(512, 29);
        this.txtTitle.TabIndex = 1;
        //
        // lblDescription
        //
        this.lblDescription.AutoSize = true;
        this.lblDescription.Font = UITheme.FontLabelBold;
        this.lblDescription.ForeColor = UITheme.Neutral700;
        this.lblDescription.Location = new System.Drawing.Point(24, 120);
        this.lblDescription.Name = "lblDescription";
        this.lblDescription.Text = "Mô tả";
        //
        // txtDescription
        //
        this.txtDescription.Font = UITheme.FontBody;
        this.txtDescription.Location = new System.Drawing.Point(24, 140);
        this.txtDescription.Multiline = true;
        this.txtDescription.Name = "txtDescription";
        this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtDescription.Size = new System.Drawing.Size(512, 90);
        this.txtDescription.TabIndex = 2;
        //
        // lblTags
        //
        this.lblTags.AutoSize = true;
        this.lblTags.Font = UITheme.FontLabelBold;
        this.lblTags.ForeColor = UITheme.Neutral700;
        this.lblTags.Location = new System.Drawing.Point(24, 242);
        this.lblTags.Name = "lblTags";
        this.lblTags.Text = "Tag (ngăn cách bằng dấu phẩy)";
        //
        // txtTags
        //
        this.txtTags.Font = UITheme.FontBody;
        this.txtTags.Location = new System.Drawing.Point(24, 262);
        this.txtTags.Name = "txtTags";
        this.txtTags.PlaceholderText = "VD: TikTok, Marketing";
        this.txtTags.Size = new System.Drawing.Size(512, 29);
        this.txtTags.TabIndex = 3;
        //
        // lblTagHint
        //
        this.lblTagHint.Font = UITheme.FontLabel;
        this.lblTagHint.ForeColor = UITheme.Neutral600;
        this.lblTagHint.Location = new System.Drawing.Point(24, 294);
        this.lblTagHint.Name = "lblTagHint";
        this.lblTagHint.Size = new System.Drawing.Size(512, 32);
        this.lblTagHint.Text = "";
        //
        // lblStatus
        //
        this.lblStatus.AutoSize = true;
        this.lblStatus.Font = UITheme.FontLabelBold;
        this.lblStatus.ForeColor = UITheme.Neutral700;
        this.lblStatus.Location = new System.Drawing.Point(24, 336);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Text = "Trạng thái";
        //
        // cboStatus
        //
        this.cboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboStatus.Font = UITheme.FontBody;
        this.cboStatus.Location = new System.Drawing.Point(24, 356);
        this.cboStatus.Name = "cboStatus";
        this.cboStatus.Size = new System.Drawing.Size(240, 29);
        this.cboStatus.TabIndex = 4;
        //
        // lblNote
        //
        this.lblNote.AutoSize = true;
        this.lblNote.Font = UITheme.FontLabelBold;
        this.lblNote.ForeColor = UITheme.Neutral700;
        this.lblNote.Location = new System.Drawing.Point(24, 398);
        this.lblNote.Name = "lblNote";
        this.lblNote.Text = "Ghi chú";
        //
        // txtNote
        //
        this.txtNote.Font = UITheme.FontBody;
        this.txtNote.Location = new System.Drawing.Point(24, 418);
        this.txtNote.Multiline = true;
        this.txtNote.Name = "txtNote";
        this.txtNote.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtNote.Size = new System.Drawing.Size(512, 80);
        this.txtNote.TabIndex = 5;
        //
        // lblError
        //
        this.lblError.Font = UITheme.FontLabel;
        this.lblError.ForeColor = UITheme.Danger;
        this.lblError.Location = new System.Drawing.Point(24, 506);
        this.lblError.Name = "lblError";
        this.lblError.Size = new System.Drawing.Size(512, 22);
        //
        // btnSave
        //
        this.btnSave.Location = new System.Drawing.Point(318, 534);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(120, 38);
        this.btnSave.Style = RoundButtonStyle.Primary;
        this.btnSave.TabIndex = 6;
        this.btnSave.Text = "Lưu";
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
        //
        // btnCancel
        //
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(446, 534);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(90, 38);
        this.btnCancel.Style = RoundButtonStyle.Secondary;
        this.btnCancel.TabIndex = 7;
        this.btnCancel.Text = "Hủy";
        //
        // IdeaEditDialog
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
        this.BackColor = UITheme.White;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(560, 592);
        this.Controls.Add(this.lblHeader);
        this.Controls.Add(this.lblTitle);
        this.Controls.Add(this.txtTitle);
        this.Controls.Add(this.lblDescription);
        this.Controls.Add(this.txtDescription);
        this.Controls.Add(this.lblTags);
        this.Controls.Add(this.txtTags);
        this.Controls.Add(this.lblTagHint);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.cboStatus);
        this.Controls.Add(this.lblNote);
        this.Controls.Add(this.txtNote);
        this.Controls.Add(this.lblError);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.btnCancel);
        this.Font = UITheme.FontBody;
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "IdeaEditDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Idea";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
