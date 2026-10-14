using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Forms.Projects;
using CreatorFlow.Helpers;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

public sealed class ProjectPickerControl : UserControl
{
    private readonly ProjectApiFacade projects;
    private readonly CancellationTokenSource lifetime = new();
    private readonly TableLayoutPanel layout = UiTheme.CreateFormBody();
    private readonly TableLayoutPanel activeRows = UiTheme.CreateFormBody();
    private readonly TableLayoutPanel archivedRows = UiTheme.CreateFormBody();
    private readonly TableLayoutPanel activeSection = UiTheme.CreateFormBody();
    private readonly TableLayoutPanel archivedSection = UiTheme.CreateFormBody();
    private readonly Label message = new() { Text = "Đang tải Project…", AutoSize = true, ForeColor = UITheme.Neutral600 };
    private int sequence;
    private bool activationQueued;
    private bool released;
    public event Action<long>? SelectRequested;
    public event Action<ProjectResponse>? ProjectCreated;
    public void ShowSelectionError(string text) { message.Text = text; message.ForeColor = UITheme.Danger; message.Visible = true; }
    public ProjectPickerControl(ProjectApiFacade projects)
    {
        this.projects = projects; Dock = DockStyle.Fill; Padding = new(32); BackColor = UITheme.Neutral50; Font = UITheme.FontBody;
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        var header = new ProjectPageHeaderControl("Chọn Project", "Chọn Project để bắt đầu làm việc hoặc tạo Project đầu tiên của bạn.");
        header.CreateRequested += (_, _) =>
        {
            using var dialog = new CreateProjectDialog(projects);
            if (dialog.ShowProjectDialog(this) == DialogResult.OK && dialog.CreatedProject is { } result) ProjectCreated?.Invoke(result);
        };
        header.ReloadRequested += async (_, _) => await ReloadAsync();
        UiTheme.AddRow(layout, header, 24); UiTheme.AddRow(layout, message);
        layout.SizeChanged += (_, _) => message.MaximumSize = new(Math.Max(1, layout.ClientSize.Width), 0);
        foreach (var (title, rows, section) in new[] { ("Project đang hoạt động", activeRows, activeSection), ("Project đã lưu trữ", archivedRows, archivedSection) })
        {
            rows.ColumnStyles.Add(new(SizeType.Percent, 100)); rows.BackColor = UITheme.White;
            rows.Padding = new(16); UiTheme.StyleCard(rows);
            section.ColumnStyles.Add(new(SizeType.Percent, 100)); section.Visible = false;
            UiTheme.AddRow(section, new Label { Text = title, Font = UITheme.FontH2, AutoSize = true }, 8);
            UiTheme.AddRow(section, rows, 0);
            UiTheme.AddRow(layout, section, 24);
        }
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true }; scroll.Controls.Add(layout); Controls.Add(scroll);
    }
    public async Task ReloadAsync()
    {
        int version = ++sequence; long generation = projects.Context.Generation;
        message.Visible = true; message.ForeColor = UITheme.Neutral600;
        message.Text = activeRows.Controls.Count + archivedRows.Controls.Count > 0 ? "Đang làm mới danh sách Project…" : "Đang tải Project…";
        try
        {
            var result = await projects.ListAsync(lifetime.Token);
            if (IsDisposed || lifetime.IsCancellationRequested || version != sequence || generation != projects.Context.Generation) return;
            foreach (var rows in new[] { activeRows, archivedRows })
            {
                foreach (Control child in rows.Controls.Cast<Control>().ToArray()) { rows.Controls.Remove(child); child.Dispose(); }
                rows.RowStyles.Clear(); rows.RowCount = 0;
            }
            foreach (var item in result.Items)
            {
                var row = new ProjectListRowControl { Text = item.Name, Subtitle = item.CurrentUserRole,
                    Badge = item.Status == "ACTIVE" ? "Đang hoạt động" : "Đã lưu trữ", SelectableProject = item.Status == "ACTIVE",
                    TabStop = item.Status == "ACTIVE", AccessibleName = item.Name, AccessibleDescription = item.Status, Tag = item.ProjectId };
                if (!row.SelectableProject) row.Cursor = Cursors.Default;
                row.Click += (_, _) => QueueSelection(item.ProjectId);
                UiTheme.AddRow(item.Status == "ACTIVE" ? activeRows : archivedRows, row, 0);
            }
            foreach (var rows in new[] { activeRows, archivedRows })
            {
                if (rows.Controls.OfType<ProjectListRowControl>().LastOrDefault() is { } last) last.ShowDivider = false;
            }
            activeSection.Visible = activeRows.Controls.Count > 0;
            archivedSection.Visible = archivedRows.Controls.Count > 0;
            message.Text = archivedRows.Controls.Count == 0
                ? "Bạn chưa có Project. Chọn Thêm Project để bắt đầu."
                : "Bạn chưa có Project đang hoạt động. Chọn Thêm Project để bắt đầu.";
            message.Visible = activeRows.Controls.Count == 0;
        }
        catch (OperationCanceledException) when (IsDisposed || lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (!IsDisposed && version == sequence && generation == projects.Context.Generation)
            {
                bool hasProjects = activeRows.Controls.Count + archivedRows.Controls.Count > 0;
                message.Text = "Không thể tải danh sách Project. " + error.Message +
                    (hasProjects ? " Danh sách hiển thị là lần tải thành công trước." : "") + " Chọn Làm mới để thử lại.";
                message.ForeColor = UITheme.Danger; message.Visible = true;
            }
        }
    }
    private void QueueSelection(long id)
    {
        if (activationQueued || !IsHandleCreated) return;
        int version = sequence; long generation = projects.Context.Generation; activationQueued = true;
        BeginInvoke(new Action(() =>
        {
            activationQueued = false;
            if (!IsDisposed && !lifetime.IsCancellationRequested && version == sequence && generation == projects.Context.Generation) SelectRequested?.Invoke(id);
        }));
    }
    protected override void Dispose(bool disposing)
    { if (disposing && !released) { released = true; lifetime.Cancel(); lifetime.Dispose(); } base.Dispose(disposing); }
}
