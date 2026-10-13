using CreatorFlow.Helpers;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

public sealed class ProjectPageHeaderControl : Panel
{
    public event EventHandler? CreateRequested;
    public event EventHandler? ReloadRequested;
    private readonly Label title;
    private readonly Label description;
    private readonly FlowLayoutPanel actions;
    public ProjectPageHeaderControl(string caption, string explanation)
    {
        BackColor = UITheme.White; Padding = new(20); Height = 112; UiTheme.StyleCard(this);
        title = new Label { Text = caption, Font = UITheme.FontH1, AutoEllipsis = true };
        description = new Label { Text = explanation, Font = UITheme.FontBody, ForeColor = UITheme.Neutral600, AutoEllipsis = true };
        actions = new FlowLayoutPanel { Width = 280, Height = 40, WrapContents = false };
        var retry = new ProjectActionButton { Text = "Làm mới", Style = RoundButtonStyle.Ghost, Width = 112, Height = 36, Margin = new(0, 0, 8, 0) };
        var create = new ProjectActionButton { Text = "Thêm Project", ShowPlusIcon = true, Width = 156, Height = 36, Margin = Padding.Empty };
        actions.Controls.AddRange([retry, create]); Controls.AddRange([title, description, actions]);
        retry.Click += (_, _) => ReloadRequested?.Invoke(this, EventArgs.Empty);
        create.Click += (_, _) => CreateRequested?.Invoke(this, EventArgs.Empty);
        SizeChanged += (_, _) => Arrange(); Arrange();
    }
    private void Arrange()
    {
        bool narrow = ClientSize.Width < 720;
        Height = narrow ? 156 : 112;
        int textWidth = Math.Max(1, ClientSize.Width - 40 - (narrow ? 0 : actions.Width + 24));
        title.SetBounds(20, 20, textWidth, 32); description.SetBounds(20, 60, textWidth, 32);
        actions.Location = narrow ? new Point(20, 108) : new Point(ClientSize.Width - 20 - actions.Width, 36);
    }
}
