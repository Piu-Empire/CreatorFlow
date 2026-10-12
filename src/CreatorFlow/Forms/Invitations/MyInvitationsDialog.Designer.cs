#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Invitations;

partial class MyInvitationsDialog
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label lblHeader;
    private System.Windows.Forms.Label lblSub;
    private System.Windows.Forms.Panel pnlDivider;
    private System.Windows.Forms.Label lblList;
    private System.Windows.Forms.ListView lvInvitations;
    private System.Windows.Forms.ColumnHeader colProject;
    private System.Windows.Forms.ColumnHeader colRole;
    private System.Windows.Forms.ColumnHeader colInvitedBy;
    private System.Windows.Forms.ColumnHeader colStatus;
    private System.Windows.Forms.ColumnHeader colExpires;
    private System.Windows.Forms.Label _lblMessage;
    private System.Windows.Forms.Panel pnlDivider2;
    private CreatorFlow.Controls.RoundedButton btnReject;
    private CreatorFlow.Controls.RoundedButton btnAccept;
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
        this.lblList = new System.Windows.Forms.Label();
        this.lvInvitations = new System.Windows.Forms.ListView();
        this.colProject = new System.Windows.Forms.ColumnHeader();
        this.colRole = new System.Windows.Forms.ColumnHeader();
        this.colInvitedBy = new System.Windows.Forms.ColumnHeader();
        this.colStatus = new System.Windows.Forms.ColumnHeader();
        this.colExpires = new System.Windows.Forms.ColumnHeader();
        this._lblMessage = new System.Windows.Forms.Label();
        this.pnlDivider2 = new System.Windows.Forms.Panel();
        this.btnReject = new CreatorFlow.Controls.RoundedButton();
        this.btnAccept = new CreatorFlow.Controls.RoundedButton();
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
        this.lblHeader.Text = "Lời mời của tôi";
        //
        // lblSub
        //
        this.lblSub.AutoEllipsis = true;
        this.lblSub.Font = UITheme.FontBody;
        this.lblSub.ForeColor = UITheme.Neutral600;
        this.lblSub.Location = new System.Drawing.Point(28, 52);
        this.lblSub.Name = "lblSub";
        this.lblSub.Size = new System.Drawing.Size(624, 22);
        this.lblSub.TabIndex = 1;
        this.lblSub.Text = "Các lời mời tham gia Project được gửi tới email tài khoản của bạn.";
        this.lblSub.UseMnemonic = false;
        //
        // pnlDivider
        //
        this.pnlDivider.BackColor = UITheme.Neutral200;
        this.pnlDivider.Location = new System.Drawing.Point(28, 86);
        this.pnlDivider.Name = "pnlDivider";
        this.pnlDivider.Size = new System.Drawing.Size(624, 1);
        this.pnlDivider.TabIndex = 2;
        //
        // lblList
        //
        this.lblList.AutoSize = true;
        this.lblList.Font = UITheme.FontLabelBold;
        this.lblList.ForeColor = UITheme.Neutral700;
        this.lblList.Location = new System.Drawing.Point(28, 102);
        this.lblList.Name = "lblList";
        this.lblList.TabIndex = 3;
        this.lblList.Text = "Lời mời (0)";
        //
        // lvInvitations
        //
        this.lvInvitations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colProject, this.colRole, this.colInvitedBy, this.colStatus, this.colExpires});
        this.lvInvitations.Font = UITheme.FontBody;
        this.lvInvitations.FullRowSelect = true;
        this.lvInvitations.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
        this.lvInvitations.HideSelection = false;
        this.lvInvitations.Location = new System.Drawing.Point(28, 124);
        this.lvInvitations.MultiSelect = false;
        this.lvInvitations.Name = "lvInvitations";
        this.lvInvitations.Size = new System.Drawing.Size(624, 220);
        this.lvInvitations.TabIndex = 0;
        this.lvInvitations.UseCompatibleStateImageBehavior = false;
        this.lvInvitations.View = System.Windows.Forms.View.Details;
        //
        // colProject / colRole / colInvitedBy / colStatus / colExpires  (tổng 606px, chừa chỗ thanh cuộn)
        //
        this.colProject.Text = "Project";
        this.colProject.Width = 170;
        this.colRole.Text = "Vai trò";
        this.colRole.Width = 76;
        this.colInvitedBy.Text = "Người mời";
        this.colInvitedBy.Width = 120;
        this.colStatus.Text = "Trạng thái";
        this.colStatus.Width = 100;
        this.colExpires.Text = "Hết hạn";
        this.colExpires.Width = 140;
        //
        // _lblMessage  (lỗi: đỏ #DC2626; thành công: xanh; đang tải: xám — 11px đậm, style guide mục 13)
        //
        this._lblMessage.Font = UITheme.FontLabelBold;
        this._lblMessage.ForeColor = UITheme.Danger;
        this._lblMessage.Location = new System.Drawing.Point(28, 356);
        this._lblMessage.Name = "_lblMessage";
        this._lblMessage.Size = new System.Drawing.Size(624, 36);
        this._lblMessage.TabIndex = 4;
        this._lblMessage.UseMnemonic = false;
        //
        // pnlDivider2
        //
        this.pnlDivider2.BackColor = UITheme.Neutral200;
        this.pnlDivider2.Location = new System.Drawing.Point(28, 402);
        this.pnlDivider2.Name = "pnlDivider2";
        this.pnlDivider2.Size = new System.Drawing.Size(624, 1);
        this.pnlDivider2.TabIndex = 5;
        //
        // btnReject
        //
        this.btnReject.Location = new System.Drawing.Point(306, 416);
        this.btnReject.Name = "btnReject";
        this.btnReject.Size = new System.Drawing.Size(110, 36);
        this.btnReject.Style = RoundButtonStyle.Danger;
        this.btnReject.TabIndex = 1;
        this.btnReject.Text = "Từ chối";
        this.btnReject.UseVisualStyleBackColor = true;
        this.btnReject.Click += new System.EventHandler(this.btnReject_Click);
        //
        // btnAccept
        //
        this.btnAccept.Location = new System.Drawing.Point(424, 416);
        this.btnAccept.Name = "btnAccept";
        this.btnAccept.Size = new System.Drawing.Size(130, 36);
        this.btnAccept.Style = RoundButtonStyle.Primary;
        this.btnAccept.TabIndex = 2;
        this.btnAccept.Text = "Chấp nhận";
        this.btnAccept.UseVisualStyleBackColor = true;
        this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click);
        //
        // btnClose
        //
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnClose.Location = new System.Drawing.Point(562, 416);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(90, 36);
        this.btnClose.Style = RoundButtonStyle.Secondary;
        this.btnClose.TabIndex = 3;
        this.btnClose.Text = "Đóng";
        this.btnClose.UseVisualStyleBackColor = true;
        //
        // MyInvitationsDialog
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.BackColor = UITheme.White;
        this.ClientSize = new System.Drawing.Size(680, 480);
        this.Controls.Add(this.lblHeader);
        this.Controls.Add(this.lblSub);
        this.Controls.Add(this.pnlDivider);
        this.Controls.Add(this.lblList);
        this.Controls.Add(this.lvInvitations);
        this.Controls.Add(this._lblMessage);
        this.Controls.Add(this.pnlDivider2);
        this.Controls.Add(this.btnReject);
        this.Controls.Add(this.btnAccept);
        this.Controls.Add(this.btnClose);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "MyInvitationsDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Lời mời của tôi";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
