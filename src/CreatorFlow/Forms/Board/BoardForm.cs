using CreatorFlow.Models.Enums;
using CreatorFlow.Contracts.Search;
using CreatorFlow.Controls;
using CreatorFlow.Controls.IdeaBank;
using CreatorFlow.Controls.MyTasks;
using CreatorFlow.Forms.Invitations;
using CreatorFlow.Forms.ProjectSettings;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

/// <summary>
/// Màn hình Production Board. Layout tĩnh nằm trong BoardForm.Designer.cs,
/// file này chỉ chứa dữ liệu, sự kiện và logic.
/// </summary>
public partial class BoardForm : Form, IMessageFilter
{
    private static readonly ContentStatus[] PipelineStages =
    {
        ContentStatus.Idea, ContentStatus.Script, ContentStatus.Production,
        ContentStatus.Editing, ContentStatus.Review, ContentStatus.Ready, ContentStatus.Published,
    };

    private const int WM_MOUSEWHEEL = 0x020A;
    private const int MK_SHIFT = 0x0004;

    private readonly WorkflowService _workflowService;
    private readonly IBoardRepository _boardRepository;
    private readonly IReviewQueueRepository _reviewQueueRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly ContentService _contentService;
    private readonly MyTaskService _myTaskService;
    private readonly IdeaService _ideaService;
    private readonly IIdeaBackend? _ideaBackend;
    private readonly IBoardBackend? _boardBackend;
    private readonly IContentDetailBackend? _detailBackend;
    private readonly IAssignmentBackend? _assignmentBackend;
    private readonly IReviewQueueBackend? _reviewQueueBackend;
    private readonly INotificationBackend? _notificationBackend;
    private NotificationPanelControl? _notificationPanel;
    private long? _reopenContentId; // chế độ API: mở lại drawer của thẻ này sau khi Board tải lại (sau khi lưu/đổi trạng thái/giao việc)
    private readonly IMyTaskBackend? _taskBackend;
    private readonly AuthApiFacade? _auth;
    private readonly ProjectInvitationService? _invitationService;
    private readonly InvitationInboxService? _invitationInboxService;
    private ProfileControl? _profile;
    private AdminUserManagementControl? _admin;
    private AdminReportsControl? _reports;
    private CalendarControl? _calendar;
    private DashboardControl? _dashboard;
    private readonly CancellationTokenSource _authLifetime = new();
    private int _avatarGeneration;
    private int _globalSearchGeneration;
    private CancellationTokenSource? _globalSearchRequest;

    private readonly Dictionary<ContentStatus, KanbanColumnControl> _columns = new();
    private List<ContentBoardCard> _allCards = new();
    private KanbanCardControl? _selectedCardControl;
    private long? _selectedContentId;

    /// <summary>
    /// Constructor không tham số CHỈ để Visual Studio Designer mở được form. Không dùng khi chạy thật.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public BoardForm()
    {
        InitializeComponent();
        _workflowService = null!;
        _boardRepository = null!;
        _reviewQueueRepository = null!;
        _activityRepository = null!;
        _memberRepository = null!;
        _contentService = null!;
        _myTaskService = null!;
        _ideaService = null!;
    }

    public BoardForm(
        WorkflowService workflowService,
        IBoardRepository boardRepository,
        IReviewQueueRepository reviewQueueRepository,
        IActivityRepository activityRepository,
        IProjectMemberRepository memberRepository,
        ContentService contentService,
        MyTaskService myTaskService,
        ScriptService scriptService,
        IdeaService ideaService,
        AuthApiFacade? auth = null,
        IMetricBackend? metrics = null,
        ProjectInvitationService? invitationService = null,
        InvitationInboxService? invitationInboxService = null,
        ProjectApiFacade? projects = null,
        IMyTaskBackend? taskBackend = null,
        IIdeaBackend? ideaBackend = null,
        IBoardBackend? boardBackend = null,
        IContentDetailBackend? detailBackend = null,
        IAssignmentBackend? assignmentBackend = null,
        IReviewQueueBackend? reviewQueueBackend = null,
        INotificationBackend? notificationBackend = null)
    {
        InitializeComponent();
        _workflowService = workflowService;
        _boardRepository = boardRepository;
        _contentService = contentService;
        _myTaskService = myTaskService;
        _ideaService = ideaService;
        _reviewQueueRepository = reviewQueueRepository;
        _activityRepository = activityRepository;
        _memberRepository = memberRepository;
        _auth = auth;
        _invitationService = invitationService;
        _invitationInboxService = invitationInboxService;
        _taskBackend = taskBackend;
        _ideaBackend = ideaBackend;
        _boardBackend = boardBackend;
        _detailBackend = detailBackend;
        _assignmentBackend = assignmentBackend;
        _reviewQueueBackend = reviewQueueBackend;
        _notificationBackend = notificationBackend;
        _topHeaderControl.BellClicked += async (_, _) => await OnBellClickedAsync();
        _topHeaderControl.SetNotificationsEnabled(_notificationBackend is not null);
        InitializeProjects(projects);
        if (auth is not null)
        {
            _sidebarControl.SetAuthenticatedAccount(auth.Session.CurrentUser?.DisplayName ?? string.Empty,
                auth.Session.CurrentUser?.Email ?? string.Empty, null);
            _sidebarControl.SetSystemAdmin(auth.Session.CurrentUser?.IsSystemAdmin == true);
            Shown += async (_, _) => await RefreshAuthenticatedAvatarAsync();
            _sidebarControl.ProfileRequested += async (_, _) => await OpenProfileAsync();
            _sidebarControl.AdminWorkspaceRequested += async (_, _) => await OpenAdminAsync();
            _sidebarControl.AdminReportsRequested += async (_, _) => await OpenReportsAsync();
            _sidebarControl.CalendarRequested += (_, _) => OpenCalendar();
            _sidebarControl.SummaryRequested += (_, _) => OpenDashboard();
        }

        // TẠM-LOCAL: chế độ local không có auth nên menu Calendar/Summary chưa được gắn; gắn thêm để test.
        // Xóa khối này trước khi push (xem docs/tasks/scrum-49-loc-calendar/local-test-cleanup.md).
        if (auth is null)
        {
            _sidebarControl.CalendarRequested += (_, _) => OpenCalendar();
            _sidebarControl.SummaryRequested += (_, _) => OpenDashboard();
        }

        _pnlDrawerHost.Paint += PnlDrawerHost_Paint;
        BuildColumns();

        _contentDetailPanel.Initialize(_workflowService, _activityRepository, _memberRepository, _contentService, scriptService, metrics, loadLocalPlatforms: projects is null, detailBackend: detailBackend, assignmentBackend: assignmentBackend);
        _contentDetailPanel.ContentChanged += (_, _) => { if (_projects is not null) _reopenContentId = _selectedContentId; ReloadBoard(); _toast.ShowToast(this, "Cap nhat thanh cong!"); };
        _contentDetailPanel.CloseRequested += (_, _) => CloseDrawer();

        _topHeaderControl.NewContentClicked += (_, _) => OpenCreateContentDialog();
        _topHeaderControl.GlobalSearchRequested += async (_, args) => await SearchGloballyAsync(args.Query);
        _topHeaderControl.GlobalSearchResultSelected += async (_, args) => await OpenGlobalSearchResultAsync(args.Result);
        _topHeaderControl.SetGlobalSearchEnabled(auth is not null && projects is not null);

        _modulePageHeader.CreateContentClicked += (_, _) => OpenCreateContentDialog();
        _modulePageHeader.SearchTextChanged += (_, _) => ApplyFilters();
        _modulePageHeader.PlatformFilterChanged += (_, _) => ApplyFilters();
        _modulePageHeader.SprintFilterChanged += (_, _) => ApplyFilters();

        _sidebarControl.ReviewQueueRequested += (_, _) => OpenReviewQueue();
        _sidebarControl.ContentLibraryRequested += async (_, _) => await OpenContentLibraryAsync();
        _sidebarControl.BoardRequested += async (_, _) =>
        {
            if (!CanLeaveProjectPage()) return;
            if (_projects is not null && !await ValidateProjectAsync()) return;

            CloseProjectPage();
            CloseProfile();
            CloseAdmin();
            CloseReports();
            CloseCalendar();
            CloseDashboard();
            _sidebarControl.SetActiveMenu("Board");
            ReloadBoard();
        };
        _sidebarControl.MyTasksRequested += (_, _) => OpenMyTasks();
        _sidebarControl.IdeaBankRequested += (_, _) => OpenIdeaBank();
        if (projects is null) _sidebarControl.ProjectSettingsRequested += (_, _) => OpenProjectSettings();
        _sidebarControl.InvitationsRequested += (_, _) => OpenMyInvitations();
        UpdateProjectContext();
        if (!HasProjectContext) ReloadBoard();

        Load += (_, _) =>
        {
            Application.AddMessageFilter(this);
            ReloadBoard();
        };
    }

