using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Projects;

public sealed partial class RestoreProjectDialog : Form
{
    public RestoreProjectDialog(string projectName)
    {
        InitializeComponent();

        Font = UITheme.FontBody;
        BackColor = UITheme.White;
        titleLabel.Font = UITheme.FontH2;
        titleLabel.ForeColor = UITheme.Ink;
        subtitleLabel.ForeColor = UITheme.Neutral600;
        messageLabel.ForeColor = UITheme.Ink;
        messageLabel.Text = $"Khôi phục Project “{projectName}”?\nCác thành viên có thể tiếp tục làm việc trong Project sau khi khôi phục.";
    }

    private void HeaderPanel_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.Neutral100);
        e.Graphics.DrawLine(pen, 0, headerPanel.ClientSize.Height - 1,
            headerPanel.ClientSize.Width, headerPanel.ClientSize.Height - 1);
    }

    private void FooterPanel_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.Neutral100);
        e.Graphics.DrawLine(pen, 0, 0, footerPanel.ClientSize.Width, 0);
    }
}
