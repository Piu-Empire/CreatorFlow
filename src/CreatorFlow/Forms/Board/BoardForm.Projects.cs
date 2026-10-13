using CreatorFlow.ApiClients;
using CreatorFlow.Controls;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

public partial class BoardForm
{
    private ProjectApiFacade? _projects;
    private readonly List<long> _recentProjects = new();
    private ToolStripDropDown? _projectMenu;
    private Control? _projectPage;
    private CancellationTokenSource? _projectSwitch;
    private readonly System.Windows.Forms.Timer _projectTimer = new() { Interval = 60000 };
    private bool _validatingProject;
    private int _switchSequence;

    private void InitializeProjects(ProjectApiFacade? projects)
    {
        _projects = projects;
        if (projects is null) return;
        if (projects.Context.Current is { } opened) _recentProjects.Add(opened.ProjectId);
        projects.Context.Changed += ProjectChanged;
        _sidebarControl.ProjectManagementEnabled = true;
        _sidebarControl.ProjectSelectionRequested += async (_, _) => await ShowProjectMenuAsync();
        _sidebarControl.ProjectSettingsRequested += async (_, _) =>
        {
            await ShowProjectSettingsAsync();
        };
        Shown += async (_, _) => { if (projects.Context.Current is null) await ShowProjectSettingsAsync(); else RenderServerWorkspace(); };
        Activated += async (_, _) => { await ValidateProjectAsync(); };
        _projectTimer.Tick += async (_, _) => { await ValidateProjectAsync(); };
        _projectTimer.Start();
        FormClosed += (_, _) =>
        {
            projects.Context.Changed -= ProjectChanged;
            CloseProjectMenu(); _recentProjects.Clear();
            _projectTimer.Stop(); _projectTimer.Dispose();
            _projectSwitch?.Cancel(); _projectSwitch?.Dispose();
            CloseProjectPage();
        };
    }
    private bool CanLeaveProjectPage()
    {
        if (_profile?.HasUnsavedChanges == true && MessageBox.Show(this, "Bỏ thay đổi hồ sơ chưa lưu?", "Thay đổi chưa lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
        return !_contentDetailPanel.HasUnsavedChanges || _contentDetailPanel.Clear();
    }
    private void CloseProjectPage()
    {
        if (_projectPage is not { } page) return;
        _projectPage = null; _pnlMain.Controls.Remove(page); page.Dispose();
    }
    private void ShowProjectPage(Control page, string title)
    {
        _projectSwitch?.Cancel(); ++_switchSequence;
        CloseProjectPage(); CloseProfile(); CloseAdmin(); CloseReports(); CloseCalendar(); _projectPage = page;
        _pnlBoardArea.Visible = false; _pnlDrawerHost.Visible = false;
        _pnlMain.Controls.Add(page); page.BringToFront();
        _topHeaderControl.SetAuthenticatedPage(title);
    }
    private void CloseProjectMenu()
    {
        var menu = _projectMenu; _projectMenu = null;
        if (menu is null) return;
        menu.Close(); menu.Dispose();
    }
    private async Task ShowProjectMenuAsync()
    {
        if (_projects is null || _auth?.Session.IsAuthenticated != true || IsDisposed) return;
        if (_projectMenu is not null) { CloseProjectMenu(); return; }
        var switcher = new ProjectSwitcherControl(_projects, _recentProjects.ToArray());
        var anchor = _sidebarControl.ProjectMenuScreenLocation;
        var area = Screen.FromPoint(anchor).WorkingArea;
        var available = new Size(Math.Min(_sidebarControl.ProjectMenuAnchorWidth, area.Width - 32), Math.Max(160, area.Bottom - anchor.Y - 12));
        switcher.ConstrainTo(new Size(available.Width - 8, available.Height));
        var host = new ToolStripControlHost(switcher) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = switcher.PopupContentSize };
        var menu = new ToolStripDropDown { Padding = new(4), BackColor = UITheme.White };
        menu.SizeChanged += (_, _) =>
        {
            using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, menu.Width, menu.Height), 8);
            var old = menu.Region; menu.Region = new Region(path); old?.Dispose();
        };
        menu.Paint += (_, e) =>
        {
            using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, menu.Width - 1, menu.Height - 1), 8);
            using var pen = new Pen(UITheme.Neutral200); e.Graphics.DrawPath(pen, path);
        };
        menu.Items.Add(host); _projectMenu = menu;
        switcher.ContentSizeChanged += (_, _) => { if (!menu.IsDisposed) host.Size = switcher.PopupContentSize; };
        switcher.SelectRequested += async id => { menu.Close(); await SelectProjectAsync(id); };
        switcher.CreateRequested += async (_, _) => { menu.Close(); await CreateProjectAsync(); };
        switcher.ManagementRequested += async (_, _) => { menu.Close(); await ShowProjectSettingsAsync(); };
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_projectMenu, menu)) _projectMenu = null;
            // Dispose after the click event unwinds so its handler can complete.
            if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => menu.Dispose()));
            else menu.Dispose();
        };
        _sidebarControl.ShowProjectMenu(menu);
        switcher.ConstrainTo(new Size(available.Width - (menu.Width - host.Width), available.Height));
        await switcher.ReloadAsync();
        if (!menu.IsDisposed && menu.Visible) switcher.SelectNextControl(null, true, true, true, false);
    }
    private async Task CreateProjectAsync()
    {
        if (_projects is null || _auth?.Session.IsAuthenticated != true) return;
        long generation = _projects.Context.Generation, authGeneration = _auth.Session.Generation;
        string profileDraft = DraftSnapshot(_profile), contentDraft = DraftSnapshot(_contentDetailPanel);
        bool dirty = _profile?.HasUnsavedChanges == true || _contentDetailPanel.HasUnsavedChanges;
        using var dialog = new CreatorFlow.Forms.Projects.CreateProjectDialog(_projects, dirty);
        if (dialog.ShowProjectDialog(this) == DialogResult.OK && dialog.CreatedProject is { } created && !IsDisposed &&
            generation == _projects.Context.Generation && authGeneration == _auth.Session.Generation &&
            profileDraft == DraftSnapshot(_profile) && contentDraft == DraftSnapshot(_contentDetailPanel))
            await SelectProjectCoreAsync(created.ProjectId, false);
    }
    private async Task ShowProjectSettingsAsync()
    {
        if (_projects is null || IsDisposed || !CanLeaveProjectPage()) return;
        var settings = new ProjectSettingsControl(_projects, _invitationService is not null);
        settings.SelectRequested += async id => await SelectProjectAsync(id);
        settings.InvitationRequested += (_, _) => OpenProjectSettings();
        settings.CreateRequested += async (_, _) => await CreateProjectAsync();
        settings.ProjectUnavailable += target =>
        {
            if (!ReferenceEquals(_projectPage, settings)) return;
            if (_projects.Context.Current?.ProjectId == target)
            { _recentProjects.Remove(target); _projects.Context.Clear(); }
        };
        ShowProjectPage(settings, "Quản lý Project");
        await settings.LoadProjectAsync();
    }
    private bool _selectingProject;
    private Task SelectProjectAsync(long id) => SelectProjectCoreAsync(id, true);
    private static string DraftSnapshot(Control? root)
    {
        if (root is null) return string.Empty;
        var values = new List<string>();
        void Read(Control control)
        {
            if (control is TextBoxBase or ComboBox) values.Add(control.Name + ":" + control.Text);
            if (control is CheckBox check) values.Add(control.Name + ":" + check.Checked);
            if (control is DateTimePicker date) values.Add(control.Name + ":" + date.Checked + ":" + date.Value.Ticks);
            foreach (Control child in control.Controls) Read(child);
        }
        Read(root); return string.Join("\u001f", values);
    }
    private async Task SelectProjectCoreAsync(long id, bool confirmSelection)
    {
        if (_projects is null || _auth is null || _selectingProject || _projects.Context.Current?.ProjectId == id) return;
        _selectingProject = true;
        _projectSwitch?.Cancel(); _projectSwitch?.Dispose();
        _projectSwitch = CancellationTokenSource.CreateLinkedTokenSource(_authLifetime.Token);
        var token = _projectSwitch.Token; int sequence = ++_switchSequence;
        long contextGeneration = _projects.Context.Generation; long authGeneration = _auth.Session.Generation;
        bool Valid() => !IsDisposed && !token.IsCancellationRequested && _auth.Session.IsAuthenticated && sequence == _switchSequence &&
            contextGeneration == _projects.Context.Generation && authGeneration == _auth.Session.Generation;
        try
        {
            string profileDraft = DraftSnapshot(_profile), contentDraft = DraftSnapshot(_contentDetailPanel);
            var access = await _projects.AccessAsync(id, token);
            if (!Valid()) return;
            bool dirty = _profile?.HasUnsavedChanges == true || _contentDetailPanel.HasUnsavedChanges;
            if (profileDraft != DraftSnapshot(_profile) || contentDraft != DraftSnapshot(_contentDetailPanel))
            { _toast.ShowToast(this, "Bản nháp đã thay đổi. Vui lòng chọn lại Project."); return; }
            if (confirmSelection)
            {
                using var confirm = new CreatorFlow.Forms.Projects.ProjectSwitchDialog(access.Name, _projects.Context.Current is null, dirty);
                if (confirm.ShowProjectDialog(this) != DialogResult.OK || !Valid()) return;
                var latest = await _projects.AccessAsync(id, token);
                if (!Valid()) return;
                if (latest.Name != access.Name || profileDraft != DraftSnapshot(_profile) || contentDraft != DraftSnapshot(_contentDetailPanel))
                { _toast.ShowToast(this, "Thông tin Project hoặc bản nháp đã thay đổi. Vui lòng chọn lại."); return; }
                access = latest;
            }
            if (!Valid()) return;
            // Consent and access are verified before discarding drafts or changing navigation.
            _projects.Context.Apply(access); ResetProjectState();
            _recentProjects.Remove(id); _recentProjects.Insert(0, id);
            if (_recentProjects.Count > 5) _recentProjects.RemoveRange(5, _recentProjects.Count - 5);
            CloseProjectPage(); CloseProfile(); CloseAdmin(); CloseReports(); CloseCalendar(); ReloadBoard();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || IsDisposed || authGeneration != _auth.Session.Generation) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (Valid())
            {
                using var notice = new CreatorFlow.Forms.Projects.ProjectModalForm("Không thể chọn Project", error.Message, "Đóng", 96, true);
                notice.ShowProjectDialog(this);
            }
        }
        finally { _selectingProject = false; }
    }
    private void ResetProjectState()
    {
        _allCards.Clear(); _selectedContentId = null; _selectedCardControl = null;
        _modulePageHeader.ResetFilters();
        _sidebarControl.BacklogCount = _sidebarControl.MyWorkCount = _sidebarControl.ReviewQueueCount = 0;
        _contentDetailPanel.ResetProject(); _pnlDrawerHost.Visible = false;
        CloseCalendar();
        foreach (var column in _columns.Values) column.ClearCards();
    }
    private async void ProjectChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || _projects is null) return;
        CloseProjectMenu();
        UpdateProjectContext();
        if (_projects.Context.Current is not null)
        {
            if (_projectPage is null && _profile is null && _admin is null && _reports is null && _calendar is null) RenderServerWorkspace();
            return;
        }
        if (_auth?.Session.IsAuthenticated != true) _recentProjects.Clear();
        _projectSwitch?.Cancel(); ResetProjectState();
        foreach (Form child in OwnedForms) child.Close();
        CloseProjectPage(); CloseProfile();
        if (_auth?.Session.IsAuthenticated == true) await ShowProjectSettingsAsync();
    }
    private async Task<bool> ValidateProjectAsync()
    {
        if (_projects?.Context.Current is not { } current || _validatingProject || IsDisposed || _authLifetime.IsCancellationRequested) return false;
        long generation = _projects.Context.Generation; long? authGeneration = _auth?.Session.Generation;
        int sequence = _switchSequence;
        _validatingProject = true;
        try
        {
            bool valid = await _projects.RevalidateAsync(_authLifetime.Token);
            if (!valid && !IsDisposed)
                _toast.ShowToast(this, "Project không còn khả dụng. Vui lòng chọn lại.");
            return valid && !IsDisposed && sequence == _switchSequence && authGeneration == _auth?.Session.Generation &&
                _projects.Context.Current?.ProjectId == current.ProjectId;
        }
        catch (OperationCanceledException) when (_authLifetime.IsCancellationRequested || IsDisposed || _auth?.Session.IsAuthenticated != true) { }
        catch (ApiBusinessException error) when (error.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            if (!IsDisposed && generation == _projects.Context.Generation && authGeneration == _auth?.Session.Generation)
                _projects.Context.Clear();
        }
        catch (Exception error) when (error is ApiException or ApiBusinessException)
        {
            if (!IsDisposed) _toast.ShowToast(this, error.Message);
        }
        finally { _validatingProject = false; }
        return false;
    }
    private void RenderServerWorkspace()
    {
        _allCards.Clear(); _selectedContentId = null; _selectedCardControl = null;
        _sidebarControl.BacklogCount = _sidebarControl.MyWorkCount = _sidebarControl.ReviewQueueCount = 0;
        _topHeaderControl.ClearStats();
        _pnlDrawerHost.Visible = false;
        bool current = _projects?.Context.Current is not null;
        _modulePageHeader.Visible = current; _flowColumns.Visible = current;
        _modulePageHeader.SetContentCreationEnabled(false);
        foreach (var column in _columns.Values) column.SourceAvailable = false;
        ApplyFilters();
        if (_projectPage is not null || _profile is not null || _admin is not null || _reports is not null || _calendar is not null) return;
        _pnlBoardArea.Visible = true;
        if (_pnlBoardArea.Controls["serverProjectState"] is not Label state)
        {
            state = new Label { Name = "serverProjectState", Dock = DockStyle.Top, Height = 64,
                Padding = new Padding(0, 8, 32, 8), TextAlign = ContentAlignment.MiddleLeft,
                Font = UITheme.FontBody, ForeColor = UITheme.Neutral600, BackColor = UITheme.Neutral50 };
            _pnlBoardArea.Controls.Add(state);
            _pnlBoardArea.Controls.SetChildIndex(state, 1);
        }
        state.Text = current
            ? "Board chưa kết nối nguồn nội dung cho Project này. Bạn vẫn có thể mở My Tasks và Lời mời."
            : "Chưa chọn Project. Hãy tạo hoặc chọn Project để bắt đầu.";
        _topHeaderControl.SetAuthenticatedPage("Board");
    }
    private async void ShowUnavailableModule(string module)
    {
        if (_projects?.Context.Current is null || !CanLeaveProjectPage()) return;
        if (!await ValidateProjectAsync() || IsDisposed || _projects.Context.Current is null) return;
        CloseProjectPage(); CloseProfile(); RenderServerWorkspace();
        _topHeaderControl.SetAuthenticatedPage(module);
        if (_pnlBoardArea.Controls["serverProjectState"] is Label state)
            state.Text = $"{module} chưa được kết nối nguồn dữ liệu cho Project hiện tại.";
    }
}
