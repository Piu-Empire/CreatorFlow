using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

public sealed class ProjectSwitcherControl : UserControl
{
    private readonly ProjectApiFacade projects;
    private readonly IReadOnlyList<long> recent;
    private readonly CancellationTokenSource lifetime = new();
    private readonly FlowLayoutPanel rows = new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
    private readonly Panel viewport = new() { Dock = DockStyle.Top, AutoScroll = true };
    private readonly FlowLayoutPanel actions = new() { Dock = DockStyle.Bottom, Height = 96, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Label message = new() { AutoSize = true, MaximumSize = new(280, 0), ForeColor = UITheme.Neutral600, Margin = new(12) };
    private int sequence;
    private bool resourcesReleased;
    private int maximumHeight = 560;
    private int menuWidth = 304;
    public event Action<long>? SelectRequested;
    public event EventHandler? CreateRequested;
    public event EventHandler? ManagementRequested;
    public event EventHandler? ContentSizeChanged;
    public Size PopupContentSize => new(menuWidth, Math.Min(maximumHeight, rows.GetPreferredSize(new Size(menuWidth - 16, 0)).Height + actions.Height + Padding.Vertical));

    public ProjectSwitcherControl(ProjectApiFacade projects, IReadOnlyList<long> recent)
    {
        this.projects = projects; this.recent = recent;
        Width = menuWidth; Padding = new(8); BackColor = UITheme.White; Font = UITheme.FontBody;
        viewport.Controls.Add(rows); Controls.Add(viewport); Controls.Add(actions);
        actions.Controls.Add(new Panel { Width = menuWidth - 16, Height = 1, BackColor = UITheme.Neutral200, Margin = new(4, 4, 4, 8) });
        AddAction("Thêm Project", true).Click += (_, _) => CreateRequested?.Invoke(this, EventArgs.Empty);
        AddAction("Quản lý Project", false).Click += (_, _) => ManagementRequested?.Invoke(this, EventArgs.Empty);
    }
    public void ConstrainTo(Size available)
    {
        menuWidth = Math.Min(304, Math.Max(160, available.Width)); maximumHeight = Math.Max(160, available.Height);
        foreach (Control control in actions.Controls) control.Width = menuWidth - 16;
        UpdateContentSize();
    }
    private void UpdateContentSize()
    {
        message.MaximumSize = new(Math.Max(1, menuWidth - Padding.Horizontal - message.Margin.Horizontal), 0);
        int natural = rows.GetPreferredSize(new Size(menuWidth - 16, 0)).Height;
        viewport.Height = Math.Max(40, Math.Min(natural, maximumHeight - actions.Height - Padding.Vertical));
        foreach (Control control in rows.Controls) control.Width = menuWidth - 16 - (natural > viewport.Height ? SystemInformation.VerticalScrollBarWidth : 0);
        Size = PopupContentSize; ContentSizeChanged?.Invoke(this, EventArgs.Empty);
    }
    public async Task ReloadAsync()
    {
        int version = ++sequence; long generation = projects.Context.Generation;
        ClearRows(); message.Text = "Đang tải Project…"; rows.Controls.Add(message); UpdateContentSize();
        try
        {
            var result = await projects.ListAsync(lifetime.Token);
            if (IsDisposed || version != sequence || generation != projects.Context.Generation) return;
            ClearRows();
            var active = result.Items.Where(item => item.Status == "ACTIVE").ToList();
            var ordered = recent.Select(id => active.FirstOrDefault(item => item.ProjectId == id)).OfType<ProjectListItem>()
                .Concat(active.Where(item => !recent.Contains(item.ProjectId))).Take(5);
            foreach (var item in ordered)
            {
                var row = new ProjectListRowControl { Text = item.Name, Subtitle = item.CurrentUserRole, Compact = true, Height = 56,
                    Width = menuWidth - 16, IsCurrent = projects.Context.Current?.ProjectId == item.ProjectId, Tag = item.ProjectId,
                    AccessibleName = item.Name + (projects.Context.Current?.ProjectId == item.ProjectId ? ", đang mở" : "") };
                row.Click += (_, _) => SelectRequested?.Invoke(item.ProjectId); rows.Controls.Add(row);
            }
            if (active.Count == 0) { message.Text = "Chưa có Project đang hoạt động."; rows.Controls.Add(message); }
            if (rows.Controls.OfType<ProjectListRowControl>().LastOrDefault() is { } last) last.ShowDivider = false;
            UpdateContentSize();
        }
        catch (OperationCanceledException) when (resourcesReleased || IsDisposed || lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (IsDisposed || version != sequence || generation != projects.Context.Generation) return;
            ClearRows(); message.Text = error.Message; rows.Controls.Add(message);
            var retry = new ProjectActionButton { Text = "Thử lại", Style = RoundButtonStyle.Ghost, Width = menuWidth - 16, Height = 36 };
            retry.Click += async (_, _) => await ReloadAsync(); rows.Controls.Add(retry); UpdateContentSize();
        }
    }
    private Button AddAction(string text, bool plus)
    {
        var button = new Button { Text = text, TextAlign = ContentAlignment.MiddleLeft, Padding = new(40, 0, 12, 0),
            Width = menuWidth - 16, Height = 36, Margin = new(0, 0, 0, 4), Font = UITheme.FontBodyBold, Cursor = Cursors.Hand };
        UiTheme.StyleButton(button); button.BackColor = UITheme.White; button.ForeColor = UITheme.Ink;
        button.FlatAppearance.MouseOverBackColor = UITheme.Neutral50; button.FlatAppearance.MouseDownBackColor = UITheme.Neutral100;
        button.Paint += (_, e) =>
        {
            if (plus) UIIcons.Plus(e.Graphics, new Rectangle(12, 10, 16, 16), UITheme.Ink);
            else UIIcons.Gear(e.Graphics, new Rectangle(12, 10, 16, 16), UITheme.Neutral600);
        };
        actions.Controls.Add(button); return button;
    }
    private void ClearRows()
    {
        rows.Controls.Remove(message);
        foreach (Control control in rows.Controls.Cast<Control>().ToArray()) { rows.Controls.Remove(control); control.Dispose(); }
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is Keys.Down or Keys.Up) { SelectNextControl(ActiveControl, keyData == Keys.Down, true, true, true); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !resourcesReleased) { resourcesReleased = true; lifetime.Cancel(); lifetime.Dispose(); if (message.Parent is null) message.Dispose(); }
        base.Dispose(disposing);
    }
}
