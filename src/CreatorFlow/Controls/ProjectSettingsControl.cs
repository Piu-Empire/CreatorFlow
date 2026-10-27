using System.Net;
using CreatorFlow.ApiClients;
using CreatorFlow.Helpers;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Forms.Projects;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

public sealed class ProjectSettingsControl : UserControl
{
    private readonly ProjectApiFacade projects;
    private readonly CancellationTokenSource lifetime = new();
    private readonly TableLayoutPanel layout = UiTheme.CreateFormBody();
    private readonly TableLayoutPanel currentRows = UiTheme.CreateFormBody();
    private readonly TableLayoutPanel activeRows = UiTheme.CreateFormBody();
    private readonly TableLayoutPanel archivedRows = UiTheme.CreateFormBody();
    private readonly Label message = new() { AutoSize = true, ForeColor = UITheme.Neutral600 };
    private readonly Button archive = new() { Text = "Lưu trữ Project", Width = 150 };
    private readonly Button delete = new() { Text = "Xóa Project", Width = 150 };
    private readonly FlowLayoutPanel danger = new() { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
    private Label? memberEntry;
    private Label? invitationEntry;
    private readonly Label targetName = new() { AutoSize = true, Font = UITheme.FontBodyBold };
    private ProjectResponse? loadedCurrent;
    private bool busy;
    private bool released;
    private int sequence;
    private long? uncertainTarget;
    private bool uncertainRestore;
    public event Action<long>? SelectRequested;
    public event EventHandler? MembersRequested;
    public event EventHandler? InvitationRequested;
    public event EventHandler? CreateRequested;

    public event Action<long>? ProjectUnavailable;

    public ProjectSettingsControl(ProjectApiFacade projects, bool invitationsAvailable = false)
    {
        this.projects = projects;
        Dock = DockStyle.Fill; Font = UITheme.FontBody; BackColor = UITheme.Neutral50;
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        var header = new ProjectPageHeaderControl("Quản lý Project", "Xem các Project của bạn và chọn Project để làm việc.");
        header.CreateRequested += (_, _) => CreateRequested?.Invoke(this, EventArgs.Empty);
        header.ReloadRequested += async (_, _) => { if (!busy) await LoadProjectAsync(); };
        UiTheme.AddRow(layout, header, 24); UiTheme.AddRow(layout, message);
        message.Visible = false;
        layout.SizeChanged += (_, _) => message.MaximumSize = new(Math.Max(1, layout.ClientSize.Width), 0);
        archivedRows.ColumnCount = 2;
        archivedRows.ColumnStyles.Clear();
        archivedRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        archivedRows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        AddSection("Project hiện tại", currentRows); AddSection("Project đang hoạt động", activeRows); AddSection("Project đã lưu trữ", archivedRows);
        danger.Padding = new(16); danger.BackColor = UITheme.DangerBg; UiTheme.StyleCard(danger);
        danger.Controls.AddRange([new Label { Text = "Khu vực nguy hiểm", AutoSize = true, ForeColor = UITheme.Danger, Font = UITheme.FontBodyBold }, targetName,
            new Label { Text = "Lưu trữ: ngừng làm việc trong Project này nhưng vẫn giữ dữ liệu.", AutoSize = true }, archive,
            new Label { Text = "Xóa vĩnh viễn Project và dữ liệu liên quan. Không thể hoàn tác.", AutoSize = true, ForeColor = UITheme.Danger }, delete]);
        foreach (var button in new[] { archive, delete }) { button.Height = 40; UiTheme.StyleButton(button); }
        delete.BackColor = UITheme.Danger; delete.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
        UiTheme.AddRow(layout, danger, 0);
        danger.SizeChanged += (_, _) => { foreach (var label in danger.Controls.OfType<Label>()) label.MaximumSize = new(Math.Max(100, danger.ClientSize.Width - 32), 0); };
        var menu = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 208, Padding = new(16, 32, 8, 32), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        foreach (string title in new[] { "Quản lý Project", "Thành viên", "Lời mời", "Vai trò & quyền", "Nền tảng", "Tags" })
        {
            var item = new Label { Text = title, Width = 184, Height = 36, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new(12, 0, 8, 0), Font = title == "Quản lý Project" ? UITheme.FontBodyBold : UITheme.FontBody,
                BackColor = title == "Quản lý Project" ? UITheme.Neutral100 : UITheme.Neutral50,
                ForeColor = title == "Quản lý Project" ? UITheme.Ink : UITheme.Neutral400 };
            if (title == "Thành viên")
            {
                memberEntry = item; item.Cursor = Cursors.Hand;
                item.Click += (_, _) => MembersRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (title == "Lời mời" && invitationsAvailable)
            {
                invitationEntry = item; item.ForeColor = UITheme.Ink; item.Cursor = Cursors.Hand;
                item.Click += (_, _) => InvitationRequested?.Invoke(this, EventArgs.Empty);
            }
            menu.Controls.Add(item);
            if (title != "Quản lý Project" && title != "Thành viên" && !(title == "Lời mời" && invitationsAvailable)) menu.Controls.Add(new Label { Text = "Chưa khả dụng", Width = 184, Height = 24, Padding = new(12, 0, 0, 0), ForeColor = UITheme.Neutral400, Font = UITheme.FontLabel });
        }
        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new(32) }; body.Controls.Add(layout);
        Controls.Add(body); Controls.Add(menu);
        archive.Click += async (_, _) => await MutateAsync(false); delete.Click += async (_, _) => await MutateAsync(true);
        projects.Context.Changed += ContextChanged; SetBusy(true);
    }
    private void AddSection(string title, TableLayoutPanel rows)
    {
        if (rows.ColumnStyles.Count == 0) rows.ColumnStyles.Add(new(SizeType.Percent, 100));
        rows.BackColor = UITheme.White; rows.Padding = new(16); UiTheme.StyleCard(rows);
        UiTheme.AddRow(layout, new Label { Text = title, Font = UITheme.FontH2, AutoSize = true }, 8);
        UiTheme.AddRow(layout, rows, 24);
    }
    private async void ContextChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        loadedCurrent = null; SetBusy(true); await LoadProjectAsync();
    }
    private static void ClearRows(TableLayoutPanel rows)
    {
        foreach (Control control in rows.Controls.Cast<Control>().ToArray()) { rows.Controls.Remove(control); control.Dispose(); }
        rows.RowStyles.Clear(); rows.RowCount = 0;
    }
    private ProjectListRowControl AddProject(TableLayoutPanel rows, ProjectListItem item, bool current)
    {
        var row = new ProjectListRowControl { Text = item.Name, Subtitle = item.CurrentUserRole, IsCurrent = current,
            Badge = current ? "Đang mở" : item.Status == "ACTIVE" ? "Đang hoạt động" : "Đã lưu trữ",
            SelectableProject = item.Status == "ACTIVE", TabStop = item.Status == "ACTIVE", Tag = item.ProjectId,
            AccessibleName = item.Name + (current ? ", Project hiện tại" : ""), AccessibleDescription = item.Status };
        if (!row.SelectableProject) row.Cursor = Cursors.Default;
        row.Click += (_, _) => SelectRequested?.Invoke(item.ProjectId);
        int rowIndex = rows.RowCount;
        UiTheme.AddRow(rows, row, 0);
        if (ReferenceEquals(rows, archivedRows) && item.CurrentUserRole == "OWNER")
        {
            var restore = new ProjectActionButton
            {
                Name = $"restoreProject_{item.ProjectId}",
                Text = "Khôi phục",
                Width = 112,
                Height = 36,
                Anchor = AnchorStyles.None,
                Margin = Padding.Empty
            };
            restore.Click += async (_, _) =>
            {
                restore.Enabled = false;
                try { await RestoreArchivedProjectAsync(item); }
                finally { if (!restore.IsDisposed) restore.Enabled = true; }
            };
            rows.Controls.Add(restore, 1, rowIndex);
        }
        return row;
    }
    private void SetBusy(bool value)
    {
        busy = value; message.Visible = !string.IsNullOrEmpty(message.Text);
        bool owner = loadedCurrent is { CurrentUserRole: "OWNER", Status: "ACTIVE" } &&
            projects.Context.Current is { Role: CreatorFlow.Models.Enums.ProjectRole.Owner } current && current.ProjectId == loadedCurrent.ProjectId;
        if (memberEntry is not null)
        {
            memberEntry.Enabled = owner && !value && uncertainTarget is null;
            memberEntry.ForeColor = memberEntry.Enabled ? UITheme.Ink : UITheme.Neutral400;
            memberEntry.Text = owner ? "Thành viên" : "Thành viên · Chỉ Owner";
        }
        if (invitationEntry is not null)
        {
            invitationEntry.Enabled = owner && !value && uncertainTarget is null;
            invitationEntry.ForeColor = invitationEntry.Enabled ? UITheme.Ink : UITheme.Neutral400;
            invitationEntry.Text = owner ? "Lời mời" : "Lời mời · Chỉ Owner";
        }
        danger.Visible = owner; archive.Enabled = delete.Enabled = owner && !value && uncertainTarget is null;
        targetName.Text = loadedCurrent is null ? "" : "Project hiện tại: " + loadedCurrent.Name;
    }
    public async Task LoadProjectAsync()
    {
        int version = ++sequence; long generation = projects.Context.Generation; var current = projects.Context.Current;
        bool Valid() => !IsDisposed && !lifetime.IsCancellationRequested && version == sequence && generation == projects.Context.Generation;
        loadedCurrent = null; SetBusy(true); message.Text = "Đang tải Project…"; message.Visible = true;
        try
        {
            var result = await projects.ListAsync(lifetime.Token);
            if (!Valid()) return;
            foreach (var rows in new[] { currentRows, activeRows, archivedRows }) ClearRows(rows);
            loadedCurrent = null;
            var details = new List<(ProjectListItem Item, ProjectListRowControl Label)>();
            foreach (var item in result.Items.OrderBy(item => item.ProjectId == current?.ProjectId ? 0 : 1))
            {
                bool isCurrent = item.ProjectId == current?.ProjectId && item.Status == "ACTIVE";
                var rows = isCurrent ? currentRows : item.Status == "ACTIVE" ? activeRows : archivedRows;
                details.Add((item, AddProject(rows, item, isCurrent)));
            }
            foreach (var rows in new[] { currentRows, activeRows, archivedRows })
            {
                if (rows.Controls.OfType<ProjectListRowControl>().LastOrDefault() is { } last) last.ShowDivider = false;
                if (rows.Controls.Count == 0) UiTheme.AddRow(rows, new Label { Text = rows == currentRows ? "Chưa chọn Project." : "Không có Project trong nhóm này.", AutoSize = true, ForeColor = UITheme.Neutral600 });
            }
            message.Text = string.Empty; message.Visible = false;
            foreach (var (item, label) in details)
            {
                try
                {
                    var detail = await projects.GetAsync(item.ProjectId, lifetime.Token);
                    if (!Valid()) return;
                    label.Subtitle = string.IsNullOrWhiteSpace(detail.Description) ? item.CurrentUserRole : item.CurrentUserRole + " · " + detail.Description;
                    if (detail.ProjectId == current?.ProjectId) loadedCurrent = detail;
                    if (detail.ProjectId == current?.ProjectId && detail.Status != "ACTIVE")
                    { ProjectUnavailable?.Invoke(detail.ProjectId); return; }
                }
                catch (Exception error) when (error is ApiException or ApiBusinessException)
                {
                    if (!Valid()) return;
                    label.Subtitle = item.CurrentUserRole + " · Chưa tải được chi tiết. Chọn Làm mới.";
                    if (error is ApiBusinessException invalid && item.ProjectId == current?.ProjectId && invalid.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
                    { ProjectUnavailable?.Invoke(item.ProjectId); return; }
                }
            }
            if (!Valid()) return;
            if (current is not null && !result.Items.Any(item => item.ProjectId == current.ProjectId))
            {
                try { await projects.AccessAsync(current.ProjectId, lifetime.Token); }
                catch (ApiBusinessException invalid) when (invalid.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
                { if (Valid()) ProjectUnavailable?.Invoke(current.ProjectId); return; }
                if (!Valid()) return;
            }
            if (uncertainTarget is { } target)
            {
                var actual = result.Items.FirstOrDefault(item => item.ProjectId == target);
                if (uncertainRestore)
                {
                    message.Text = actual is null
                        ? "Project không còn trong danh sách có quyền truy cập."
                        : $"Đã đối chiếu trạng thái khôi phục: {actual.Name} · {actual.Status}.";
                    if (actual is null || actual.Status is "ACTIVE" or "ARCHIVED")
                    {
                        uncertainTarget = null;
                        uncertainRestore = false;
                    }
                }
                else
                {
                    message.Text = actual is null ? "Project không còn trong danh sách có quyền truy cập." : $"Trạng thái đã đối chiếu: {actual.Name} · {actual.Status}.";
                    if (actual is null || actual.Status == "ARCHIVED") { uncertainTarget = null; if (current?.ProjectId == target) ProjectUnavailable?.Invoke(target); }
                    else if (loadedCurrent?.ProjectId == target && loadedCurrent.Status == "ACTIVE")
                    { uncertainTarget = null; message.Text += " Nếu cần thao tác mới, hãy xác nhận lại."; }
                }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested || IsDisposed) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException) { if (Valid()) message.Text = error.Message + " Chọn Làm mới / Thử lại để đọc trạng thái; không tự gửi lại thao tác."; }
        finally { if (Valid()) SetBusy(false); }
    }
    private async Task MutateAsync(bool deleting)
    {
        var current = projects.Context.Current;
        if (busy || uncertainTarget is not null || current is null || loadedCurrent is null || loadedCurrent.ProjectId != current.ProjectId || loadedCurrent.CurrentUserRole != "OWNER" || current.Role != CreatorFlow.Models.Enums.ProjectRole.Owner) return;
        long generation = projects.Context.Generation; int version = ++sequence; bool sent = false;
        bool Valid() => !IsDisposed && !lifetime.IsCancellationRequested && version == sequence && generation == projects.Context.Generation && projects.Context.Current?.ProjectId == current.ProjectId;
        SetBusy(true);
        try
        {
            var latest = await projects.GetAsync(current.ProjectId, lifetime.Token);
            if (!Valid()) return;
            loadedCurrent = latest;
            if (latest.CurrentUserRole != "OWNER" || latest.Status != "ACTIVE") { message.Text = "Bạn không còn quyền thao tác trên Project này."; return; }
            if (deleting)
            {
                using var confirm = new DeleteProjectDialog(latest.Name);
                if (confirm.ShowProjectDialog(this) != DialogResult.OK || !Valid()) return;
                sent = true; await projects.DeleteAsync(current.ProjectId, lifetime.Token);
            }
            else
            {
                using var confirm = new ArchiveProjectDialog(latest.Name);
                if (confirm.ShowProjectDialog(this) != DialogResult.OK || !Valid()) return;
                sent = true; await projects.ArchiveAsync(current.ProjectId, lifetime.Token);
            }
            if (Valid()) ProjectUnavailable?.Invoke(current.ProjectId);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested || IsDisposed) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (!Valid()) return;
            message.Text = error.Message;
            if (sent && (error is ApiException || error is ApiBusinessException unavailable && (int)unavailable.StatusCode >= 500))
            { uncertainTarget = current.ProjectId; uncertainRestore = false; message.Text = "Chưa xác định kết quả. Đang đọc lại trạng thái; không gửi lại thao tác."; await LoadProjectAsync(); }
            else if (error is ApiBusinessException invalid && invalid.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict) ProjectUnavailable?.Invoke(current.ProjectId);
            else if (error is ApiBusinessException denied && denied.StatusCode == HttpStatusCode.Forbidden)
            { loadedCurrent = null; await LoadProjectAsync(); }
        }
        finally { if (Valid()) SetBusy(false); }
    }

    private async Task RestoreArchivedProjectAsync(ProjectListItem item)
    {
        if (busy || uncertainTarget is not null || IsDisposed || lifetime.IsCancellationRequested || item.CurrentUserRole != "OWNER") return;
        using (var confirm = new RestoreProjectDialog(item.Name))
        {
            if (confirm.ShowProjectDialog(this) != DialogResult.OK || IsDisposed || lifetime.IsCancellationRequested) return;
        }

        var current = projects.Context.Current;
        long generation = projects.Context.Generation;
        int version = ++sequence;
        bool sent = false;
        bool Valid() => !IsDisposed && !lifetime.IsCancellationRequested && version == sequence &&
            generation == projects.Context.Generation && projects.Context.Current?.ProjectId == current?.ProjectId;
        SetBusy(true);
        try
        {
            var latest = await projects.GetAsync(item.ProjectId, lifetime.Token);
            if (!Valid()) return;
            if (latest.CurrentUserRole != "OWNER" || latest.Status != "ARCHIVED")
            {
                message.Text = "Project đã thay đổi trạng thái hoặc bạn không còn quyền khôi phục.";
                await LoadProjectAsync();
                return;
            }

            sent = true;
            await projects.RestoreAsync(item.ProjectId, lifetime.Token);
            if (Valid()) await LoadProjectAsync();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested || IsDisposed) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (!Valid()) return;
            message.Text = error.Message;
            if (sent && (error is ApiException || error is ApiBusinessException unavailable && (int)unavailable.StatusCode >= 500))
            {
                uncertainTarget = item.ProjectId;
                uncertainRestore = true;
                message.Text = "Chưa xác định kết quả khôi phục. Đang đọc lại trạng thái; không gửi lại thao tác.";
                await LoadProjectAsync();
            }
            else if (error is ApiBusinessException denied && denied.StatusCode == HttpStatusCode.Forbidden)
            {
                await LoadProjectAsync();
            }
            else if (error is ApiBusinessException invalid && invalid.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            {
                await LoadProjectAsync();
            }
        }
        finally { if (Valid()) SetBusy(false); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !released) { released = true; projects.Context.Changed -= ContextChanged; lifetime.Cancel(); lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}
