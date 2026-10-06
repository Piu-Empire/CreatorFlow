using CreatorFlow.Models.Enums;
using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
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
    }

    public BoardForm(
        WorkflowService workflowService,
        IBoardRepository boardRepository,
        IReviewQueueRepository reviewQueueRepository,
        IActivityRepository activityRepository,
        IProjectMemberRepository memberRepository,
        ContentService contentService)
    {
        InitializeComponent();
        _workflowService = workflowService;
        _boardRepository = boardRepository;
        _contentService = contentService;
        _reviewQueueRepository = reviewQueueRepository;
        _activityRepository = activityRepository;
        _memberRepository = memberRepository;

        _pnlDrawerHost.Paint += PnlDrawerHost_Paint;
        BuildColumns();

        _contentDetailPanel.Initialize(_workflowService, _activityRepository, _memberRepository, _contentService);
        _contentDetailPanel.ContentChanged += (_, _) => { ReloadBoard(); _toast.ShowToast(this, "Cap nhat thanh cong!"); };
        _contentDetailPanel.CloseRequested += (_, _) => CloseDrawer();

        _topHeaderControl.NewContentClicked += (_, _) => OpenCreateContentDialog();

        _modulePageHeader.RefreshClicked += (_, _) => { ReloadBoard(); _toast.ShowToast(this, "Da lam moi du lieu Board"); };
        _modulePageHeader.ReviewQueueClicked += (_, _) => OpenReviewQueue();
        _modulePageHeader.CreateContentClicked += (_, _) => OpenCreateContentDialog();
        _modulePageHeader.SearchTextChanged += (_, _) => ApplyFilters();
        _modulePageHeader.PlatformFilterChanged += (_, _) => ApplyFilters();

        _sidebarControl.ReviewQueueRequested += (_, _) => OpenReviewQueue();
        _sidebarControl.BoardRequested += (_, _) => ReloadBoard();

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
    private void ReloadBoard()
    {
        _allCards = _boardRepository.GetBoardCards(CurrentSession.CurrentProjectId);
        int overdueCount = _allCards.Count(c => c.IsOverdue);
        _topHeaderControl.UpdateStats(_allCards.Count, overdueCount);
        var pendingReviews = _reviewQueueRepository.GetPendingReviews(CurrentSession.CurrentProjectId);
        _sidebarControl.ReviewQueueCount = pendingReviews.Count;
        _sidebarControl.Invalidate();
        ApplyFilters();

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
        var filtered = _allCards.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
            filtered = filtered.Where(c => c.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                           c.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(platformFilter) && platformFilter != "All")
            filtered = filtered.Where(c => c.Platforms.Any(p => p.Equals(platformFilter, StringComparison.OrdinalIgnoreCase)));

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
        if (sender is KanbanCardControl cardCtrl)
            SelectCard(cardCtrl);
    }

    private void SelectCard(KanbanCardControl cardCtrl)
    {
        // Đang mở thẻ khác và có thay đổi chưa lưu → hỏi trước khi chuyển (Clear() tự hiện hộp thoại xác nhận).
        if (_selectedContentId.HasValue && _selectedContentId != cardCtrl.Card.ContentId && _contentDetailPanel.HasUnsavedChanges)
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
        _toast.ShowToast(this, "Workflow khong cho phep lui giai doan");
    }

    private void TryMoveNext(ContentBoardCard card)
    {
        try
        {
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
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ==========================================================
    // Dialogs
    // ==========================================================
    private void OpenCreateContentDialog(ContentStatus initialStatus = ContentStatus.Idea)
    {
        var members = _contentService.GetMembers(CurrentSession.CurrentProjectId);
        using var dlg = new CreateContentDialog(members, _contentService.GetAvailablePlatforms(), initialStatus);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            long newId = _contentService.Create(
                CurrentSession.CurrentProjectId, initialStatus, dlg.Draft, CurrentSession.CurrentUserId);
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
    }

    private void OpenReviewQueue()
    {
        using var queueForm = new Review.ReviewQueueForm(_reviewQueueRepository, _workflowService);
        queueForm.ShowDialog(this);
        ReloadBoard();
    }

    // ==========================================================
    // Cuộn chuột theo vị trí con trỏ (WinForms mặc định chỉ cuộn control đang focus)
    //  - Lăn chuột trên cột: cuộn dọc danh sách thẻ
    //  - Shift + lăn (hoặc lăn ngoài cột): cuộn ngang board
    // ==========================================================
    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != WM_MOUSEWHEEL || !Visible || !ReferenceEquals(ActiveForm, this))
            return false;

        long wParam = m.WParam.ToInt64();
        int delta = unchecked((short)(wParam >> 16));
        bool shift = (wParam & MK_SHIFT) != 0;

        var hit = FindControlAt(Cursor.Position);
        for (Control? p = hit; p != null; p = p.Parent)
        {
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