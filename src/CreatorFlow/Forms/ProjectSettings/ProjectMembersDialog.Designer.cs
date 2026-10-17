#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.ProjectSettings;

partial class ProjectMembersDialog
{
    // Dispose(bool) đã có trong ProjectMembersDialog.cs nên không khai báo lại ở đây.

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        bodyLayout = new TableLayoutPanel();
        headingLabel = new Label();
        projectLabel = new Label();
        noticePanel = new Panel();
        noticeContent = new TableLayoutPanel();
        noticeIcon = new Label();
        noticeText = new Label();
        listHost = new Panel();
        members = new TableLayoutPanel();
        footerPanel = new Panel();
        actionsPanel = new FlowLayoutPanel();
        reload = new ProjectActionButton();
        close = new ProjectActionButton();
        message = new Label();
        bodyLayout.SuspendLayout();
        noticePanel.SuspendLayout();
        noticeContent.SuspendLayout();
        listHost.SuspendLayout();
        footerPanel.SuspendLayout();
        actionsPanel.SuspendLayout();
        SuspendLayout();
        //
        // bodyLayout
        //
        bodyLayout.BackColor = Color.White;
        bodyLayout.ColumnCount = 1;
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bodyLayout.Controls.Add(headingLabel, 0, 0);
        bodyLayout.Controls.Add(projectLabel, 0, 1);
        bodyLayout.Controls.Add(noticePanel, 0, 2);
        bodyLayout.Controls.Add(listHost, 0, 3);
        bodyLayout.Dock = DockStyle.Fill;
        bodyLayout.Location = new Point(0, 0);
        bodyLayout.Name = "bodyLayout";
        bodyLayout.Padding = new Padding(36, 22, 36, 20);
        bodyLayout.RowCount = 4;
        bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        bodyLayout.Size = new Size(860, 574);
        bodyLayout.TabIndex = 0;
        //
        // headingLabel
        //
        headingLabel.Dock = DockStyle.Fill;
        headingLabel.Margin = new Padding(0);
        headingLabel.Name = "headingLabel";
        headingLabel.TabIndex = 0;
        headingLabel.Text = "Thành viên";
        headingLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // projectLabel  (Text gán trong constructor theo projectName)
        //
        projectLabel.AutoEllipsis = true;
        projectLabel.Dock = DockStyle.Fill;
        projectLabel.ForeColor = Color.FromArgb(71, 85, 105);
        projectLabel.Margin = new Padding(0, 0, 0, 14);
        projectLabel.Name = "projectLabel";
        projectLabel.TabIndex = 1;
        projectLabel.TextAlign = ContentAlignment.TopLeft;
        //
        // noticePanel
        //
        noticePanel.BackColor = Color.FromArgb(255, 251, 235);
        noticePanel.Controls.Add(noticeContent);
        noticePanel.Dock = DockStyle.Fill;
        noticePanel.Margin = new Padding(0, 0, 0, 14);
        noticePanel.Name = "noticePanel";
        noticePanel.Padding = new Padding(18, 12, 18, 12);
        noticePanel.TabIndex = 2;
        noticePanel.SizeChanged += NoticePanel_SizeChanged;
        noticePanel.Paint += NoticePanel_Paint;
        //
        // noticeContent
        //
        noticeContent.BackColor = Color.FromArgb(255, 251, 235);
        noticeContent.ColumnCount = 2;
        noticeContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
        noticeContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        noticeContent.Controls.Add(noticeIcon, 0, 0);
        noticeContent.Controls.Add(noticeText, 1, 0);
        noticeContent.Dock = DockStyle.Fill;
        noticeContent.Margin = new Padding(0);
        noticeContent.Name = "noticeContent";
        noticeContent.RowCount = 1;
        noticeContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        noticeContent.TabIndex = 0;
        //
        // noticeIcon
        //
        noticeIcon.Dock = DockStyle.Fill;
        noticeIcon.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        noticeIcon.ForeColor = Color.FromArgb(217, 119, 6);
        noticeIcon.Margin = new Padding(0);
        noticeIcon.Name = "noticeIcon";
        noticeIcon.TabIndex = 0;
        noticeIcon.Text = "ⓘ";
        noticeIcon.TextAlign = ContentAlignment.MiddleLeft;
        //
        // noticeText
        //
        noticeText.Dock = DockStyle.Fill;
        noticeText.ForeColor = Color.FromArgb(146, 64, 14);
        noticeText.Margin = new Padding(0);
        noticeText.Name = "noticeText";
        noticeText.TabIndex = 1;
        noticeText.Text = "Chỉ Owner có thể đổi vai trò hoặc gỡ thành viên. Mỗi Project lưu vai trò riêng và phải luôn giữ ít nhất một Owner.";
        noticeText.TextAlign = ContentAlignment.MiddleLeft;
        //
        // listHost
        //
        listHost.AutoScroll = true;
        listHost.BackColor = Color.White;
        listHost.Controls.Add(members);
        listHost.Dock = DockStyle.Fill;
        listHost.Margin = new Padding(0);
        listHost.Name = "listHost";
        listHost.TabIndex = 3;
        //
        // members  (các dòng được thêm lúc chạy trong AddMember)
        //
        members.AutoSize = true;
        members.BackColor = Color.White;
        members.ColumnCount = 1;
        members.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        members.Dock = DockStyle.Top;
        members.Margin = new Padding(0);
        members.Name = "members";
        members.TabIndex = 0;
        //
        // footerPanel
        //
        footerPanel.BackColor = Color.FromArgb(248, 250, 252);
        footerPanel.Controls.Add(message);
        footerPanel.Controls.Add(actionsPanel);
        footerPanel.Dock = DockStyle.Bottom;
        footerPanel.Location = new Point(0, 574);
        footerPanel.Name = "footerPanel";
        footerPanel.Padding = new Padding(32, 12, 32, 12);
        footerPanel.Size = new Size(860, 76);
        footerPanel.TabIndex = 1;
        footerPanel.Paint += FooterPanel_Paint;
        //
        // actionsPanel
        //
        actionsPanel.AutoSize = true;
        actionsPanel.BackColor = Color.FromArgb(248, 250, 252);
        actionsPanel.Controls.Add(reload);
        actionsPanel.Controls.Add(close);
        actionsPanel.Dock = DockStyle.Right;
        actionsPanel.Margin = new Padding(0);
        actionsPanel.Name = "actionsPanel";
        actionsPanel.TabIndex = 1;
        actionsPanel.WrapContents = false;
        //
        // reload
        //
        reload.AccessibleName = "Tải lại danh sách thành viên";
        reload.Margin = new Padding(0, 0, 12, 0);
        reload.Name = "reload";
        reload.Size = new Size(112, 40);
        reload.Style = RoundButtonStyle.Secondary;
        reload.TabIndex = 0;
        reload.Text = "Tải lại";
        //
        // close
        //
        close.DialogResult = DialogResult.Cancel;
        close.Margin = new Padding(0);
        close.Name = "close";
        close.Size = new Size(96, 40);
        close.Style = RoundButtonStyle.Primary;
        close.TabIndex = 1;
        close.Text = "Đóng";
        //
        // message
        //
        message.AutoEllipsis = true;
        message.AutoSize = false;
        message.Dock = DockStyle.Fill;
        message.Margin = new Padding(0);
        message.Name = "message";
        message.TabIndex = 0;
        message.TextAlign = ContentAlignment.MiddleLeft;
        message.Visible = false;
        //
        // ProjectMembersDialog
        //
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = close;
        ClientSize = new Size(860, 650);
        Controls.Add(bodyLayout);
        Controls.Add(footerPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        KeyPreview = true;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(760, 560);
        Name = "ProjectMembersDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Thành viên Project";
        bodyLayout.ResumeLayout(false);
        noticePanel.ResumeLayout(false);
        noticeContent.ResumeLayout(false);
        listHost.ResumeLayout(false);
        listHost.PerformLayout();
        footerPanel.ResumeLayout(false);
        footerPanel.PerformLayout();
        actionsPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel bodyLayout;
    private Label headingLabel;
    private Label projectLabel;
    private Panel noticePanel;
    private TableLayoutPanel noticeContent;
    private Label noticeIcon;
    private Label noticeText;
    private Panel listHost;
    private TableLayoutPanel members;
    private Panel footerPanel;
    private FlowLayoutPanel actionsPanel;
    private ProjectActionButton reload;
    private ProjectActionButton close;
    private Label message;
}
