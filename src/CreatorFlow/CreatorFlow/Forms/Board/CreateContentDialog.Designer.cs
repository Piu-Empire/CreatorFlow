using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

partial class CreateContentDialog
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label lblHeader;
    private System.Windows.Forms.Label lblSub;
    private System.Windows.Forms.Panel pnlDivider;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.TextBox _txtTitle;
    private System.Windows.Forms.Label lblPlatforms;
    private System.Windows.Forms.CheckBox _chkYouTube;
    private System.Windows.Forms.CheckBox _chkTikTok;
    private System.Windows.Forms.CheckBox _chkInstagram;
    private System.Windows.Forms.CheckBox _chkFacebook;
    private System.Windows.Forms.Label lblPriority;
    private System.Windows.Forms.Label lblAssignee;
    private System.Windows.Forms.ComboBox _cboPriority;
    private System.Windows.Forms.ComboBox _cboAssignee;
    private System.Windows.Forms.Label lblDeadline;
    private System.Windows.Forms.DateTimePicker _dtpDeadline;
    private System.Windows.Forms.Label _lblError;
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
        this.lblTitle = new System.Windows.Forms.Label();
        this._txtTitle = new System.Windows.Forms.TextBox();
        this.lblPlatforms = new System.Windows.Forms.Label();
        this._chkYouTube = new System.Windows.Forms.CheckBox();
        this._chkTikTok = new System.Windows.Forms.CheckBox();
        this._chkInstagram = new System.Windows.Forms.CheckBox();
        this._chkFacebook = new System.Windows.Forms.CheckBox();
        this.lblPriority = new System.Windows.Forms.Label();
        this.lblAssignee = new System.Windows.Forms.Label();
        this._cboPriority = new System.Windows.Forms.ComboBox();
        this._cboAssignee = new System.Windows.Forms.ComboBox();
        this.lblDeadline = new System.Windows.Forms.Label();
        this._dtpDeadline = new System.Windows.Forms.DateTimePicker();
        this._lblError = new System.Windows.Forms.Label();
        this.pnlDivider2 = new System.Windows.Forms.Panel();
        this.btnCancel = new CreatorFlow.Controls.RoundedButton();
        this.btnSubmit = new CreatorFlow.Controls.RoundedButton();
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
        this.lblHeader.Text = "Tạo nội dung mới";
        // 
        // lblSub
        // 
        this.lblSub.AutoSize = true;
        this.lblSub.Font = UITheme.FontBody;
        this.lblSub.ForeColor = UITheme.Neutral600;
        this.lblSub.Location = new System.Drawing.Point(28, 52);
        this.lblSub.Name = "lblSub";
        this.lblSub.TabIndex = 1;
        this.lblSub.Text = "Điền thông tin ý tưởng để đưa vào pipeline sản xuất.";
        // 
        // pnlDivider
        // 
        this.pnlDivider.BackColor = UITheme.Neutral200;
        this.pnlDivider.Location = new System.Drawing.Point(28, 86);
        this.pnlDivider.Name = "pnlDivider";
        this.pnlDivider.Size = new System.Drawing.Size(424, 1);
        this.pnlDivider.TabIndex = 2;
        // 
        // lblTitle
        // 
        this.lblTitle.AutoSize = true;
        this.lblTitle.Font = UITheme.FontLabelBold;
        this.lblTitle.ForeColor = UITheme.Neutral700;
        this.lblTitle.Location = new System.Drawing.Point(28, 102);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.TabIndex = 3;
        this.lblTitle.Text = "Tiêu đề nội dung *";
        // 
        // _txtTitle
        // 
        this._txtTitle.Font = UITheme.FontBody;
        this._txtTitle.Location = new System.Drawing.Point(28, 122);
        this._txtTitle.Name = "_txtTitle";
        this._txtTitle.PlaceholderText = "Nhập tiêu đề ý tưởng hoặc video...";
        this._txtTitle.Size = new System.Drawing.Size(424, 29);
        this._txtTitle.TabIndex = 0;
        // 
        // lblPlatforms
        // 
        this.lblPlatforms.AutoSize = true;
        this.lblPlatforms.Font = UITheme.FontLabelBold;
        this.lblPlatforms.ForeColor = UITheme.Neutral700;
        this.lblPlatforms.Location = new System.Drawing.Point(28, 162);
        this.lblPlatforms.Name = "lblPlatforms";
        this.lblPlatforms.TabIndex = 5;
        this.lblPlatforms.Text = "Nền tảng đăng bài";
        // 
        // _chkYouTube
        // 
        this._chkYouTube.AutoSize = true;
        this._chkYouTube.Checked = true;
        this._chkYouTube.CheckState = System.Windows.Forms.CheckState.Checked;
        this._chkYouTube.Location = new System.Drawing.Point(28, 184);
        this._chkYouTube.Name = "_chkYouTube";
        this._chkYouTube.TabIndex = 1;
        this._chkYouTube.Text = "YouTube";
        // 
        // _chkTikTok
        // 
        this._chkTikTok.AutoSize = true;
        this._chkTikTok.Location = new System.Drawing.Point(128, 184);
        this._chkTikTok.Name = "_chkTikTok";
        this._chkTikTok.TabIndex = 2;
        this._chkTikTok.Text = "TikTok";
        // 
        // _chkInstagram
        // 
        this._chkInstagram.AutoSize = true;
        this._chkInstagram.Location = new System.Drawing.Point(228, 184);
        this._chkInstagram.Name = "_chkInstagram";
        this._chkInstagram.TabIndex = 3;
        this._chkInstagram.Text = "Instagram";
        // 
        // _chkFacebook
        // 
        this._chkFacebook.AutoSize = true;
        this._chkFacebook.Location = new System.Drawing.Point(343, 184);
        this._chkFacebook.Name = "_chkFacebook";
        this._chkFacebook.TabIndex = 4;
        this._chkFacebook.Text = "Facebook";
        // 
        // lblPriority
        // 
        this.lblPriority.AutoSize = true;
        this.lblPriority.Font = UITheme.FontLabelBold;
        this.lblPriority.ForeColor = UITheme.Neutral700;
        this.lblPriority.Location = new System.Drawing.Point(28, 220);
        this.lblPriority.Name = "lblPriority";
        this.lblPriority.TabIndex = 10;
        this.lblPriority.Text = "Mức độ ưu tiên";
        // 
        // lblAssignee
        // 
        this.lblAssignee.AutoSize = true;
        this.lblAssignee.Font = UITheme.FontLabelBold;
        this.lblAssignee.ForeColor = UITheme.Neutral700;
        this.lblAssignee.Location = new System.Drawing.Point(248, 220);
        this.lblAssignee.Name = "lblAssignee";
        this.lblAssignee.TabIndex = 11;
        this.lblAssignee.Text = "Người phụ trách";
        // 
        // _cboPriority
        // 
        this._cboPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._cboPriority.Font = UITheme.FontBody;
        this._cboPriority.Location = new System.Drawing.Point(28, 240);
        this._cboPriority.Name = "_cboPriority";
        this._cboPriority.Size = new System.Drawing.Size(204, 29);
        this._cboPriority.TabIndex = 5;
        // 
        // _cboAssignee
        // 
        this._cboAssignee.DisplayMember = "Name";
        this._cboAssignee.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._cboAssignee.Font = UITheme.FontBody;
        this._cboAssignee.Location = new System.Drawing.Point(248, 240);
        this._cboAssignee.Name = "_cboAssignee";
        this._cboAssignee.Size = new System.Drawing.Size(204, 29);
        this._cboAssignee.TabIndex = 6;
        this._cboAssignee.ValueMember = "Id";
        // 
        // lblDeadline
        // 
        this.lblDeadline.AutoSize = true;
        this.lblDeadline.Font = UITheme.FontLabelBold;
        this.lblDeadline.ForeColor = UITheme.Neutral700;
        this.lblDeadline.Location = new System.Drawing.Point(28, 280);
        this.lblDeadline.Name = "lblDeadline";
        this.lblDeadline.TabIndex = 14;
        this.lblDeadline.Text = "Hạn hoàn thành (Deadline)";
        // 
        // _dtpDeadline
        // 
        this._dtpDeadline.Font = UITheme.FontBody;
        this._dtpDeadline.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this._dtpDeadline.Location = new System.Drawing.Point(28, 300);
        this._dtpDeadline.Name = "_dtpDeadline";
        this._dtpDeadline.Size = new System.Drawing.Size(204, 29);
        this._dtpDeadline.TabIndex = 7;
        // 
        // _lblError
        // 
        this._lblError.AutoSize = true;
        this._lblError.Font = UITheme.FontLabelBold;
        this._lblError.ForeColor = UITheme.Danger;
        this._lblError.Location = new System.Drawing.Point(248, 304);
        this._lblError.Name = "_lblError";
        this._lblError.TabIndex = 16;
        // 
        // pnlDivider2
        // 
        this.pnlDivider2.BackColor = UITheme.Neutral200;
        this.pnlDivider2.Location = new System.Drawing.Point(28, 348);
        this.pnlDivider2.Name = "pnlDivider2";
        this.pnlDivider2.Size = new System.Drawing.Size(424, 1);
        this.pnlDivider2.TabIndex = 17;
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(232, 364);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(90, 36);
        this.btnCancel.TabIndex = 9;
        this.btnCancel.Text = "Huỷ";
        this.btnCancel.UseVisualStyleBackColor = true;
        // 
        // btnSubmit
        // 
        this.btnSubmit.Location = new System.Drawing.Point(332, 364);
        this.btnSubmit.Name = "btnSubmit";
        this.btnSubmit.Size = new System.Drawing.Size(120, 36);
        this.btnSubmit.TabIndex = 8;
        this.btnSubmit.Text = "Tạo nội dung";
        this.btnSubmit.UseVisualStyleBackColor = true;
        this.btnSubmit.Click += new System.EventHandler(this.btnSubmit_Click);
        // 
        // CreateContentDialog
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.BackColor = UITheme.White;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(480, 420);
        this.Controls.Add(this.lblHeader);
        this.Controls.Add(this.lblSub);
        this.Controls.Add(this.pnlDivider);
        this.Controls.Add(this.lblTitle);
        this.Controls.Add(this._txtTitle);
        this.Controls.Add(this.lblPlatforms);
        this.Controls.Add(this._chkYouTube);
        this.Controls.Add(this._chkTikTok);
        this.Controls.Add(this._chkInstagram);
        this.Controls.Add(this._chkFacebook);
        this.Controls.Add(this.lblPriority);
        this.Controls.Add(this.lblAssignee);
        this.Controls.Add(this._cboPriority);
        this.Controls.Add(this._cboAssignee);
        this.Controls.Add(this.lblDeadline);
        this.Controls.Add(this._dtpDeadline);
        this.Controls.Add(this._lblError);
        this.Controls.Add(this.pnlDivider2);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSubmit);
        this.Font = UITheme.FontBody;
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "CreateContentDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Tạo nội dung mới";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
