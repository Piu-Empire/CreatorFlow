using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Windows.Forms;
using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Board;
using CreatorFlow.Contracts.ContentDetail;
using CreatorFlow.Contracts.Projects;
using CreatorFlow.Contracts.Users;
using CreatorFlow.Contracts.Tasks;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Controls;
using CreatorFlow.Controls.ContentLibrary;
using CreatorFlow.Controls.MyTasks;
using CreatorFlow.Forms.Board;
using CreatorFlow.Forms.Projects;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Backends;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[DoNotParallelize]
[TestCategory("ProjectShell")]
public sealed class ProjectShellTests
{
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 0)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    public void Picker_ShowsOnlyGroupsWithProjects_AndOneEmptyHint(int activeCount, int archivedCount)
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); using var picker = new ProjectPickerControl(fixture.Projects);
            var items = new List<ProjectListItem>();
            if (activeCount > 0) items.Add(new(81, "A", "OWNER", "ACTIVE"));
            if (archivedCount > 0) items.Add(new(82, "Archived B", "OWNER", "ARCHIVED"));
            fixture.ListResponse = () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(new ProjectListResponse(items.ToArray())) });
            picker.CreateControl(); Complete(picker.ReloadAsync());
            Assert.AreEqual(activeCount > 0, Descendants(picker).OfType<Label>().Single(label => label.Text == "Project đang hoạt động").Visible);
            Assert.AreEqual(archivedCount > 0, Descendants(picker).OfType<Label>().Single(label => label.Text == "Project đã lưu trữ").Visible);
            Assert.AreEqual(activeCount + archivedCount, Descendants(picker).OfType<ProjectListRowControl>().Count());
            var message = Field<Label>(picker, "message");
            Assert.AreEqual(activeCount == 0, message.Visible);
            if (activeCount == 0) StringAssert.Contains(message.Text, "Chọn Thêm Project để bắt đầu.");
            CapturePicker(picker, $"picker-active{activeCount}-archived{archivedCount}");
        });
    }
    [TestMethod]
    public void Picker_FirstLoadPendingAndFailure_HasNoEmptyCards_RetryShowsProjects()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); using var picker = new ProjectPickerControl(fixture.Projects);
            var pending = new TaskCompletionSource<HttpResponseMessage>(); fixture.ListResponse = () => pending.Task;
            picker.CreateControl(); var loading = picker.ReloadAsync();
            Assert.IsFalse(Descendants(picker).OfType<Label>().Single(label => label.Text == "Project đang hoạt động").Visible);
            Assert.IsFalse(Descendants(picker).OfType<Label>().Single(label => label.Text == "Project đã lưu trữ").Visible);
            StringAssert.Contains(Field<Label>(picker, "message").Text, "Đang tải");
            CapturePicker(picker, "picker-loading");
            pending.SetResult(Error(404)); Complete(loading);
            Assert.AreEqual(0, Descendants(picker).OfType<ProjectListRowControl>().Count());
            StringAssert.Contains(Field<Label>(picker, "message").Text, "Không thể tải danh sách Project.");
            Assert.IsFalse(Field<Label>(picker, "message").Text.Contains("Bạn chưa có Project"));
            CapturePicker(picker, "picker-first-load-error");
            fixture.ListResponse = null; Complete(picker.ReloadAsync());
            Assert.IsTrue(Descendants(picker).OfType<Label>().Single(label => label.Text == "Project đang hoạt động").Visible);
            Assert.IsTrue(Descendants(picker).OfType<Label>().Single(label => label.Text == "Project đã lưu trữ").Visible);
            Assert.IsFalse(Field<Label>(picker, "message").Visible);
            CapturePicker(picker, "picker-retry-success");
        });
    }
    [TestMethod]
    public void Picker_RefreshFailureKeepsPreviousRowsAndCurrent_WithStaleListNotice()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            using var picker = new ProjectPickerControl(fixture.Projects); picker.CreateControl(); Complete(picker.ReloadAsync());
            var oldRows = Descendants(picker).OfType<ProjectListRowControl>().ToArray();
            fixture.ListResponse = () => Task.FromResult(Error(503)); Complete(picker.ReloadAsync());
            CollectionAssert.AreEqual(oldRows, Descendants(picker).OfType<ProjectListRowControl>().ToArray());
            StringAssert.Contains(Field<Label>(picker, "message").Text, "lần tải thành công trước");
            Assert.IsTrue(Field<Label>(picker, "message").Visible);
            Assert.AreEqual(81L, fixture.Projects.Context.Current!.ProjectId); Assert.IsTrue(fixture.Session.IsAuthenticated);
            CapturePicker(picker, "picker-refresh-error");
        });
    }
    [TestMethod]
    public void Picker_LateEmptyResponseCannotHideNewerSuccessfulList()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); using var picker = new ProjectPickerControl(fixture.Projects);
            var pending = new TaskCompletionSource<HttpResponseMessage>(); fixture.ListResponse = () => pending.Task;
            picker.CreateControl(); var oldLoading = picker.ReloadAsync();
            fixture.ListResponse = null; Complete(picker.ReloadAsync());
            pending.SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ProjectListResponse([])) });
            Complete(oldLoading);
            Assert.AreEqual(3, Descendants(picker).OfType<ProjectListRowControl>().Count());
            Assert.IsTrue(Descendants(picker).OfType<Label>().Single(label => label.Text == "Project đang hoạt động").Visible);
            Assert.IsFalse(Field<Label>(picker, "message").Visible);
        });
    }
    private static void CapturePicker(ProjectPickerControl picker, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("CREATORFLOW_UI_TEST_EVIDENCE");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        picker.Size = new(960, 600); picker.PerformLayout();
        using var bitmap = new System.Drawing.Bitmap(picker.Width, picker.Height);
        picker.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, picker.Width, picker.Height));
        bitmap.Save(Path.Combine(directory, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
    }
    [TestMethod]
    public void Picker_ArchivedRowsCannotSelectCurrent_AndHasNoShellChrome()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); using var picker = new ProjectPickerControl(fixture.Projects);
            picker.CreateControl(); Complete(picker.ReloadAsync());
            var archived = Descendants(picker).OfType<ProjectListRowControl>().Single(row => (long)row.Tag! == 82);
            long? selected = null; picker.SelectRequested += id => selected = id; archived.PerformClick(); Application.DoEvents();
            Assert.IsNull(selected); Assert.IsFalse(archived.SelectableProject);
            Assert.IsFalse(Descendants(picker).Any(control => control is SidebarControl or TopHeaderControl));
        });
    }
    [TestMethod]
    public void Switcher_OnlyActiveRecentProjects_LimitsFive_AndRequestsSelection()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture();
            fixture.ListResponse = () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ProjectListResponse(
                Enumerable.Range(1, 7).Select(id => new ProjectListItem(id, "Project " + id, "OWNER", "ACTIVE")).Append(new(82, "Archived B", "OWNER", "ARCHIVED")).ToArray())) });
            fixture.Projects.Context.Apply(new(7, "Project 7", "OWNER", "ACTIVE"));
            using var switcher = new ProjectSwitcherControl(fixture.Projects, new long[] { 7, 3, 82 });
            using var host = new ToolStripControlHost(switcher) { AutoSize = false, Size = new(320, 120) };
            switcher.ContentSizeChanged += (_, _) => host.Size = switcher.PopupContentSize;
            Complete(switcher.ReloadAsync()); switcher.PerformLayout();
            var buttons = Descendants(switcher).OfType<Button>().ToArray();
            Assert.AreEqual(7, buttons.Length); StringAssert.Contains(buttons[0].Text, "Project 7"); StringAssert.Contains(buttons[1].Text, "Project 3");
            Assert.IsFalse(buttons.Any(button => button.Text.Contains("Archived")));
            Assert.AreEqual("Thêm Project", buttons[5].Text); Assert.AreEqual("Quản lý Project", buttons[6].Text);
            Assert.IsTrue(host.Height > 120, "Host must grow to fit all five Projects and both actions");
            Assert.IsTrue(buttons[6].Parent!.Top + buttons[6].Bottom <= switcher.ClientSize.Height - switcher.Padding.Bottom, "Management action must fit in the hosted viewport");
            long? selected = null; switcher.SelectRequested += id => selected = id; buttons[1].PerformClick(); Assert.AreEqual(3L, selected);
        });
    }
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Shell_ProjectDropdownFitsAnchor_DuringLoadingAndAfterResponse(bool fail)
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture();
            fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            var pending = new TaskCompletionSource<HttpResponseMessage>();
            fixture.ListResponse = () => pending.Task;
            fixture.Shell.Show(); Application.DoEvents();
            var opening = InvokeTask(fixture.Shell, "ShowProjectMenuAsync");
            Application.DoEvents();
            var menu = Field<ToolStripDropDown>(fixture.Shell, "_projectMenu");
            var sidebar = Field<SidebarControl>(fixture.Shell, "_sidebarControl");
            void AssertFits()
            {
                Assert.IsTrue(menu.Width <= sidebar.ProjectMenuAnchorWidth, "Popup including its chrome must fit the selector width");
                Assert.AreEqual(sidebar.ProjectMenuScreenLocation.X, menu.Left, "Popup must align with the selector's left edge");
                var switcher = (ProjectSwitcherControl)((ToolStripControlHost)menu.Items[0]).Control;
                Assert.IsFalse(switcher.HorizontalScroll.Visible, "Narrow popup must not need horizontal scrolling");
                foreach (var label in Descendants(switcher).OfType<Label>())
                    Assert.IsTrue(label.Width + label.Margin.Horizontal <= switcher.ClientSize.Width - switcher.Padding.Horizontal, "Loading/error text must wrap within popup content");
            }
            AssertFits();
            pending.SetResult(fail ? Error(503) : new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(new ProjectListResponse([new(81, "Project có tên rất dài để kiểm tra menu trong sidebar", "OWNER", "ACTIVE")])) });
            Complete(opening); Application.DoEvents(); AssertFits();
            fixture.Shell.Close();
        });
    }
    [TestMethod]
    [DataRow("OWNER", true)]
    [DataRow("MANAGER", false)]
    [DataRow("CREATOR", false)]
    public void Management_DangerZoneOnlyCurrentOwner_ArchivedRowsCannotSwitch(string role, bool owner)
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.CurrentRole = role;
            fixture.Projects.Context.Apply(new(81, "A", role, "ACTIVE"));
            using var settings = new ProjectSettingsControl(fixture.Projects); Complete(settings.LoadProjectAsync());
            Assert.AreEqual(owner, Field<Button>(settings, "archive").Visible); Assert.AreEqual(owner, Field<Button>(settings, "delete").Visible);
            Assert.IsFalse(Descendants(settings).OfType<TextBox>().Any());
            var archived = Field<TableLayoutPanel>(settings, "archivedRows");
            Assert.IsFalse(Descendants(archived).OfType<ProjectListRowControl>().Single().SelectableProject);
            Assert.AreEqual(81L, fixture.Projects.Context.Current!.ProjectId);
            Assert.AreEqual(4, Descendants(settings).OfType<Label>().Count(label => label.Text == "Chưa khả dụng"));
            var members = Descendants(settings).OfType<Label>().Single(label => label.Text.StartsWith("Thành viên", StringComparison.Ordinal));
            Assert.AreEqual(owner, members.Enabled);
        });
    }
    [TestMethod]
    public void Management_NoCurrentHasNoMutation_AndLateDetailCannotReplaceCurrentC()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); using var settings = new ProjectSettingsControl(fixture.Projects);
            Complete(settings.LoadProjectAsync()); Assert.IsFalse(Field<Button>(settings, "delete").Visible);
            fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            var pending = new TaskCompletionSource<bool>();
            var responses = new List<Task<HttpResponseMessage>>();
            async Task<HttpResponseMessage> DelayedDetail(long id)
            {
                if (id == 81) await pending.Task;
                return await UxFixture.Detail(id, id == 81 ? "OWNER" : "CREATOR");
            }
            fixture.DetailResponse = id => { var response = DelayedDetail(id); responses.Add(response); return response; };
            var loading = settings.LoadProjectAsync();
            fixture.Projects.Context.Apply(new(83, "C", "CREATOR", "ACTIVE"));
            pending.SetResult(true);
            Complete(loading); Complete(Task.WhenAll(responses.ToArray()));
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Field<bool>(settings, "busy") && DateTime.UtcNow < deadline) Application.DoEvents();
            Assert.IsFalse(Field<bool>(settings, "busy"), "The reload for Current C must finish before assertions");
            Assert.AreEqual(83L, fixture.Projects.Context.Current!.ProjectId);
            Assert.IsFalse(Field<Button>(settings, "delete").Visible);
        });
    }
    [TestMethod]
    public void Picker_QueuesOneActivation_AndDisposalBeforeDispatchCancelsNavigation()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); using var picker = new ProjectPickerControl(fixture.Projects);
            picker.CreateControl(); Complete(picker.ReloadAsync());
            var row = Descendants(picker).OfType<ProjectListRowControl>().First(item => item.SelectableProject);
            int requests = 0; picker.SelectRequested += _ => requests++;
            row.PerformClick(); row.PerformClick(); Assert.AreEqual(0, requests);
            Application.DoEvents(); Assert.AreEqual(1, requests);
            row.PerformClick(); picker.Dispose(); Application.DoEvents(); Assert.AreEqual(1, requests);
        });
    }
    [TestMethod]
    [DataRow(401)]
    [DataRow(403)]
    [DataRow(404)]
    [DataRow(409)]
    public void Management_CurrentAccessErrorsSeparateAuthFromProjectValidity(int status)
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            fixture.DetailResponse = id => id == 81 ? Task.FromResult(Error(status)) : UxFixture.Detail(id, "OWNER");
            Complete(InvokeTask(fixture.Shell, "ShowProjectSettingsAsync"));
            Assert.IsNull(fixture.Projects.Context.Current); Assert.AreEqual(status != 401, fixture.Session.IsAuthenticated);
        });
    }
    [TestMethod]
    [DataRow("network")]
    [DataRow("timeout")]
    [DataRow("unavailable")]
    public void Management_TransportFailureKeepsCurrent_AndRetryOnlyReads(string failure)
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            fixture.ListResponse = failure switch
            {
                "network" => () => Task.FromException<HttpResponseMessage>(new HttpRequestException("Fixture offline")),
                "timeout" => () => Task.FromException<HttpResponseMessage>(new TaskCanceledException("Fixture timeout")),
                _ => () => Task.FromResult(Error(503))
            };
            using var settings = new ProjectSettingsControl(fixture.Projects); Complete(settings.LoadProjectAsync());
            Assert.AreEqual(81L, fixture.Projects.Context.Current!.ProjectId); Assert.IsTrue(fixture.Session.IsAuthenticated);
            Assert.IsFalse(Field<Button>(settings, "delete").Enabled);
            fixture.ListResponse = null; Complete(settings.LoadProjectAsync());
            Assert.IsTrue(Field<Button>(settings, "delete").Enabled); Assert.IsTrue(fixture.Methods.All(method => method == HttpMethod.Get));
        });
    }
    [TestMethod]
    public void UnknownMutationOutcome_ReconcilesReadOnly_NoDeleteOrArchiveResend()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            using var settings = new ProjectSettingsControl(fixture.Projects); Complete(settings.LoadProjectAsync());
            typeof(ProjectSettingsControl).GetField("uncertainTarget", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, (long?)81);
            Complete(settings.LoadProjectAsync()); Assert.AreEqual(81L, fixture.Projects.Context.Current!.ProjectId);
            Assert.IsTrue(fixture.Methods.All(method => method == HttpMethod.Get));
            StringAssert.Contains(Field<Label>(settings, "message").Text, "xác nhận lại");
        });
    }
    [TestMethod]
    public void SuccessfulSwitchUpdatesSessionRecency_AndSelectingCurrentDoesNotReset()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); CompleteSelection(fixture.Shell, 81L, true);
            CompleteSelection(fixture.Shell, 83L, true);
            CollectionAssert.AreEqual(new long[] { 83, 81 }, Field<List<long>>(fixture.Shell, "_recentProjects"));
            long generation = fixture.Projects.Context.Generation;
            CompleteSelection(fixture.Shell, 83L, true); Assert.AreEqual(generation, fixture.Projects.Context.Generation);
        });
    }
    [TestMethod]
    public void SwitchConfirmation_CancelKeepsCurrentAndOldCards_ConfirmChangesCurrent()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            var cards = Field<List<ContentBoardCard>>(fixture.Shell, "_allCards"); cards.Add(new ContentBoardCard { ContentId = 101, ProjectId = 81, Title = "Old A" });
            var detail = Field<ContentDetailPanel>(fixture.Shell, "_contentDetailPanel");
            typeof(ContentDetailPanel).GetField("_isDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(detail, true);
            long generation = fixture.Projects.Context.Generation;
            CompleteSelection(fixture.Shell, 83L, false);
            Assert.AreEqual(81L, fixture.Projects.Context.Current!.ProjectId); Assert.AreEqual(generation, fixture.Projects.Context.Generation); Assert.AreEqual(1, cards.Count);
            Assert.IsTrue(detail.HasUnsavedChanges);
            CompleteSelection(fixture.Shell, 83L, true);
            Assert.AreEqual(83L, fixture.Projects.Context.Current!.ProjectId); Assert.AreEqual(0, cards.Count);
            Assert.IsFalse(detail.HasUnsavedChanges);
        });
    }
    [TestMethod]
    public void Gate_OnlyOpensShellAfterConfirmedAccess_NoSidebarOrTopbarBeforeSelection()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); using var gate = new ProjectSelectionForm(new AuthApiFacade(fixture.Api, fixture.Session), fixture.Projects);
            gate.CreateControl(); int ready = 0; gate.ProjectReady += (_, _) => ready++;
            Assert.IsFalse(Descendants(gate).Any(control => control is SidebarControl or TopHeaderControl));
            using (var answer = AnswerSwitch(false)) Complete(InvokeTask(gate, "SelectAsync", 81L, true));
            Assert.AreEqual(0, ready); Assert.IsNull(fixture.Projects.Context.Current);
            using (var answer = AnswerSwitch(true)) Complete(InvokeTask(gate, "SelectAsync", 81L, true));
            Assert.AreEqual(1, ready); Assert.AreEqual(81L, fixture.Projects.Context.Current!.ProjectId);
        });
    }
    [TestMethod]
    public void Gate_SessionClearedWhileAccessPending_DoesNotPublishCurrentOrOpenShell()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture();
            using var gate = new ProjectSelectionForm(new AuthApiFacade(fixture.Api, fixture.Session), fixture.Projects);
            var pending = new TaskCompletionSource<HttpResponseMessage>();
            fixture.AccessResponse = () => pending.Task;
            int ready = 0; gate.ProjectReady += (_, _) => ready++;
            var selection = InvokeTask(gate, "SelectAsync", 81L, false);
            fixture.Session.Clear();
            pending.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(new ProjectAccessResponse(81, "A", "OWNER", "ACTIVE")) });
            Complete(selection);
            Assert.AreEqual(0, ready); Assert.IsNull(fixture.Projects.Context.Current);
            Assert.IsFalse(fixture.Session.IsAuthenticated);
        });
    }
    private static System.Windows.Forms.Timer AnswerSwitch(bool accept)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.OfType<ProjectSwitchDialog>().LastOrDefault();
            if (dialog is null) return;
            var button = Descendants(dialog).OfType<Button>().Single(item => item.Name == (accept ? "projectConfirm" : "projectCancel"));
            timer.Stop(); button.PerformClick();
        };
        timer.Start(); return timer;
    }
    private static void CompleteSelection(BoardForm shell, long id, bool accept)
    {
        using var answer = AnswerSwitch(accept); Complete(InvokeTask(shell, "SelectProjectAsync", id));
    }
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IntegratedShell_RealProjectKeepsBoardAndDevelopNavigation_WithoutDemo(bool admin)
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(admin);
            fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            fixture.Shell.Show(); Application.DoEvents();
            Assert.IsTrue(Field<ModulePageHeaderControl>(fixture.Shell, "_modulePageHeader").Visible);
            Assert.IsTrue(Field<FlowLayoutPanel>(fixture.Shell, "_flowColumns").Visible);
            var columns = Descendants(fixture.Shell).OfType<KanbanColumnControl>().ToArray();
            Assert.AreEqual(7, columns.Length); Assert.IsTrue(columns.All(column => !column.SourceAvailable));
            Assert.AreEqual(0, Field<List<ContentBoardCard>>(fixture.Shell, "_allCards").Count);
            Assert.AreEqual(0, ((DemoReadTrap)Field<IBoardRepository>(fixture.Shell, "_boardRepository")).Reads);
            var sidebar = Field<SidebarControl>(fixture.Shell, "_sidebarControl");
            var items = ((System.Collections.IEnumerable)Field<object>(sidebar, "_items")).Cast<object>().ToArray();
            var titles = items.Select(item => (string)item.GetType().GetField("Title")!.GetValue(item)!).ToArray();
            foreach (string title in new[] { "Kho Content", "Backlog", "Calendar", "My Tasks", "Invitations", "Project Settings" })
                CollectionAssert.Contains(titles, title);
            Assert.AreEqual(admin, titles.Contains("Quản trị người dùng"));
            if (admin)
            {
                sidebar.ScrollNavigation(-1200);
                foreach (var item in items.Where(item => ((string)item.GetType().GetField("Title")!.GetValue(item)!) == "Báo cáo vi phạm"))
                    Assert.IsTrue((bool)typeof(SidebarControl).GetMethod("IsNavigationVisible", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(sidebar, [item])!);
            }
            fixture.Shell.Close();
        });
    }

    [TestMethod]
    public void ContentLibrary_LoadsProjectContentAndOpensArchivedContentDetailThroughApi()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(contentBackends: true);
            fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));

            Complete(InvokeTask(fixture.Shell, "OpenContentLibraryAsync"));

            var library = Field<Control>(fixture.Shell, "_projectPage") as ContentLibraryControl;
            Assert.IsNotNull(library);
            Assert.AreEqual(1, library.TotalContentCount);
            CollectionAssert.Contains(fixture.Paths, "/api/projects/81/contents");
            var row = Descendants(library).OfType<ContentLibraryRowControl>().Single();
            Assert.AreEqual(ContentStatus.Archived, row.Content.Status);

            typeof(BoardForm).GetMethod("OpenContentLibraryDetail", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(fixture.Shell, [901L]);

            var detail = Field<ContentDetailPanel>(fixture.Shell, "_contentDetailPanel");
            var loadedContent = Field<ContentBoardCard?>(detail, "_current");
            Assert.IsNotNull(loadedContent);
            Assert.AreEqual(901L, loadedContent.ContentId);
            Assert.AreEqual("Archived video", loadedContent.Title);
            Assert.IsTrue(Field<bool>(detail, "_detailLoaded"));
            CollectionAssert.Contains(fixture.Paths, "/api/contents/901");
            CollectionAssert.Contains(fixture.Paths, "/api/contents/901/references");
        });
    }
    [TestMethod]
    public void IntegratedCalendar_IsUnavailableAndDisposedOnProjectSwitch()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); CompleteSelection(fixture.Shell, 81, true);
            typeof(BoardForm).GetMethod("OpenCalendar", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fixture.Shell, null);
            var calendar = Field<CalendarControl>(fixture.Shell, "_calendar");
            Assert.IsNotNull(calendar.UnavailableReason);
            Assert.IsNull(calendar.CardProvider!());
            CompleteSelection(fixture.Shell, 83, true);
            Assert.IsTrue(calendar.IsDisposed); Assert.IsNull(Field<CalendarControl?>(fixture.Shell, "_calendar"));
        });
    }
    [TestMethod]
    public void MyTasksPage_UsesApiForCurrentProjectAndProgress_WithoutLocalContent()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.CurrentRole = "CREATOR";
            fixture.Projects.Context.Apply(new(81, "A", "CREATOR", "ACTIVE"));
            using var page = new MyTasksControl(new ApiMyTaskBackend(fixture.Api, fixture.Session)) { Dock = DockStyle.Fill };
            using var host = new Form { ClientSize = new System.Drawing.Size(1120, 700) };
            host.Controls.Add(page);
            host.Show(); Application.DoEvents();
            var list = Field<ListView>(page, "listViewTasks");
            Assert.AreEqual(1, list.Items.Count); StringAssert.Contains(list.Items[0].SubItems[1].Text, "Server task");
            CollectionAssert.Contains(fixture.Paths, "/api/projects/81/my-tasks");
            list.Items[0].Selected = true; list.Select(); Application.DoEvents();
            Field<NumericUpDown>(page, "nudProgress").Value = 60;
            Field<Button>(page, "btnSaveProgress").PerformClick(); Application.DoEvents();
            CollectionAssert.Contains(fixture.Paths, "/api/contents/101/progress");
            Assert.IsFalse(Field<Button>(page, "btnSaveDeadline").Enabled);
            host.Close();
        });
    }
    [TestMethod]
    public void MyTasksPage_ReadErrorAndRetry_DoNotClaimEmptyOrClearAuth()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            fixture.TaskResponse = () => Task.FromResult(Error(503));
            using var page = new MyTasksControl(new ApiMyTaskBackend(fixture.Api, fixture.Session)) { Dock = DockStyle.Fill };
            using var host = new Form { ClientSize = new System.Drawing.Size(1120, 700) };
            host.Controls.Add(page);
            host.Show(); Application.DoEvents();
            Assert.AreEqual("Không tải được công việc", Field<string>(page, "_emptyTitle"));
            Assert.IsTrue(fixture.Session.IsAuthenticated); Assert.AreEqual(81L, fixture.Projects.Context.Current!.ProjectId);
            fixture.TaskResponse = null;
            Field<Button>(page, "btnRefresh").PerformClick(); Application.DoEvents();
            Assert.AreEqual(1, Field<ListView>(page, "listViewTasks").Items.Count);
            host.Close();
        });
    }
    [TestMethod]
    public void MyTasksPage_ContextChangeClearsOldProjectBeforeAnotherRequest()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            long generation = fixture.Projects.Context.Generation;
            using var page = new MyTasksControl(new ApiMyTaskBackend(fixture.Api, fixture.Session), () => generation == fixture.Projects.Context.Generation)
                { Dock = DockStyle.Fill };
            using var host = new Form { ClientSize = new System.Drawing.Size(1120, 700) };
            host.Controls.Add(page);
            host.Show(); Application.DoEvents(); int requests = fixture.Paths.Count;
            fixture.Projects.Context.Apply(new(83, "C", "CREATOR", "ACTIVE"));
            typeof(MyTasksControl).GetMethod("ReloadTasks", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, null);
            Assert.IsFalse(page.IsDisposed);
            Assert.AreEqual("Project đã thay đổi", Field<string>(page, "_emptyTitle"));
            Assert.AreEqual(requests, fixture.Paths.Count);
            host.Close();
        });
    }
    [TestMethod]
    [DataRow(401, false)]
    [DataRow(403, true)]
    public void MyTaskApi_UnauthorizedClearsAuth_ButForbiddenPreservesIt(int status, bool authenticated)
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture(); fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            fixture.TaskResponse = () => Task.FromResult(Error(status));
            Assert.Throws<UnauthorizedWorkflowActionException>(() => new ApiMyTaskBackend(fixture.Api, fixture.Session).GetMyTasks(81));
            Assert.AreEqual(authenticated, fixture.Session.IsAuthenticated);
            Assert.AreEqual(authenticated, fixture.Projects.Context.Current is not null);
        });
    }
    [TestMethod]
    public void Gate_HasAccountInvitationEntryBeforeSelectingCurrentProject()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture();
            using var gate = new ProjectSelectionForm(new AuthApiFacade(fixture.Api, fixture.Session), fixture.Projects, new InvitationInboxService(fixture.Api, fixture.Session));
            Assert.IsNull(fixture.Projects.Context.Current);
            Assert.IsTrue(Descendants(gate).OfType<Button>().Any(button => button.Text == "Lời mời của tôi"));
            Assert.IsFalse(Descendants(gate).Any(control => control is SidebarControl or TopHeaderControl));
        });
    }
    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    [TestMethod]
    public void ReviewMenu_ChangesWithProjectRoleAndLogout()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture();
            var sidebar = Field<SidebarControl>(fixture.Shell, "_sidebarControl");
            fixture.Projects.Context.Apply(new(81, "A", "OWNER", "ACTIVE"));
            Assert.IsTrue(sidebar.ReviewActionsEnabled);
            var detail = Field<ContentDetailPanel>(fixture.Shell, "_contentDetailPanel");
            var roles = Field<Dictionary<long, ProjectRole?>>(detail, "_roleCache");
            roles[81] = ProjectRole.Owner;
            fixture.Projects.Context.Apply(new(83, "C", "CREATOR", "ACTIVE"));
            Assert.IsFalse(sidebar.ReviewActionsEnabled);
            Assert.AreEqual(0, roles.Count);
            fixture.Projects.Context.Apply(new(83, "C", "MANAGER", "ACTIVE"));
            Assert.IsTrue(sidebar.ReviewActionsEnabled);
            fixture.Session.Clear();
            Assert.IsFalse(sidebar.ReviewActionsEnabled);
            Assert.IsNull(CurrentSession.CurrentUserRole);
        });
    }

    [TestMethod]
    public void BackendRejection_RefreshesCurrentRoleWithoutRetryingWrite()
    {
        RunSta(() =>
        {
            using var fixture = new UxFixture();
            _ = fixture.Shell.Handle;
            fixture.Projects.Context.Apply(new(81, "Project 81", "OWNER", "ACTIVE"));
            fixture.CurrentRole = "CREATOR";
            fixture.Session.NotifyAuthorizationRejected(fixture.Session.Generation);
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (Field<bool>(fixture.Shell, "_authorizationRefreshQueued") && DateTime.UtcNow < deadline) Application.DoEvents();
            Assert.IsFalse(Field<bool>(fixture.Shell, "_authorizationRefreshQueued"));
            Assert.AreEqual(ProjectRole.Creator, fixture.Projects.Context.Current!.Role);
            Assert.IsFalse(Field<SidebarControl>(fixture.Shell, "_sidebarControl").ReviewActionsEnabled);
            Assert.IsTrue(fixture.Paths.Contains("/api/projects/81/access"));
            Assert.IsTrue(fixture.Methods.All(method => method == HttpMethod.Get));
        });
    }

    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static Task InvokeTask(object target, string name, params object[] args) => (Task)target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args)!;
    private static void Complete(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!task.IsCompleted && DateTime.UtcNow < deadline) Application.DoEvents();
        Assert.IsTrue(task.IsCompleted, "Fixture task did not finish"); task.GetAwaiter().GetResult();
    }
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception error) { failure = error; } });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(15)), "UX fixture did not finish");
        if (failure is not null) throw new AssertFailedException(failure.ToString());
    }
    private static HttpResponseMessage Error(int status) => new((HttpStatusCode)status)
        { Content = JsonContent.Create(new { code = "fixture_error", title = "Fixture error" }) };
    private sealed class UxFixture : IDisposable
    {
        private readonly HttpClient http;
        private readonly ApiClient api;
        public ApiClient Api => api;
        public UserSession Session { get; } = new();
        public ProjectApiFacade Projects { get; }
        public BoardForm Shell { get; }
        public List<string> Paths { get; } = [];
        public List<HttpMethod> Methods { get; } = [];
        public Func<Task<HttpResponseMessage>>? AccessResponse { get; set; }
        public Func<long, Task<HttpResponseMessage>>? DetailResponse { get; set; }
        public string CurrentRole { get; set; } = "OWNER";
        public static Task<HttpResponseMessage> Detail(long id, string role) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ProjectResponse(id, id == 81 ? "A" : id == 82 ? "Archived B" : "C", "Description", "Owner", role, id == 82 ? "ARCHIVED" : "ACTIVE", DateTimeOffset.UtcNow)) });
        public Func<Task<HttpResponseMessage>>? ListResponse { get; set; }
        public Func<Task<HttpResponseMessage>>? TaskResponse { get; set; }
        public bool ContentBackends { get; }
        public UxFixture(bool systemAdmin = false, bool contentBackends = false)
        {
            ContentBackends = contentBackends;
            http = new HttpClient(new UxHandler(this)) { BaseAddress = new("http://fixture.test/") }; api = new ApiClient(http);
            Session.TrySetLogin(new LoginResponse("fixture", DateTimeOffset.UtcNow.AddMinutes(30), new(42, "fixture@example.test", "Fixture", systemAdmin)), 0);
            CurrentSession.CurrentUserId = 42; CurrentSession.CurrentUserName = "Fixture";
            Projects = new ProjectApiFacade(api, Session);
            var content = new InMemoryContentRepository(); var history = new InMemoryContentStatusHistoryRepository();
            var reviews = new InMemoryReviewRepository(); var members = new InMemoryProjectMemberRepository();
            var tasks = new InMemoryMyTaskRepository(); var uow = new InMemoryUnitOfWork();
            Shell = new BoardForm(new WorkflowService(content, history, reviews, members, uow), new DemoReadTrap(),
                new InMemoryReviewQueueRepository(), new InMemoryActivityRepository(), members,
                new ContentService(content, new InMemoryContentDetailsRepository(), history, members, new InMemoryPlatformRepository(), uow, tasks),
                new MyTaskService(tasks, members, uow),
                new ScriptService(new InMemoryContentScriptRepository(), new InMemoryContentDetailsRepository(), new InMemoryPlatformRepository(), members, tasks, uow, new NotConfiguredAiService()),
                new IdeaService(new InMemoryIdeaRepository(withSeed: false), members, history, uow),
                auth: new AuthApiFacade(api, Session), projects: Projects, taskBackend: new ApiMyTaskBackend(api, Session),
                boardBackend: contentBackends ? new ApiBoardBackend(api, Session) : null,
                detailBackend: contentBackends ? new ApiContentDetailBackend(api, Session) : null,
                assignmentBackend: contentBackends ? new ApiAssignmentBackend(api, Session) : null);
            Field<System.Windows.Forms.Timer>(Shell, "_projectTimer").Stop();
        }
        public void Dispose() { Field<System.Windows.Forms.Timer>(Shell, "_projectTimer").Dispose(); Shell.Dispose(); Projects.Dispose(); Session.Clear(); api.Dispose(); http.Dispose(); }
        private sealed class UxHandler(UxFixture fixture) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                string path = request.RequestUri!.AbsolutePath; fixture.Paths.Add(path); fixture.Methods.Add(request.Method);
                if (request.Method == HttpMethod.Get && path == "/api/users/me/avatar") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                if (request.Method == HttpMethod.Get && path == "/api/users/me") return Reply(new ProfileResponse(42, "fixture@example.test", "Fixture", "Active", fixture.Session.CurrentUser!.IsSystemAdmin, null));
                if (path.EndsWith("/my-role")) return Reply(new ProjectRoleResponse(fixture.CurrentRole == "OWNER" ? "Owner" : fixture.CurrentRole == "MANAGER" ? "Manager" : "Creator"));
                if (path.EndsWith("/my-tasks")) return fixture.TaskResponse?.Invoke() ?? Reply(new MyTasksResponse([TaskItem(long.Parse(path.Split('/')[3]))]));
                if (path == "/api/platforms") return Reply(new PlatformOptionsResponse(["TikTok", "YouTube"]));
                if (path.EndsWith("/assignable-members")) return Reply(new AssignableMembersResponse([new(7, "Creator One", "Creator")]));
                if (path == "/api/projects/81/contents" && fixture.ContentBackends)
                    return Reply(new BoardResponse([new BoardCardResponse(901, 81, "CNT-901", "Archived video", "Card description", "Archived", "Low", null, null, ["TikTok"], [], false,
                        [new BoardAssigneeResponse(901, 7, "Creator One", "InProgress", 45, null)])]));
                if (path == "/api/contents/901") return Reply(new ContentDetailResponse(901, 81, "CNT-901", "Archived video detail", "Detail description", "Video", "Archived", "Low", null, null, 42, ["TikTok"], "Owner"));
                if (path == "/api/contents/901/references") return Reply(new ContentReferencesResponse(901, false, []));
                if (path == "/api/contents/901/script") return Reply(new ScriptResponse(901, 81, string.Empty, "v1", true, false, null));
                if (path == "/api/contents/901/activity") return Reply(new ActivityResponse(901, []));
                if (path == "/api/contents/101/progress") return Reply(TaskItem(81, 60));
                if (path.EndsWith("/access")) return fixture.AccessResponse?.Invoke() ?? Reply(new ProjectAccessResponse(long.Parse(path.Split('/')[3]), "Project " + path.Split('/')[3], fixture.CurrentRole, "ACTIVE"));
                if (path == "/api/projects") return fixture.ListResponse?.Invoke() ?? Reply(new ProjectListResponse([new(81, "A", fixture.CurrentRole, "ACTIVE"), new(82, "Archived B", "OWNER", "ARCHIVED"), new(83, "C", "CREATOR", "ACTIVE")]));
                long id = long.Parse(path.Split('/')[3]);
                return fixture.DetailResponse?.Invoke(id) ?? Detail(id, id == 81 ? fixture.CurrentRole : id == 83 ? "CREATOR" : "OWNER");
            }
            private static MyTaskResponse TaskItem(long projectId, int progress = 20) => new(1, 101, projectId, 42, "CNT-101", "Server task", "Script", "InProgress", "High", null, progress, "Owner", ["TikTok"], 1, 0);
            private static Task<HttpResponseMessage> Reply<T>(T body) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        }
    }
    [TestMethod]
    public void ServerProjectSwitch_ClearsOldCardsWithoutReadingDemoSource()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var http = new HttpClient(new AccessHandler()) { BaseAddress = new("http://fixture.test/") };
                using var api = new ApiClient(http); var session = new UserSession();
                session.TrySetLogin(new LoginResponse("fixture", DateTimeOffset.UtcNow.AddMinutes(30), new(42, "fixture@example.test", "Fixture", false)), 0);
            CurrentSession.CurrentUserId = 42; CurrentSession.CurrentUserName = "Fixture";
                using var projects = new ProjectApiFacade(api, session);
                var content = new InMemoryContentRepository(); var history = new InMemoryContentStatusHistoryRepository();
                var reviews = new InMemoryReviewRepository(); var members = new InMemoryProjectMemberRepository();
                var tasks = new InMemoryMyTaskRepository(); var uow = new InMemoryUnitOfWork();
                var workflow = new WorkflowService(content, history, reviews, members, uow);
                var contentService = new ContentService(content, new InMemoryContentDetailsRepository(), history, members, new InMemoryPlatformRepository(), uow, tasks);
                var source = new DemoReadTrap();
                using var shell = new BoardForm(workflow, source, new InMemoryReviewQueueRepository(), new InMemoryActivityRepository(), members,
                    contentService, new MyTaskService(tasks, members, uow),
                    new ScriptService(new InMemoryContentScriptRepository(), new InMemoryContentDetailsRepository(), new InMemoryPlatformRepository(), members, tasks, uow, new NotConfiguredAiService()),
                    new IdeaService(new InMemoryIdeaRepository(withSeed: false), members, history, uow),
                    auth: new AuthApiFacade(api, session), projects: projects);
                CompleteSelection(shell, 81L, true);
                var cards = (List<ContentBoardCard>)typeof(BoardForm).GetField("_allCards", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(shell)!;
                cards.Add(new ContentBoardCard { ContentId = 101, ProjectId = 81, Title = "Old A" });
                CompleteSelection(shell, 82L, true);
                Assert.AreEqual(82L, projects.Context.Current!.ProjectId); Assert.AreEqual("Project 82", CurrentSession.CurrentProjectName);
                Assert.AreEqual(0, cards.Count); Assert.AreEqual(0, source.Reads);
                session.Clear(); Assert.IsNull(projects.Context.Current); Assert.AreEqual(0L, CurrentSession.CurrentProjectId);
            }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(15)), "Shell fixture did not finish");
        if (failure is not null) throw new AssertFailedException(failure.ToString());
    }
    private sealed class DemoReadTrap : IBoardRepository
    {
        public int Reads { get; private set; }
        public List<ContentBoardCard> GetBoardCards(long projectId) { Reads++; throw new InvalidOperationException("Server Project must not read demo repository"); }
    }
    private sealed class AccessHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            long id = long.Parse(request.RequestUri!.AbsolutePath.Split('/')[3]);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ProjectAccessResponse(id, $"Project {id}", "OWNER", "ACTIVE")) });
        }
    }
}