    // Handler đặt tên (Designer không đọc được lambda trong InitializeComponent)
    private void PnlDrawerHost_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.Neutral200, 1f);
        e.Graphics.DrawLine(pen, 0, 0, 0, _pnlDrawerHost.Height);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _authLifetime.Cancel();
        _globalSearchRequest?.Cancel();
        _globalSearchRequest?.Dispose();
        _globalSearchRequest = null;
        _authLifetime.Dispose();
        Application.RemoveMessageFilter(this);
        base.OnFormClosed(e);
    }

    // ==========================================================
    // Dựng cột Kanban (số lượng cột phụ thuộc PipelineStages nên dựng bằng code)
    // ==========================================================
    private void BuildColumns()
    {
        _flowColumns.SuspendLayout();
        foreach (var status in PipelineStages)
        {
            var col = new KanbanColumnControl(status) { Margin = new Padding(0, 0, 16, 0) };
            col.AddCardRequested += (_, st) => OpenCreateContentDialog(st);
            _columns[status] = col;
            _flowColumns.Controls.Add(col);
        }
        _flowColumns.ResumeLayout(true);

        // ClientSizeChanged (không phải SizeChanged) vì thanh cuộn ngang xuất hiện làm giảm ClientSize.Height
        _flowColumns.ClientSizeChanged += (_, _) => AdjustColumnHeights();
        AdjustColumnHeights();
    }

    /// <summary>Cột có thẻ: cao hết vùng board. Cột rỗng: thấp gọn như mẫu.</summary>
    private void AdjustColumnHeights()
    {
        int avail = _flowColumns.ClientSize.Height - _flowColumns.Padding.Vertical - 1;
        if (avail <= 0) return;

        _flowColumns.SuspendLayout();
        foreach (var col in _columns.Values)
        {
            int h = col.IsEmpty ? Math.Min(KanbanColumnControl.EmptyColumnHeight, avail) : avail;
            if (col.Height != h) col.Height = h;
        }
        _flowColumns.ResumeLayout(true);
    }

    // ==========================================================
    // Tải & lọc dữ liệu
    // ==========================================================
    private bool HasProjectContext => CurrentSession.CurrentProjectId > 0 &&
        (_auth is null || _auth.Session.IsAuthenticated);

    private void UpdateProjectContext()
    {
        _topHeaderControl.SetProjectContext(HasProjectContext ? CurrentSession.CurrentProjectName : null,
            HasProjectContext && _profile is null && (_projects is null || CanCreateContentViaApi));
        _sidebarControl.SetProjectName(HasProjectContext ? CurrentSession.CurrentProjectName : null);
        _sidebarControl.ProjectActionsEnabled = HasProjectContext;
        _sidebarControl.Invalidate();
    }

    /// <summary>Chế độ API: nút New Content chỉ bật khi tạo Content và lấy danh sách người nhận việc đã nối API.</summary>
    private bool CanCreateContentViaApi => _detailBackend is not null && _assignmentBackend is not null;

    private void ReloadBoard()
    {
        UpdateProjectContext();
        if (_projects is not null) { RenderServerWorkspace(); return; }
        if (!HasProjectContext)
        {
            _allCards.Clear();
            _selectedContentId = null;
            _selectedCardControl = null;
            _sidebarControl.BacklogCount = _sidebarControl.MyWorkCount = _sidebarControl.ReviewQueueCount = 0;
            _modulePageHeader.Visible = false;
            _flowColumns.Visible = false;
            _pnlDrawerHost.Visible = false;
            if (_pnlBoardArea.Controls["noProjectContext"] is null)
                _pnlBoardArea.Controls.Add(new Label
                {
                    Name = "noProjectContext",
                    Dock = DockStyle.Fill,
                    Text = "Chưa chọn dự án. Bạn có thể mở Hồ sơ từ avatar bên trái để quản lý tài khoản hoặc đăng xuất.",
                    ForeColor = UITheme.Neutral600,
                    BackColor = UITheme.Neutral50,
                    TextAlign = ContentAlignment.MiddleCenter
                });
            _dashboard?.RefreshData();
            return;
        }
        if (_pnlBoardArea.Controls["noProjectContext"] is Control emptyState)
        {
            _pnlBoardArea.Controls.Remove(emptyState);
            emptyState.Dispose();
        }
        _modulePageHeader.Visible = true;
        _flowColumns.Visible = true;
        _allCards = _boardRepository.GetBoardCards(CurrentSession.CurrentProjectId);
        int overdueCount = _allCards.Count(c => c.IsOverdue);
        _topHeaderControl.UpdateStats(_allCards.Count, overdueCount);
        var pendingReviews = _reviewQueueRepository.GetPendingReviews(CurrentSession.CurrentProjectId);
        _sidebarControl.ReviewQueueCount = pendingReviews.Count;
        _sidebarControl.MyWorkCount = _myTaskService.CountOpenTasks(CurrentSession.CurrentProjectId, CurrentSession.CurrentUserId);
        _sidebarControl.BacklogCount = _ideaService.CountBacklog(CurrentSession.CurrentProjectId, CurrentSession.CurrentUserId);
        _sidebarControl.Invalidate();
        ApplyFilters();
        _dashboard?.RefreshData();

        // Drawer đang mở: nạp lại dữ liệu mới (trạng thái/nút workflow) hoặc đóng nếu thẻ không còn.
        // Nếu đang có thay đổi chưa lưu thì KHÔNG ghi đè, để không mất dữ liệu người dùng đang nhập.
        if (_selectedContentId.HasValue && !_contentDetailPanel.HasUnsavedChanges)
        {
            var current = _allCards.FirstOrDefault(c => c.ContentId == _selectedContentId.Value);
            if (current != null) _contentDetailPanel.LoadContent(current);
            else CloseDrawer();
        }
    }

    private void ApplyFilters()
    {
        string searchText = _modulePageHeader.SearchText;
        string platformFilter = _modulePageHeader.SelectedPlatform;
        string sprintFilter = _modulePageHeader.SelectedSprint;
        var filtered = _allCards.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
            filtered = filtered.Where(c => c.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                           c.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(platformFilter) && platformFilter != "All")
            filtered = filtered.Where(c => c.Platforms.Any(p => p.Equals(platformFilter, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(sprintFilter) && sprintFilter != ModulePageHeaderControl.AllSprintsFilter)
            filtered = filtered.Where(c => c.Sprint.Equals(sprintFilter, StringComparison.OrdinalIgnoreCase));

        var list = filtered.ToList();
        _selectedCardControl = null;

        foreach (var status in PipelineStages)
        {
            var column = _columns[status];
            var columnCards = list.Where(c => c.Status == status).ToList();

            column.CardsFlow.SuspendLayout();
            column.ClearCards();
            column.SetCardCount(columnCards.Count);
            foreach (var card in columnCards)
            {
                var cardControl = new KanbanCardControl(card);
                cardControl.CardClicked += OnCardClicked;
                cardControl.MoveNextRequested += OnMoveNextRequested;
                cardControl.MovePrevRequested += OnMovePrevRequested;
                if (_selectedContentId == card.ContentId)
                {
                    cardControl.IsSelected = true;
                    _selectedCardControl = cardControl;
                }
                column.CardsFlow.Controls.Add(cardControl);
            }
            column.CardsFlow.ResumeLayout(true);
            column.LayoutCards();
        }

        AdjustColumnHeights();
    }

    // ==========================================================
    // Chọn thẻ / Drawer chi tiết
    // ==========================================================
    private void OnCardClicked(object? sender, ContentBoardCard card)
    {
        if (_projects is not null)
        {
            // Đã đăng nhập: drawer chi tiết chạy qua API khi có IContentDetailBackend.
            if (_detailBackend is not null && sender is KanbanCardControl apiCardCtrl)
                SelectCard(apiCardCtrl);
            else if (_boardBackend is not null)
                _toast.ShowToast(this, "Chi tiết Content chưa kết nối API");
            return;
        }
        if (sender is KanbanCardControl cardCtrl)
            SelectCard(cardCtrl);
    }

    private void SelectCard(KanbanCardControl cardCtrl)
    {
        if (!HasProjectContext || (_projects is not null && _detailBackend is null)) return;
        // Đang có thay đổi chưa lưu; chuyển thẻ hoặc tải lại cùng thẻ đều phải xác nhận trước.
        if (_selectedContentId.HasValue && _contentDetailPanel.HasUnsavedChanges)
        {
            if (!_contentDetailPanel.Clear()) return;
        }

        if (_selectedCardControl != null && !_selectedCardControl.IsDisposed)
            _selectedCardControl.IsSelected = false;

        _selectedCardControl = cardCtrl;
        _selectedCardControl.IsSelected = true;
        _selectedContentId = cardCtrl.Card.ContentId;

        _pnlDrawerHost.Visible = true;
        _contentDetailPanel.LoadContent(cardCtrl.Card);
    }

    private void CloseDrawer()
    {
        if (!_contentDetailPanel.Clear()) return; // Người dùng chọn giữ lại thay đổi chưa lưu → không đóng.

        if (_selectedCardControl != null && !_selectedCardControl.IsDisposed)
            _selectedCardControl.IsSelected = false;

        _selectedCardControl = null;
        _selectedContentId = null;
        _pnlDrawerHost.Visible = false;
        _calendar?.RefreshData(); // Vẽ lại chip (deadline có thể vừa đổi trong Drawer).
    }

    private KanbanCardControl? FindCardControl(long contentId)
    {
        foreach (var col in _columns.Values)
            foreach (Control c in col.CardsFlow.Controls)
                if (c is KanbanCardControl kc && kc.Card.ContentId == contentId)
                    return kc;
        return null;
    }

    // ==========================================================
    // Mũi tên ‹ › trên thẻ
    // ==========================================================
    private void OnMoveNextRequested(object? sender, ContentBoardCard card)
    {
        // Hoãn sang vòng lặp message kế tiếp: ReloadBoard sẽ dispose chính thẻ vừa bấm.
        BeginInvoke(new Action(() => TryMoveNext(card)));
    }

    private void OnMovePrevRequested(object? sender, ContentBoardCard card)
    {
        // Hoãn sang vòng lặp message kế tiếp: ReloadBoard sẽ dispose chính thẻ vừa bấm.
        BeginInvoke(new Action(() => TryMovePrev(card)));
    }

    /// <summary>
    /// Mũi tên ‹ trên thẻ: chỉ Review mới có bước lùi hợp lệ (từ chối → Editing).
    /// Các giai đoạn khác workflow không cho lùi nên chỉ báo cho người dùng.
    /// </summary>
    private void TryMovePrev(ContentBoardCard card)
    {
        if (!HasProjectContext) return;

        if (card.Status != ContentStatus.Review)
        {
            _toast.ShowToast(this, "Quy trình không cho phép lùi giai đoạn");
            return;
        }

        if (_projects is not null && _detailBackend is null)
        {
            _toast.ShowToast(this, "Chuyển trạng thái chưa kết nối API");
            return;
        }

        using var dialog = new ReviewDecisionDialog(
            $"{card.Code} — {card.Title}",
            "Từ chối sẽ chuyển Content về Editing để Creator sửa lại.");

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            if (_projects is not null)
                _detailBackend!.DecideReview(card.ContentId, dialog.Approved, dialog.Feedback);
            else if (dialog.Approved)
                _workflowService.ApproveReview(card.ContentId, CurrentSession.CurrentUserId, dialog.Feedback);
            else
                _workflowService.RejectReview(card.ContentId, CurrentSession.CurrentUserId, dialog.Feedback);

            ReloadBoard();
            _toast.ShowToast(this, "Cap nhat trang thai thanh cong!");
        }
        catch (InvalidWorkflowTransitionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (ContentValidationException ex) // chế độ API: lỗi nghiệp vụ/mạng từ Backend
        {
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException ex) // chế độ API: 404 endpoint/dữ liệu
        {
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void TryMoveNext(ContentBoardCard card)
    {
        if (!HasProjectContext) return;
        try
        {
            // Chế độ API: quyền và luồng trạng thái do Backend kiểm tra, client chỉ gửi yêu cầu.
            if (_projects is not null)
            {
                if (_detailBackend is null)
                {
                    _toast.ShowToast(this, "Chuyển trạng thái chưa kết nối API");
                    return;
                }

                switch (card.Status)
                {
                    case ContentStatus.Editing:
                        _detailBackend.SubmitForReview(card.ContentId);
                        break;

                    case ContentStatus.Review:
                        // Approve/Reject cần quyết định + feedback → mở drawer để xử lý
                        var reviewCtrl = FindCardControl(card.ContentId);
                        if (reviewCtrl != null) SelectCard(reviewCtrl);
                        return;

                    default:
                        var apiNext = _workflowService.GetNextSimpleStatus(card.Status);
                        if (!apiNext.HasValue) return;
                        _detailBackend.ChangeStatus(card.ContentId, apiNext.Value);
                        break;
                }

                ReloadBoard();
                _toast.ShowToast(this, "Cap nhat trang thai thanh cong!");
                return;
            }

            switch (card.Status)
            {
                case ContentStatus.Editing:
                    _workflowService.SubmitForReview(card.ContentId, CurrentSession.CurrentUserId);
                    break;

                case ContentStatus.Review:
                    // Approve/Reject cần quyết định + feedback → mở drawer để xử lý
                    var ctrl = FindCardControl(card.ContentId);
                    if (ctrl != null) SelectCard(ctrl);
                    return;

                default:
                    var next = _workflowService.GetNextSimpleStatus(card.Status);
                    if (!next.HasValue) return;
                    _workflowService.ChangeStatus(card.ContentId, next.Value, CurrentSession.CurrentUserId);
                    break;
            }

            ReloadBoard();
            _toast.ShowToast(this, "Cap nhat trang thai thanh cong!");
        }
        catch (InvalidWorkflowTransitionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (ContentValidationException ex) // chế độ API: lỗi nghiệp vụ/mạng từ Backend
        {
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException ex) // chế độ API: 404 endpoint/dữ liệu
        {
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ==========================================================
    // Dialogs
    // ==========================================================
    private void OpenCreateContentDialog(ContentStatus initialStatus = ContentStatus.Idea)
    {
        // New Content is available over Project Settings; its modal opens above the settings page.
        // Other workspaces and Profile keep their existing navigation/unsaved-state behavior.
        if (!HasProjectContext || _profile is not null || _admin is not null || _reports is not null || _calendar is not null || _dashboard is not null) return;
        bool viaApi = _projects is not null;
        if (viaApi && (_detailBackend is null || _assignmentBackend is null)) { ShowUnavailableModule("Tạo Content"); return; }

        List<ProjectMemberInfo> members;
        List<string> platforms;
        string apiOperation = $"lấy danh sách Creator được giao (GET /api/projects/{CurrentSession.CurrentProjectId}/assignable-members)";
        try
        {
            // Chế độ API: danh sách người nhận việc và platform lấy từ API (quyền do Backend quyết định).
            members = viaApi
                ? _assignmentBackend!.GetAssignableMembers(CurrentSession.CurrentProjectId)
                : _contentService.GetAssignableMembers(CurrentSession.CurrentProjectId, CurrentSession.CurrentUserId);
            apiOperation = "lấy danh sách nền tảng (GET /api/platforms)";
            platforms = viaApi ? _detailBackend!.GetAvailablePlatforms() : _contentService.GetAvailablePlatforms();
        }
        catch (ContentValidationException ex)
        {
            MessageBox.Show(this, ex.Message, "Không mở được hộp thoại tạo nội dung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (InvalidOperationException ex)
        {
            if (viaApi) ShowContentApiNotFound(apiOperation, ex);
            else MessageBox.Show(this, ex.Message, "Không mở được hộp thoại tạo nội dung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new CreateContentDialog(members, platforms, initialStatus);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            long newId = viaApi
                ? _detailBackend!.Create(CurrentSession.CurrentProjectId, initialStatus, dlg.Draft)
                : _contentService.Create(CurrentSession.CurrentProjectId, initialStatus, dlg.Draft, CurrentSession.CurrentUserId);
            ReloadBoard();
            _toast.ShowToast(this, $"Da tao noi dung CNT-{newId:000} thanh cong!");
        }
        catch (ContentValidationException ex)
        {
            MessageBox.Show(this, ex.Message, "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException ex)
        {
            if (viaApi)
                ShowContentApiNotFound($"tạo Content (POST /api/projects/{CurrentSession.CurrentProjectId}/contents)", ex);
            else
                MessageBox.Show(this, ex.Message, "Không tạo được Content", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowContentApiNotFound(string operation, InvalidOperationException error)
    {
        MessageBox.Show(this,
            $"Không thể {operation}. API trả về HTTP 404.\r\n\r\n" +
            "Board cũng đang nhận HTTP 404, nên API ở Api:BaseUrl có thể chưa được cập nhật cùng phiên bản với ứng dụng. " +
            "Hãy kiểm tra appsettings.Local.json hoặc Api:BaseUrl, rồi chạy/redeploy CreatorFlow.Api mới nhất.\r\n\r\n" +
            error.Message,
            "CreatorFlow.Api chưa đồng bộ",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>Mở trang My Tasks trong vùng nội dung chính.</summary>
    private async void OpenMyTasks()
    {
        if (!HasProjectContext || !CanLeaveProjectPage()) return;
        if (_projects is not null && !await ValidateProjectAsync()) return;
        if (_projects is not null && _taskBackend is null) { ShowUnavailableModule("My Tasks"); return; }

        long generation = _projects?.Context.Generation ?? 0;
        long authGeneration = _auth?.Session.Generation ?? 0;
        MyTasksControl page = _projects is null
            ? new MyTasksControl(_myTaskService)
            : new MyTasksControl(_taskBackend!, () => !IsDisposed && _auth?.Session.IsAuthenticated == true &&
                authGeneration == _auth.Session.Generation && generation == _projects.Context.Generation);
        page.Dock = DockStyle.Fill;
        page.ContentOpenRequested += OpenMyTaskContent;
        page.ActiveTaskCountChanged += count =>
        {
            _sidebarControl.MyWorkCount = count;
            _sidebarControl.Invalidate();
        };

        ShowProjectPage(page, "My Tasks");
        _sidebarControl.SetActiveMenu("My Tasks");
    }

    /// <summary>Mở trang Idea Bank của Project hiện hành ngay trong vùng nội dung chính.</summary>
    private async void OpenIdeaBank() => await OpenIdeaBankPageAsync();

    private async Task<IdeaBankControl?> OpenIdeaBankPageAsync(long? focusIdeaId = null)
    {
        if (!HasProjectContext || !CanLeaveProjectPage()) return null;
        if (_projects is not null && !await ValidateProjectAsync()) return null;
        if (!HasProjectContext) return null;

        IIdeaBackend? backend = _projects is null ? _ideaService : _ideaBackend;
        if (backend is null)
        {
            ShowUnavailableModule("Idea Bank");
            return null;
        }

        long projectId = CurrentSession.CurrentProjectId;
        var page = new IdeaBankControl(backend)
        {
            Dock = DockStyle.Fill,
        };
        page.IdeasChanged += (_, _) => RefreshBacklogCount();
        page.CloseRequested += (_, _) => CloseIdeaBankPage(page);
        ShowProjectPage(page, "Backlog");
        _sidebarControl.SetActiveMenu("Backlog");
        if (focusIdeaId is long ideaId && !page.FocusIdea(ideaId))
            _toast.ShowToast(this, "Không còn tìm thấy ý tưởng này trong Project.");
        RefreshBacklogCount();

        void RefreshBacklogCount()
        {
            try
            {
                _sidebarControl.BacklogCount = backend.CountBacklog(projectId, CurrentSession.CurrentUserId);
            }
            catch (Exception error) when (error is IdeaValidationException or UnauthorizedWorkflowActionException)
            {
                _sidebarControl.BacklogCount = 0;
            }
            _sidebarControl.Invalidate();
        }
        return page;
    }

    private void CloseIdeaBankPage(IdeaBankControl page)
    {
        if (!ReferenceEquals(_projectPage, page)) return;

        CloseProjectPage();
        _pnlBoardArea.Visible = true;
        _modulePageHeader.Visible = true;
        _flowColumns.Visible = true;
        _pnlDrawerHost.Visible = false;
        if (_pnlBoardArea.Controls["serverProjectState"] is Label serverState)
            serverState.Visible = false;
        _topHeaderControl.SetAuthenticatedPage("Board");
        _sidebarControl.SetActiveMenu("Board");
        ReloadBoard();
    }

    /// <summary>Chạy một hộp thoại mở từ sidebar: sáng mục <paramref name="menu"/> trong lúc mở, đóng xong trả highlight về mục trước đó.</summary>
    private void ShowWithActiveMenu(string menu, Action show)
    {
        string? previous = _sidebarControl.ActiveMenu;
        _sidebarControl.SetActiveMenu(menu);
        try
        {
            show();
        }
        finally
        {
            _sidebarControl.SetActiveMenu(previous ?? "Board");
        }
    }

    /// <summary>Mở hộp thoại Mời thành viên (SCRUM-26) của Project hiện hành. Quyền Owner do API quyết định.</summary>
    private void OpenProjectSettings()
    {
        if (!HasProjectContext) return;
        if (!CanLeaveProjectPage()) return;
        CloseProfile();
        if (_invitationService is null)
        {
            _toast.ShowToast(this, "Chua ket noi API de moi thanh vien");
            return;
        }

        using var dialog = new InviteMemberDialog(_invitationService, CurrentSession.CurrentProjectId, CurrentSession.CurrentProjectName);
        ShowWithActiveMenu("Project Settings", () => dialog.ShowDialog(this));
    }

    /// <summary>
    /// Mở hộp thoại Lời mời của tôi: xem lời mời gửi tới tài khoản đang đăng nhập và chấp nhận / từ chối.
    /// Không cần đang ở trong Project nào (người mới được mời thường chưa có Project). Hạn dùng, trạng thái và việc tạo thành viên do API quyết định.
    /// </summary>
    private void OpenMyInvitations()
    {
        if (!CanLeaveProjectPage()) return;
        CloseProfile();
        if (_invitationInboxService is null)
        {
            _toast.ShowToast(this, "Chưa kết nối API để xem lời mời");
            return;
        }

        using var dialog = new MyInvitationsDialog(_invitationInboxService);
        ShowWithActiveMenu("Invitations", () => dialog.ShowDialog(this));
    }

    /// <summary>Mở drawer chi tiết cho Content theo Id (kể cả khi thẻ đang bị ẩn bởi bộ lọc tìm kiếm/platform).</summary>
    private void OpenContentById(long contentId)
    {
        if (!HasProjectContext) return;
        if (_projects is not null) { ShowUnavailableModule("Chi tiết Content"); return; }
        var card = _allCards.FirstOrDefault(c => c.ContentId == contentId);
        if (card is null)
        {
            _toast.ShowToast(this, "Khong tim thay Content tren Board");
            return;
        }

        // Đang có thay đổi chưa lưu; chuyển thẻ hoặc tải lại cùng thẻ đều phải xác nhận trước.
        if (_selectedContentId.HasValue && _contentDetailPanel.HasUnsavedChanges)
        {
            if (!_contentDetailPanel.Clear()) return;
        }

        if (_selectedCardControl != null && !_selectedCardControl.IsDisposed)
            _selectedCardControl.IsSelected = false;

        _selectedCardControl = FindCardControl(contentId);
        if (_selectedCardControl != null) _selectedCardControl.IsSelected = true;
        _selectedContentId = contentId;

        _pnlDrawerHost.Visible = true;
        _contentDetailPanel.LoadContent(card);
    }

    private void OpenReviewQueue()
    {
        if (!HasProjectContext) return;
        CloseProfile();
        CloseAdmin();
        CloseReports();
        CloseCalendar();
        CloseDashboard();
        using var queueForm = _reviewQueueBackend is not null
            ? new Review.ReviewQueueForm(_reviewQueueBackend)
            : new Review.ReviewQueueForm(_reviewQueueRepository, _workflowService);
        ShowWithActiveMenu("Review Queue", () => queueForm.ShowDialog(this));
        ReloadBoard();
    }

    private async Task OnBellClickedAsync()
    {
        if (_notificationBackend is null)
        {
            _toast.ShowToast(this, "Notifications need a CreatorFlow API connection.");
            return;
        }
        try
        {
            var result = _notificationBackend.GetNotifications(unreadOnly: false);
            _topHeaderControl.SetUnreadNotificationCount(result.UnreadCount);
            if (!result.SchemaReady)
                _toast.ShowToast(this, result.SchemaMessage ?? "Notifications require database migration 09_notifications.sql.");

            _notificationPanel ??= new NotificationPanelControl();
            _notificationPanel.SetItems(result.Items, result.SchemaReady, result.SchemaMessage);
            _notificationPanel.ItemClicked -= NotificationPanel_ItemClicked;
            _notificationPanel.ItemClicked += NotificationPanel_ItemClicked;
            _notificationPanel.MarkAllRequested -= NotificationPanel_MarkAllRequested;
            _notificationPanel.MarkAllRequested += NotificationPanel_MarkAllRequested;

            // Neo panel dưới icon Bell.
            Point bellScreen = _topHeaderControl.PointToScreen(_topHeaderControl.BellBounds.Location);
            Point panelLocation = PointToClient(new Point(bellScreen.X - 40, bellScreen.Y + _topHeaderControl.BellBounds.Height + 4));
            if (panelLocation.X + _notificationPanel.Width > ClientSize.Width)
                panelLocation.X = ClientSize.Width - _notificationPanel.Width - 8;
            if (panelLocation.X < 8) panelLocation.X = 8;
            _notificationPanel.Location = panelLocation;

            if (!_notificationPanel.Visible || _notificationPanel.Parent != this)
            {
                if (_notificationPanel.Parent != this) Controls.Add(_notificationPanel);
                _notificationPanel.BringToFront();
                _notificationPanel.Visible = true;
            }
            else
            {
                _notificationPanel.Visible = false;
            }
        }
        catch (Exception ex)
        {
            string message = ex.Message;
            if (message.Contains("404") || message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
                message = "API chua ho tro thong bao (can deploy phien ban moi cua CreatorFlow.Api).";
            _toast.ShowToast(this, message);
        }
    }

    private void NotificationPanel_ItemClicked(object? sender, long notificationId)
    {
        if (_notificationBackend is null) return;
        try
        {
            _notificationBackend.MarkRead(notificationId);
            var result = _notificationBackend.GetNotifications(unreadOnly: false);
            _notificationPanel?.SetItems(result.Items);
            _topHeaderControl.SetUnreadNotificationCount(result.UnreadCount);
        }
        catch (Exception ex)
        {
            string message = ex.Message;
            if (message.Contains("404") || message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
                message = "API chua ho tro thong bao (can deploy phien ban moi cua CreatorFlow.Api).";
            _toast.ShowToast(this, message);
        }
    }

    private void NotificationPanel_MarkAllRequested(object? sender, EventArgs e)
    {
        if (_notificationBackend is null) return;
        try
        {
            _notificationBackend.MarkAllRead();
            var result = _notificationBackend.GetNotifications(unreadOnly: false);
            _notificationPanel?.SetItems(result.Items);
            _topHeaderControl.SetUnreadNotificationCount(result.UnreadCount);
        }
        catch (Exception ex)
        {
            string message = ex.Message;
            if (message.Contains("404") || message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
                message = "API chua ho tro thong bao (can deploy phien ban moi cua CreatorFlow.Api).";
            _toast.ShowToast(this, message);
        }
    }

    private async Task OpenProfileAsync()
    {
        if (_auth is null || !_auth.Session.IsAuthenticated || _profile is not null || !CanLeaveProjectPage()) return;
        CloseProjectPage();
        CloseAdmin();
        CloseReports();
        CloseCalendar();
        CloseDashboard();
        var profile = new ProfileControl(_auth) { Dock = DockStyle.Fill };
        _profile = profile;
        UpdateProjectContext();
        profile.ProfileSaved += async saved =>
        {
            CurrentSession.CurrentUserName = saved.DisplayName;
            await RefreshAuthenticatedAvatarAsync();
        };
        profile.PasswordChanged += (_, _) => _auth.Logout();
        profile.LogoutRequested += (_, _) => _auth.Logout();
        _pnlDrawerHost.Visible = false;
        _pnlBoardArea.Visible = false;
        _pnlMain.Controls.Add(profile);
        profile.BringToFront();
        _topHeaderControl.SetAuthenticatedPage("Hồ sơ cá nhân");
        await profile.LoadProfileAsync();
    }

    private void CloseProfile()
    {
        if (_profile is null) return;
        ProfileControl profile = _profile;
        _profile = null;
        _pnlMain.Controls.Remove(profile);
        profile.Dispose();
        _pnlBoardArea.Visible = true;
        _topHeaderControl.SetAuthenticatedPage("Board");
        UpdateProjectContext();
    }

    private async Task OpenAdminAsync()
    {
        if (_auth is null || !_auth.Session.IsAuthenticated || _admin is not null) return;
        if (_auth.Session.CurrentUser?.IsSystemAdmin != true || !CanLeaveProjectPage()) return;
        CloseProjectPage();
        CloseProfile();
        CloseReports();
        CloseCalendar();
        CloseDashboard();
        var admin = new AdminUserManagementControl(_auth, _toast) { Dock = DockStyle.Fill };
        _admin = admin;
        _pnlDrawerHost.Visible = false;
        _pnlBoardArea.Visible = false;
        _pnlMain.Controls.Add(admin);
        admin.BringToFront();
        _sidebarControl.SetActiveMenu("Quản trị người dùng");
        _topHeaderControl.SetAuthenticatedPage("Quản trị hệ thống");
        await admin.LoadUsersAsync();
    }

    private void CloseAdmin()
    {
        if (_admin is null) return;
        AdminUserManagementControl admin = _admin;
        _admin = null;
        _pnlMain.Controls.Remove(admin);
        admin.Dispose();
        _sidebarControl.SetActiveMenu("Board");
        _pnlBoardArea.Visible = true;
        _topHeaderControl.SetAuthenticatedPage("Board");
        UpdateProjectContext();
    }

    private async Task OpenReportsAsync()
    {
        if (_auth is null || !_auth.Session.IsAuthenticated || _reports is not null) return;
        if (_auth.Session.CurrentUser?.IsSystemAdmin != true || !CanLeaveProjectPage()) return;
        CloseProjectPage();
        CloseProfile();
        CloseAdmin();
        CloseCalendar();
        CloseDashboard();
        var reports = new AdminReportsControl(_auth, _toast) { Dock = DockStyle.Fill };
        _reports = reports;
        _pnlDrawerHost.Visible = false;
        _pnlBoardArea.Visible = false;
        _pnlMain.Controls.Add(reports);
        reports.BringToFront();
        _sidebarControl.SetActiveMenu("Báo cáo vi phạm");
        _topHeaderControl.SetAuthenticatedPage("Báo cáo vi phạm");
        await reports.LoadReportsAsync();
    }

    private void CloseReports()
    {
        if (_reports is null) return;
        AdminReportsControl reports = _reports;
        _reports = null;
        _pnlMain.Controls.Remove(reports);
        reports.Dispose();
        _sidebarControl.SetActiveMenu("Board");
        _pnlBoardArea.Visible = true;
        _topHeaderControl.SetAuthenticatedPage("Board");
        UpdateProjectContext();
    }

    private void OpenCalendar()
    {
        if (_calendar is not null || !CanLeaveProjectPage()) return;
        CloseProjectPage();
        CloseProfile();
        CloseAdmin();
        CloseReports();
        CloseDashboard();
        var calendar = new CalendarControl { Dock = DockStyle.Fill };
        calendar.CardProvider = () => HasProjectContext ? _allCards : null;
        if (_projects is not null && _boardBackend is null) calendar.UnavailableReason = "Lịch chưa có nguồn Content từ server cho Project này.";
        calendar.ContentOpened += OpenContentById;
        _calendar = calendar;
        _pnlDrawerHost.Visible = false;
        _pnlBoardArea.Visible = false;
        _pnlMain.Controls.Add(calendar);
        calendar.BringToFront();
        _sidebarControl.SetActiveMenu("Calendar");
        _topHeaderControl.SetAuthenticatedPage("Lịch Content");
        calendar.RefreshData();
    }

    private void CloseCalendar()
    {
        if (_calendar is null) return;
        CalendarControl calendar = _calendar;
        _calendar = null;
        _pnlMain.Controls.Remove(calendar);
        calendar.Dispose();
        _sidebarControl.SetActiveMenu("Board");
        _pnlBoardArea.Visible = true;
        _topHeaderControl.SetAuthenticatedPage("Board");
        UpdateProjectContext();
    }

    private void OpenDashboard()
    {
        if (_dashboard is not null || !CanLeaveProjectPage()) return;
        CloseProjectPage();
        CloseProfile();
        CloseAdmin();
        CloseReports();
        CloseCalendar();
        var dashboard = new DashboardControl { Dock = DockStyle.Fill };
        dashboard.CardProvider = () => HasProjectContext ? _allCards : null;
        if (_projects is not null && _boardBackend is null)
            dashboard.UnavailableReason = "Tổng quan chưa có nguồn Content từ server cho Project này.";
        _dashboard = dashboard;
        _pnlDrawerHost.Visible = false;
        _pnlBoardArea.Visible = false;
        _pnlMain.Controls.Add(dashboard);
        dashboard.BringToFront();
        _sidebarControl.SetActiveMenu("Summary");
        _topHeaderControl.SetAuthenticatedPage("Tổng quan Project");

        // Server: nạp lại card của Project hiện hành cho chắc (số liệu khớp DB), hiện trạng thái đang tải.
        // Local: _allCards đã có sẵn sau ReloadBoard nên chỉ việc vẽ.
        if (_projects is not null && _boardBackend is not null)
        {
            dashboard.BeginLoading();
            _ = LoadServerBoardAsync();
        }
        else
        {
            dashboard.RefreshData();
        }
    }

    private void CloseDashboard()
    {
        if (_dashboard is null) return;
        DashboardControl dashboard = _dashboard;
        _dashboard = null;
        _pnlMain.Controls.Remove(dashboard);
        dashboard.Dispose();
        _sidebarControl.SetActiveMenu("Board");
        _pnlBoardArea.Visible = true;
        _topHeaderControl.SetAuthenticatedPage("Board");
        UpdateProjectContext();
    }

    private async Task RefreshAuthenticatedAvatarAsync()
    {
        if (_auth is null || !_auth.Session.IsAuthenticated || IsDisposed || _authLifetime.IsCancellationRequested) return;
        int generation = ++_avatarGeneration;
        long sessionGeneration = _auth.Session.Generation;
        Image? avatar = null;
        try
        {
            var stored = await _auth.GetCurrentAvatarAsync(_authLifetime.Token);
            if (IsDisposed || _authLifetime.IsCancellationRequested || !_auth.Session.IsAuthenticated) return;
            if (stored is not null) avatar = CreatorFlow.Helpers.AvatarImageLoader.Decode(stored.ImageData);
            else
            {
                var profile = await _auth.GetCurrentProfileAsync(_authLifetime.Token);
                avatar = await CreatorFlow.Helpers.AvatarImageLoader.LoadAsync(profile.Profile?.AvatarUrl, _authLifetime.Token);
            }
            if (IsDisposed || _authLifetime.IsCancellationRequested || generation != _avatarGeneration ||
                !_auth.Session.IsAuthenticated || _auth.Session.Generation != sessionGeneration)
            { return; }
            var currentUser = _auth.Session.CurrentUser!;
            _sidebarControl.SetAuthenticatedAccount(currentUser.DisplayName, currentUser.Email, avatar);
            avatar = null; // Sidebar now owns the image.
        }
        catch (OperationCanceledException) when (_authLifetime.IsCancellationRequested) { }
        finally { avatar?.Dispose(); }
    }

    // ==========================================================
    // Cuộn chuột theo vị trí con trỏ (WinForms mặc định chỉ cuộn control đang focus)
    //  - Lăn chuột trên cột: cuộn dọc danh sách thẻ
    //  - Shift + lăn (hoặc lăn ngoài cột): cuộn ngang board
    // ==========================================================
    public bool PreFilterMessage(ref Message m)
    {
        const int WM_LBUTTONDOWN = 0x0201;
        if (m.Msg == WM_LBUTTONDOWN && _notificationPanel is { Visible: true })
        {
            var cursor = Cursor.Position;
            var panelRect = new Rectangle(_notificationPanel.PointToScreen(Point.Empty), _notificationPanel.Size);
            var bellRect = new Rectangle(_topHeaderControl.PointToScreen(_topHeaderControl.BellBounds.Location), _topHeaderControl.BellBounds.Size);
            if (!panelRect.Contains(cursor) && !bellRect.Contains(cursor))
                _notificationPanel.Visible = false;
        }

        if (m.Msg != WM_MOUSEWHEEL || !Visible || !ReferenceEquals(ActiveForm, this))
            return false;

        long wParam = m.WParam.ToInt64();
        int delta = unchecked((short)(wParam >> 16));
        bool shift = (wParam & MK_SHIFT) != 0;

        var hit = FindControlAt(Cursor.Position);
        for (Control? p = hit; p != null; p = p.Parent)
        {
            if (ReferenceEquals(p, _sidebarControl)) { _sidebarControl.ScrollNavigation(delta); return true; }
            if (ReferenceEquals(p, _pnlDrawerHost))
                return false; // Để drawer tự xử lý

            if (p is FlowLayoutPanel flow && flow.Parent is KanbanColumnControl)
            {
                if (!shift && flow.VerticalScroll.Visible)
                {
                    ScrollBy(flow, horizontal: false, delta);
                    return true;
                }
                continue; // Không cần cuộn dọc → thử cuộn ngang board
            }

            if (ReferenceEquals(p, _flowColumns))
            {
                ScrollBy(_flowColumns, horizontal: true, delta);
                return true;
            }
        }
        return false;
    }

    private Control? FindControlAt(Point screenPoint)
    {
        Control current = this;
        while (true)
        {
            var child = current.GetChildAtPoint(current.PointToClient(screenPoint), GetChildAtPointSkip.Invisible);
            if (child == null) return ReferenceEquals(current, this) ? null : current;
            current = child;
        }
    }

    private static void ScrollBy(ScrollableControl target, bool horizontal, int wheelDelta)
    {
        int step = 60 * wheelDelta / 120;              // ~60px mỗi nấc
        var pos = target.AutoScrollPosition;           // giá trị trả về là số âm/0
        target.AutoScrollPosition = horizontal
            ? new Point(-pos.X - step, -pos.Y)
            : new Point(-pos.X, -pos.Y - step);
    }
}
