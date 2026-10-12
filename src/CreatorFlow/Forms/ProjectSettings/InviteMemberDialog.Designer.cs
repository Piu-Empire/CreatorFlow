#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.ProjectSettings;

partial class InviteMemberDialog
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label lblHeader;
    private System.Windows.Forms.Label lblSub;
    private System.Windows.Forms.Panel pnlDivider;
    private System.Windows.Forms.Label lblEmail;
    private System.Windows.Forms.TextBox txtEmail;
    private System.Windows.Forms.Label lblRole;
    private System.Windows.Forms.ComboBox cboRole;
    private CreatorFlow.Controls.RoundedButton btnInvite;
    private System.Windows.Forms.Label _lblMessage;
    private System.Windows.Forms.Panel pnlDivider2;
    private System.Windows.Forms.Label lblList;
    private System.Windows.Forms.ListView lvInvitations;
    private System.Windows.Forms.ColumnHeader colEmail;
    private System.Windows.Forms.ColumnHeader colRole;
    private System.Windows.Forms.ColumnHeader colStatus;
    private System.Windows.Forms.ColumnHeader colExpires;
    private System.Windows.Forms.Panel pnlDivider3;
    private CreatorFlow.Controls.RoundedButton btnClose;

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
        this.lblEmail = new System.Windows.Forms.Label();
        this.txtEmail = new System.Windows.Forms.TextBox();
        this.lblRole = new System.Windows.Forms.Label();
        this.cboRole = new System.Windows.Forms.ComboBox();
        this.btnInvite = new CreatorFlow.Controls.RoundedButton();
        this._lblMessage = new System.Windows.Forms.Label();
        this.pnlDivider2 = new System.Windows.Forms.Panel();
        this.lblList = new System.Windows.Forms.Label();
        this.lvInvitations = new System.Windows.Forms.ListView();
        this.colEmail = new System.Windows.Forms.ColumnHeader();
        this.colRole = new System.Windows.Forms.ColumnHeader();
        this.colStatus = new System.Windows.Forms.ColumnHeader();
        this.colExpires = new System.Windows.Forms.ColumnHeader();
        this.pnlDivider3 = new System.Windows.Forms.Panel();
        this.btnClose = new CreatorFlow.Controls.RoundedButton();
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
        this.lblHeader.Text = "Mời thành viên";
        // 
        // lblSub  (tên Project, gán trong code-behind)
        // 
        this.lblSub.AutoEllipsis = true;
        this.lblSub.Font = UITheme.FontBody;
        this.lblSub.ForeColor = UITheme.Neutral600;
        this.lblSub.Location = new System.Drawing.Point(28, 52);
        this.lblSub.Name = "lblSub";
        this.lblSub.Size = new System.Drawing.Size(544, 22);
        this.lblSub.TabIndex = 1;
        this.lblSub.UseMnemonic = false;
        // 
        // pnlDivider
        // 
        this.pnlDivider.BackColor = UITheme.Neutral200;
        this.pnlDivider.Location = new System.Drawing.Point(28, 86);
        this.pnlDivider.Name = "pnlDivider";
        this.pnlDivider.Size = new System.Drawing.Size(544, 1);
        this.pnlDivider.TabIndex = 2;
        // 
        // lblEmail
        // 
        this.lblEmail.AutoSize = true;
        this.lblEmail.Font = UITheme.FontLabelBold;
        this.lblEmail.ForeColor = UITheme.Neutral700;
        this.lblEmail.Location = new System.Drawing.Point(28, 102);
        this.lblEmail.Name = "lblEmail";
        this.lblEmail.TabIndex = 3;
        this.lblEmail.Text = "Email người được mời *";
        // 
        // txtEmail
        // 
        this.txtEmail.Font = UITheme.FontBody;
        this.txtEmail.Location = new System.Drawing.Point(28, 122);
        this.txtEmail.MaxLength = 255;
        this.txtEmail.Name = "txtEmail";
        this.txtEmail.Size = new System.Drawing.Size(544, 29);
        this.txtEmail.TabIndex = 0;
        // 
        // lblRole
        // 
        this.lblRole.AutoSize = true;
        this.lblRole.Font = UITheme.FontLabelBold;
        this.lblRole.ForeColor = UITheme.Neutral700;
        this.lblRole.Location = new System.Drawing.Point(28, 164);
        this.lblRole.Name = "lblRole";
        this.lblRole.TabIndex = 5;
        this.lblRole.Text = "Vai trò trong Project *";
        // 
        // cboRole
        // 
        this.cboRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboRole.Font = UITheme.FontBody;
        this.cboRole.Location = new System.Drawing.Point(28, 184);
        this.cboRole.Name = "cboRole";
        this.cboRole.Size = new System.Drawing.Size(240, 29);
        this.cboRole.TabIndex = 1;
        // 
        // btnInvite
        // 
        this.btnInvite.Location = new System.Drawing.Point(452, 180);
        this.btnInvite.Name = "btnInvite";
        this.btnInvite.Size = new System.Drawing.Size(120, 36);
        this.btnInvite.Style = RoundButtonStyle.Primary;
        this.btnInvite.TabIndex = 2;
        this.btnInvite.Text = "Gửi lời mời";
        this.btnInvite.UseVisualStyleBackColor = true;
        this.btnInvite.Click += new System.EventHandler(this.btnInvite_Click);
        // 
        // _lblMessage  (lỗi: đỏ #DC2626; thành công: xanh; đang tải: xám — 11px đậm, style guide mục 13)
        // 
        this._lblMessage.Font = UITheme.FontLabelBold;
        this._lblMessage.ForeColor = UITheme.Danger;
        this._lblMessage.Location = new System.Drawing.Point(28, 224);
        this._lblMessage.Name = "_lblMessage";
        this._lblMessage.Size = new System.Drawing.Size(544, 36);
        this._lblMessage.TabIndex = 7;
        this._lblMessage.UseMnemonic = false;
        // 
        // pnlDivider2
        // 
        this.pnlDivider2.BackColor = UITheme.Neutral200;
        this.pnlDivider2.Location = new System.Drawing.Point(28, 268);
        this.pnlDivider2.Name = "pnlDivider2";
        this.pnlDivider2.Size = new System.Drawing.Size(544, 1);
        this.pnlDivider2.TabIndex = 8;
        // 
        // lblList
        // 
        this.lblList.AutoSize = true;
        this.lblList.Font = UITheme.FontLabelBold;
        this.lblList.ForeColor = UITheme.Neutral700;
        this.lblList.Location = new System.Drawing.Point(28, 282);
        this.lblList.Name = "lblList";
        this.lblList.TabIndex = 9;
        this.lblList.Text = "Lời mời đã gửi (0)";
        // 
        // lvInvitations
        // 
        this.lvInvitations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colEmail, this.colRole, this.colStatus, this.colExpires});
        this.lvInvitations.Font = UITheme.FontBody;
        this.lvInvitations.FullRowSelect = true;
        this.lvInvitations.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
        this.lvInvitations.Location = new System.Drawing.Point(28, 304);
        this.lvInvitations.MultiSelect = false;
        this.lvInvitations.Name = "lvInvitations";
        this.lvInvitations.Size = new System.Drawing.Size(544, 200);
        this.lvInvitations.TabIndex = 3;
        this.lvInvitations.UseCompatibleStateImageBehavior = false;
        this.lvInvitations.View = System.Windows.Forms.View.Details;
        // 
        // colEmail / colRole / colStatus / colExpires  (tổng 520px, chừa chỗ thanh cuộn)
        // 
        this.colEmail.Text = "Email";
        this.colEmail.Width = 220;
        this.colRole.Text = "Vai trò";
        this.colRole.Width = 80;
        this.colStatus.Text = "Trạng thái";
        this.colStatus.Width = 100;
        this.colExpires.Text = "Hết hạn";
        this.colExpires.Width = 120;
        // 
        // pnlDivider3
        // 
        this.pnlDivider3.BackColor = UITheme.Neutral200;
        this.pnlDivider3.Location = new System.Drawing.Point(28, 516);
        this.pnlDivider3.Name = "pnlDivider3";
        this.pnlDivider3.Size = new System.Drawing.Size(544, 1);
        this.pnlDivider3.TabIndex = 11;
        // 
        // btnClose
        // 
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnClose.Location = new System.Drawing.Point(482, 530);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(90, 36);
        this.btnClose.Style = RoundButtonStyle.Secondary;
        this.btnClose.TabIndex = 4;
        this.btnClose.Text = "Đóng";
        this.btnClose.UseVisualStyleBackColor = true;
        // 
        // InviteMemberDialog
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.BackColor = UITheme.White;
        this.ClientSize = new System.Drawing.Size(600, 590);
        this.Controls.Add(this.lblHeader);
        this.Controls.Add(this.lblSub);
        this.Controls.Add(this.pnlDivider);
        this.Controls.Add(this.lblEmail);
        this.Controls.Add(this.txtEmail);
        this.Controls.Add(this.lblRole);
        this.Controls.Add(this.cboRole);
        this.Controls.Add(this.btnInvite);
        this.Controls.Add(this._lblMessage);
        this.Controls.Add(this.pnlDivider2);
        this.Controls.Add(this.lblList);
        this.Controls.Add(this.lvInvitations);
        this.Controls.Add(this.pnlDivider3);
        this.Controls.Add(this.btnClose);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "InviteMemberDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Mời thành viên";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
