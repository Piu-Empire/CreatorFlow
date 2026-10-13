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
    private System.Windows.Forms.Label lblHook;
    private System.Windows.Forms.TextBox _txtHook;
    private System.Windows.Forms.Label lblPlatforms;
    private System.Windows.Forms.FlowLayoutPanel _pnlPlatforms;
    private System.Windows.Forms.Label lblPriority;
    private System.Windows.Forms.Label lblSprint;
    private System.Windows.Forms.ComboBox _cboPriority;
    private System.Windows.Forms.ComboBox _cboSprint;
    private System.Windows.Forms.Label lblAssignee;
    private System.Windows.Forms.Label lblDeadline;
    private System.Windows.Forms.ComboBox _cboAssignee;
    private System.Windows.Forms.DateTimePicker _dtpDeadline;
    private System.Windows.Forms.Label lblDuration;
    private System.Windows.Forms.TextBox _txtDuration;
    private System.Windows.Forms.Label lblPlannedPublish;
    private System.Windows.Forms.DateTimePicker _dtpPlannedPublish;
    private System.Windows.Forms.Label lblContentType;
    private System.Windows.Forms.ComboBox _cboContentType;
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
        this.lblHook = new System.Windows.Forms.Label();
        this._txtHook = new System.Windows.Forms.TextBox();
        this.lblPlatforms = new System.Windows.Forms.Label();
        this._pnlPlatforms = new System.Windows.Forms.FlowLayoutPanel();
        this.lblPriority = new System.Windows.Forms.Label();
        this.lblSprint = new System.Windows.Forms.Label();
        this._cboPriority = new System.Windows.Forms.ComboBox();
        this._cboSprint = new System.Windows.Forms.ComboBox();
        this.lblAssignee = new System.Windows.Forms.Label();
        this.lblDeadline = new System.Windows.Forms.Label();
        this._cboAssignee = new System.Windows.Forms.ComboBox();
        this._dtpDeadline = new System.Windows.Forms.DateTimePicker();
        this.lblDuration = new System.Windows.Forms.Label();
        this._txtDuration = new System.Windows.Forms.TextBox();
        this.lblPlannedPublish = new System.Windows.Forms.Label();
        this._dtpPlannedPublish = new System.Windows.Forms.DateTimePicker();
        this.lblContentType = new System.Windows.Forms.Label();
        this._cboContentType = new System.Windows.Forms.ComboBox();
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
        this.lblSub.Text = "Điền thông tin để đưa vào pipeline sản xuất.";
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
        // lblHook
        // 
        this.lblHook.AutoSize = true;
        this.lblHook.Font = UITheme.FontLabelBold;
        this.lblHook.ForeColor = UITheme.Neutral700;
        this.lblHook.Location = new System.Drawing.Point(28, 162);
        this.lblHook.Name = "lblHook";
        this.lblHook.TabIndex = 4;
        this.lblHook.Text = "Concept Hook";
        // 
        // _txtHook
        // 
        this._txtHook.Font = UITheme.FontBody;
        this._txtHook.Location = new System.Drawing.Point(28, 182);
        this._txtHook.Multiline = true;
        this._txtHook.Name = "_txtHook";
        this._txtHook.PlaceholderText = "Mô tả ngắn / ý tưởng mở đầu...";
        this._txtHook.Size = new System.Drawing.Size(424, 56);
        this._txtHook.TabIndex = 1;
        // 
        // lblPlatforms
        // 
        this.lblPlatforms.AutoSize = true;
        this.lblPlatforms.Font = UITheme.FontLabelBold;
        this.lblPlatforms.ForeColor = UITheme.Neutral700;
        this.lblPlatforms.Location = new System.Drawing.Point(28, 250);
        this.lblPlatforms.Name = "lblPlatforms";
        this.lblPlatforms.TabIndex = 5;
        this.lblPlatforms.Text = "Nền tảng đăng bài";
        // 
        // _pnlPlatforms
        // 
        this._pnlPlatforms.AutoSize = true;
        this._pnlPlatforms.Location = new System.Drawing.Point(28, 272);
        this._pnlPlatforms.Name = "_pnlPlatforms";
        this._pnlPlatforms.TabIndex = 2;
        this._pnlPlatforms.WrapContents = false;
        // 
        // lblPriority
        // 
        this.lblPriority.AutoSize = true;
        this.lblPriority.Font = UITheme.FontLabelBold;
        this.lblPriority.ForeColor = UITheme.Neutral700;
        this.lblPriority.Location = new System.Drawing.Point(28, 308);
        this.lblPriority.Name = "lblPriority";
        this.lblPriority.TabIndex = 10;
        this.lblPriority.Text = "Mức độ ưu tiên";
        // 
        // lblSprint
        // 
        this.lblSprint.AutoSize = true;
        this.lblSprint.Font = UITheme.FontLabelBold;
        this.lblSprint.ForeColor = UITheme.Neutral700;
        this.lblSprint.Location = new System.Drawing.Point(248, 308);
        this.lblSprint.Name = "lblSprint";
        this.lblSprint.TabIndex = 11;
        this.lblSprint.Text = "Sprint";
        // 
        // _cboPriority
        // 
        this._cboPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._cboPriority.Font = UITheme.FontBody;
        this._cboPriority.Location = new System.Drawing.Point(28, 328);
        this._cboPriority.Name = "_cboPriority";
        this._cboPriority.Size = new System.Drawing.Size(204, 29);
        this._cboPriority.TabIndex = 6;
        // 
        // _cboSprint
        // 
        this._cboSprint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._cboSprint.Font = UITheme.FontBody;
        this._cboSprint.Location = new System.Drawing.Point(248, 328);
        this._cboSprint.Name = "_cboSprint";
        this._cboSprint.Size = new System.Drawing.Size(204, 29);
        this._cboSprint.TabIndex = 7;
        // 
        // lblAssignee
        // 
        this.lblAssignee.AutoSize = true;
        this.lblAssignee.Font = UITheme.FontLabelBold;
        this.lblAssignee.ForeColor = UITheme.Neutral700;
        this.lblAssignee.Location = new System.Drawing.Point(28, 368);
        this.lblAssignee.Name = "lblAssignee";
        this.lblAssignee.TabIndex = 12;
        this.lblAssignee.Text = "Người phụ trách";
        // 
        // lblDeadline
        // 
        this.lblDeadline.AutoSize = true;
        this.lblDeadline.Font = UITheme.FontLabelBold;
        this.lblDeadline.ForeColor = UITheme.Neutral700;
        this.lblDeadline.Location = new System.Drawing.Point(248, 368);
        this.lblDeadline.Name = "lblDeadline";
        this.lblDeadline.TabIndex = 13;
        this.lblDeadline.Text = "Hạn hoàn thành";
        // 
        // _cboAssignee
        // 
        this._cboAssignee.DisplayMember = "Name";
        this._cboAssignee.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._cboAssignee.Font = UITheme.FontBody;
        this._cboAssignee.Location = new System.Drawing.Point(28, 388);
        this._cboAssignee.Name = "_cboAssignee";
        this._cboAssignee.Size = new System.Drawing.Size(204, 29);
        this._cboAssignee.TabIndex = 8;
        this._cboAssignee.ValueMember = "UserId";
        // 
        // _dtpDeadline
        // 
        this._dtpDeadline.Font = UITheme.FontBody;
        this._dtpDeadline.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this._dtpDeadline.Location = new System.Drawing.Point(248, 388);
        this._dtpDeadline.Name = "_dtpDeadline";
        this._dtpDeadline.Size = new System.Drawing.Size(204, 29);
        this._dtpDeadline.TabIndex = 9;
        // 
        // lblDuration
        // 
        this.lblDuration.AutoSize = true;
        this.lblDuration.Font = UITheme.FontLabelBold;
        this.lblDuration.ForeColor = UITheme.Neutral700;
        this.lblDuration.Location = new System.Drawing.Point(28, 428);
        this.lblDuration.Name = "lblDuration";
        this.lblDuration.TabIndex = 14;
        this.lblDuration.Text = "Thời lượng dự kiến";
        // 
        // _txtDuration
        // 
        this._txtDuration.Font = UITheme.FontBody;
        this._txtDuration.Location = new System.Drawing.Point(28, 448);
        this._txtDuration.Name = "_txtDuration";
        this._txtDuration.PlaceholderText = "VD: 60 sec, 24 min";
        this._txtDuration.Size = new System.Drawing.Size(204, 29);
        this._txtDuration.TabIndex = 10;
        // 
        // lblPlannedPublish
        // 
        this.lblPlannedPublish.AutoSize = true;
        this.lblPlannedPublish.Font = UITheme.FontLabelBold;
        this.lblPlannedPublish.ForeColor = UITheme.Neutral700;
        this.lblPlannedPublish.Location = new System.Drawing.Point(248, 428);
        this.lblPlannedPublish.Name = "lblPlannedPublish";
        this.lblPlannedPublish.TabIndex = 15;
        this.lblPlannedPublish.Text = "Ngày dự kiến đăng";
        // 
        // _dtpPlannedPublish
        // 
        this._dtpPlannedPublish.Checked = false;
        this._dtpPlannedPublish.Font = UITheme.FontBody;
        this._dtpPlannedPublish.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this._dtpPlannedPublish.Location = new System.Drawing.Point(248, 448);
        this._dtpPlannedPublish.Name = "_dtpPlannedPublish";
        this._dtpPlannedPublish.ShowCheckBox = true;
        this._dtpPlannedPublish.Size = new System.Drawing.Size(204, 29);
        this._dtpPlannedPublish.TabIndex = 11;
        // 
        // lblContentType
        // 
        this.lblContentType.AutoSize = true;
        this.lblContentType.Font = UITheme.FontLabelBold;
        this.lblContentType.ForeColor = UITheme.Neutral700;
        this.lblContentType.Location = new System.Drawing.Point(28, 488);
        this.lblContentType.Name = "lblContentType";
        this.lblContentType.TabIndex = 16;
        this.lblContentType.Text = "Loại nội dung *";
        // 
        // _cboContentType
        // 
        this._cboContentType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._cboContentType.Font = UITheme.FontBody;
        this._cboContentType.Location = new System.Drawing.Point(28, 508);
        this._cboContentType.Name = "_cboContentType";
        this._cboContentType.Size = new System.Drawing.Size(204, 29);
        this._cboContentType.TabIndex = 12;
        // 
        // _lblError
        // 
        this._lblError.AutoSize = true;
        this._lblError.Font = UITheme.FontLabelBold;
        this._lblError.ForeColor = UITheme.Danger;
        this._lblError.Location = new System.Drawing.Point(248, 490);
        this._lblError.MaximumSize = new System.Drawing.Size(204, 0);
        this._lblError.Name = "_lblError";
        this._lblError.TabIndex = 17;
        // 
        // pnlDivider2
        // 
        this.pnlDivider2.BackColor = UITheme.Neutral200;
        this.pnlDivider2.Location = new System.Drawing.Point(28, 556);
        this.pnlDivider2.Name = "pnlDivider2";
        this.pnlDivider2.Size = new System.Drawing.Size(424, 1);
        this.pnlDivider2.TabIndex = 18;
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(232, 572);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(90, 36);
        this.btnCancel.TabIndex = 13;
        this.btnCancel.Text = "Huỷ";
        this.btnCancel.UseVisualStyleBackColor = true;
        // 
        // btnSubmit
        // 
        this.btnSubmit.Location = new System.Drawing.Point(332, 572);
        this.btnSubmit.Name = "btnSubmit";
        this.btnSubmit.Size = new System.Drawing.Size(120, 36);
        this.btnSubmit.TabIndex = 14;
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
        this.ClientSize = new System.Drawing.Size(480, 628);
        this.Controls.Add(this.lblHeader);
        this.Controls.Add(this.lblSub);
        this.Controls.Add(this.pnlDivider);
        this.Controls.Add(this.lblTitle);
        this.Controls.Add(this._txtTitle);
        this.Controls.Add(this.lblHook);
        this.Controls.Add(this._txtHook);
        this.Controls.Add(this.lblPlatforms);
        this.Controls.Add(this._pnlPlatforms);
        this.Controls.Add(this.lblPriority);
        this.Controls.Add(this.lblSprint);
        this.Controls.Add(this._cboPriority);
        this.Controls.Add(this._cboSprint);
        this.Controls.Add(this.lblAssignee);
        this.Controls.Add(this.lblDeadline);
        this.Controls.Add(this._cboAssignee);
        this.Controls.Add(this._dtpDeadline);
        this.Controls.Add(this.lblDuration);
        this.Controls.Add(this._txtDuration);
        this.Controls.Add(this.lblPlannedPublish);
        this.Controls.Add(this._dtpPlannedPublish);
        this.Controls.Add(this.lblContentType);
        this.Controls.Add(this._cboContentType);
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