using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed class RestoreProjectDialog : Form
{
    public RestoreProjectDialog(string projectName)
    {
        Name = nameof(RestoreProjectDialog);
        Text = "Khôi phục Project";
        Font = UITheme.FontBody;
        BackColor = UITheme.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 270);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = UITheme.White
        };
        var title = new Label
        {
            Text = "Khôi phục Project",
            Location = new Point(28, 14),
            Size = new Size(500, 30),
            Font = UITheme.FontH2,
            ForeColor = UITheme.Ink
        };
        var subtitle = new Label
        {
            Text = "Project sẽ trở lại danh sách đang hoạt động.",
            Location = new Point(28, 49),
            Size = new Size(500, 22),
            Font = new Font("Segoe UI", 10F),
            ForeColor = UITheme.Neutral600
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral100);
            e.Graphics.DrawLine(pen, 0, header.ClientSize.Height - 1, header.ClientSize.Width, header.ClientSize.Height - 1);
        };

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 18, 28, 12),
            BackColor = UITheme.White
        };
        body.Controls.Add(new Label
        {
            Text = $"Khôi phục Project “{projectName}”?\nCác thành viên có thể tiếp tục làm việc trong Project sau khi khôi phục.",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = UITheme.Ink,
            Font = new Font("Segoe UI", 10.5F)
        });

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            Padding = new Padding(24, 16, 28, 16),
            BackColor = UITheme.Neutral50
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 228,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var cancel = new ProjectActionButton
        {
            Name = "restoreProjectCancel",
            Text = "Hủy",
            Style = RoundButtonStyle.Secondary,
            DialogResult = DialogResult.Cancel,
            Width = 88,
            Height = 42,
            Margin = new Padding(0, 0, 12, 0)
        };
        var confirm = new ProjectActionButton
        {
            Name = "restoreProjectConfirm",
            Text = "Khôi phục",
            DialogResult = DialogResult.OK,
            Width = 128,
            Height = 42,
            Margin = Padding.Empty
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(confirm);
        footer.Controls.Add(buttons);
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral100);
            e.Graphics.DrawLine(pen, 0, 0, footer.ClientSize.Width, 0);
        };

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        AcceptButton = confirm;
        CancelButton = cancel;
    }
}
