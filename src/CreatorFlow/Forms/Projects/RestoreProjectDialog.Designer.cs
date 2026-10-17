#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

partial class RestoreProjectDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        headerPanel = new Panel();
        titleLabel = new Label();
        subtitleLabel = new Label();
        bodyPanel = new Panel();
        messageLabel = new Label();
        footerPanel = new Panel();
        buttonsPanel = new FlowLayoutPanel();
        cancelButton = new ProjectActionButton();
        confirmButton = new ProjectActionButton();
        headerPanel.SuspendLayout();
        bodyPanel.SuspendLayout();
        footerPanel.SuspendLayout();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        //
        // headerPanel
        //
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(560, 88);
        headerPanel.TabIndex = 2;
        headerPanel.Paint += HeaderPanel_Paint;
        //
        // titleLabel
        //
        titleLabel.Location = new Point(28, 14);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(500, 30);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Khôi phục Project";
        //
        // subtitleLabel
        //
        subtitleLabel.Font = new Font("Segoe UI", 10F);
        subtitleLabel.Location = new Point(28, 49);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(500, 22);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "Project sẽ trở lại danh sách đang hoạt động.";
        //
        // bodyPanel
        //
        bodyPanel.Controls.Add(messageLabel);
        bodyPanel.Dock = DockStyle.Fill;
        bodyPanel.Location = new Point(0, 0);
        bodyPanel.Name = "bodyPanel";
        bodyPanel.Padding = new Padding(28, 18, 28, 12);
        bodyPanel.Size = new Size(560, 106);
        bodyPanel.TabIndex = 0;
        //
        // messageLabel  (Text được gán trong constructor vì phụ thuộc projectName)
        //
        messageLabel.AutoEllipsis = true;
        messageLabel.Dock = DockStyle.Fill;
        messageLabel.Font = new Font("Segoe UI", 10.5F);
        messageLabel.Location = new Point(28, 18);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(504, 76);
        messageLabel.TabIndex = 0;
        //
        // footerPanel
        //
        footerPanel.Controls.Add(buttonsPanel);
        footerPanel.Dock = DockStyle.Bottom;
        footerPanel.Location = new Point(0, 194);
        footerPanel.Name = "footerPanel";
        footerPanel.Padding = new Padding(24, 16, 28, 16);
        footerPanel.Size = new Size(560, 76);
        footerPanel.TabIndex = 1;
        footerPanel.Paint += FooterPanel_Paint;
        //
        // buttonsPanel
        //
        buttonsPanel.Controls.Add(cancelButton);
        buttonsPanel.Controls.Add(confirmButton);
        buttonsPanel.Dock = DockStyle.Right;
        buttonsPanel.Location = new Point(308, 16);
        buttonsPanel.Margin = new Padding(0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(228, 44);
        buttonsPanel.TabIndex = 0;
        buttonsPanel.WrapContents = false;
        //
        // cancelButton
        //
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(0, 0);
        cancelButton.Margin = new Padding(0, 0, 12, 0);
        cancelButton.Name = "restoreProjectCancel";
        cancelButton.Size = new Size(88, 42);
        cancelButton.Style = RoundButtonStyle.Secondary;
        cancelButton.TabIndex = 0;
        cancelButton.Text = "Hủy";
        //
        // confirmButton
        //
        confirmButton.DialogResult = DialogResult.OK;
        confirmButton.Location = new Point(100, 0);
        confirmButton.Margin = new Padding(0);
        confirmButton.Name = "restoreProjectConfirm";
        confirmButton.Size = new Size(128, 42);
        confirmButton.TabIndex = 1;
        confirmButton.Text = "Khôi phục";
        //
        // RestoreProjectDialog
        //
        AcceptButton = confirmButton;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = cancelButton;
        ClientSize = new Size(560, 270);
        Controls.Add(bodyPanel);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "RestoreProjectDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Khôi phục Project";
        headerPanel.ResumeLayout(false);
        bodyPanel.ResumeLayout(false);
        footerPanel.ResumeLayout(false);
        buttonsPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel headerPanel;
    private Label titleLabel;
    private Label subtitleLabel;
    private Panel bodyPanel;
    private Label messageLabel;
    private Panel footerPanel;
    private FlowLayoutPanel buttonsPanel;
    private ProjectActionButton cancelButton;
    private ProjectActionButton confirmButton;
}
