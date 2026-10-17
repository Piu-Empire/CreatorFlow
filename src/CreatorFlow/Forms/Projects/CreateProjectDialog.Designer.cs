#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

partial class CreateProjectDialog
{
    // Dispose(bool) đã có trong CreateProjectDialog.cs nên không khai báo lại ở đây.

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        headerPanel = new Panel();
        titleLabel = new Label();
        explanationLabel = new Label();
        bodyPanel = new Panel();
        footerPanel = new Panel();
        buttonsPanel = new FlowLayoutPanel();
        cancel = new ProjectActionButton();
        confirm = new ProjectActionButton();
        headerPanel.SuspendLayout();
        footerPanel.SuspendLayout();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        //
        // headerPanel
        //
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(explanationLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(760, 130);
        headerPanel.TabIndex = 2;
        headerPanel.Paint += HeaderPanel_Paint;
        //
        // titleLabel
        //
        titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        titleLabel.AutoEllipsis = true;
        titleLabel.Location = new Point(38, 24);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(684, 34);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Tạo Project";
        //
        // explanationLabel  (Text gán trong constructor theo discardDraft)
        //
        explanationLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        explanationLabel.AutoEllipsis = true;
        explanationLabel.Font = new Font("Segoe UI", 12F);
        explanationLabel.Location = new Point(38, 68);
        explanationLabel.Name = "explanationLabel";
        explanationLabel.Size = new Size(684, 30);
        explanationLabel.TabIndex = 1;
        //
        // bodyPanel  (các ô nhập được thêm lúc chạy trong BuildFields)
        //
        bodyPanel.AutoScroll = true;
        bodyPanel.Dock = DockStyle.Fill;
        bodyPanel.Location = new Point(0, 0);
        bodyPanel.Name = "bodyPanel";
        bodyPanel.Padding = new Padding(38, 24, 38, 20);
        bodyPanel.Size = new Size(760, 372);
        bodyPanel.TabIndex = 0;
        //
        // footerPanel
        //
        footerPanel.Controls.Add(buttonsPanel);
        footerPanel.Dock = DockStyle.Bottom;
        footerPanel.Location = new Point(0, 502);
        footerPanel.Name = "footerPanel";
        footerPanel.Padding = new Padding(38, 17, 38, 17);
        footerPanel.Size = new Size(760, 88);
        footerPanel.TabIndex = 1;
        footerPanel.Paint += FooterPanel_Paint;
        //
        // buttonsPanel
        //
        buttonsPanel.Controls.Add(cancel);
        buttonsPanel.Controls.Add(confirm);
        buttonsPanel.Dock = DockStyle.Right;
        buttonsPanel.Location = new Point(448, 17);
        buttonsPanel.Margin = new Padding(0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(274, 54);
        buttonsPanel.TabIndex = 0;
        buttonsPanel.WrapContents = false;
        //
        // cancel
        //
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(0, 0);
        cancel.Margin = new Padding(0, 0, 16, 0);
        cancel.Name = "createProjectCancel";
        cancel.Size = new Size(96, 52);
        cancel.Style = RoundButtonStyle.Secondary;
        cancel.TabIndex = 0;
        cancel.Text = "Hủy";
        //
        // confirm
        //
        confirm.Location = new Point(112, 0);
        confirm.Margin = new Padding(0);
        confirm.Name = "createProjectConfirm";
        confirm.Size = new Size(162, 52);
        confirm.TabIndex = 1;
        confirm.Text = "Tạo Project";
        //
        // CreateProjectDialog
        //
        AcceptButton = confirm;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = cancel;
        ClientSize = new Size(760, 590);
        Controls.Add(bodyPanel);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "CreateProjectDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Tạo Project";
        headerPanel.ResumeLayout(false);
        footerPanel.ResumeLayout(false);
        buttonsPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel headerPanel;
    private Label titleLabel;
    private Label explanationLabel;
    private Panel bodyPanel;
    private Panel footerPanel;
    private FlowLayoutPanel buttonsPanel;
    private ProjectActionButton cancel;
    private ProjectActionButton confirm;
}
