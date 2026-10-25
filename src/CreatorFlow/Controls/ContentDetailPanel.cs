using CreatorFlow.Models.Enums;
using CreatorFlow.ApiClients;
using System.Drawing.Drawing2D;
using CreatorFlow.Forms.Board;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Drawer chi tiết bên phải (theo mẫu AI Studio):
/// Header chip [Mã] [Nền tảng] [Sprint] + nút đóng → Tab (Overview / Script / Checklist / Activity)
/// → nội dung → Footer [mã] + "Done &amp; Close".
/// Toàn bộ logic Workflow (Submit Review / Approve / Reject / chuyển bước) giữ nguyên như cũ.
/// </summary>
public partial class ContentDetailPanel : UserControl
{
    private const int PadX = 24;

    private WorkflowService? _workflowService;
    private IActivityRepository? _activityRepo;
    private IProjectMemberRepository? _memberRepo;
    private ContentService? _contentService;
    private IContentDetailBackend? _detailBackend;       // chế độ API: chi tiết/sửa/script/activity/workflow chạy qua CreatorFlow.Api
    private IAssignmentBackend? _assignmentBackend;      // chế độ API: giao việc
    private bool _apiPlatformsLoaded;
    private readonly Dictionary<long, ProjectRole?> _roleCache = new(); // chế độ API: 1 lần gọi vai trò cho mỗi lần mở Content (xoá ở đầu LoadContent)

    private ContentBoardCard? _current;
    private List<ProjectMemberInfo> _members = new();
    private int _selectedTabIndex = 0; // 0: Overview, 1: Script, 2: Checklist, 3: Activity, 4: References, 5: Metrics
    private bool _isDirty;
    private bool _deadlineEdited; // true khi người dùng thật sự đổi ô Deadline (tránh vô tình ghi deadline mặc định khi chỉ sửa tiêu đề)
    private bool _isLoading; // true trong lúc LoadContent() đang gán giá trị → bỏ qua sự kiện TextChanged/SelectedIndexChanged
    private bool _detailLoaded; // false nếu đọc chi tiết (script, loại, ngày đăng) từ DB thất bại → chặn Lưu để không ghi đè dữ liệu bằng giá trị rỗng

    public event EventHandler? ContentChanged;
    public event EventHandler? CloseRequested;

    // Khung chính

    // Tab 1: Overview
    private readonly List<(Control Ctl, int GapAfter)> _overviewRows = new();

    // Index: 0 Pipeline Stage (chỉ đọc) · 1 Platform · 2 Priority · 3 Sprint · 4 Target Week · 5 Estimated Duration · 6 Content Type · 7 Planned Publish Date

    private string _leadName = "";
    private string _leadCaption = "Assignee";

    // Tab 2: Script
    private string _persistedScript = string.Empty; // script đang lưu trong DB; "Lưu thay đổi" ghi lại đúng bản này để không đụng tới script (script lưu riêng ở tab Script)

    // Tab 3: Checklist

    // Tab 4: Activity

    // Tab 5: References

    // Tab 6: Metrics (theo ContentPlatform, qua IMetricBackend; null = chưa đấu nối API)
    private IMetricBackend? _metricBackend;
    private long? _metricsLoadedFor; // ContentId đã nạp Metrics; null = cần nạp lại khi mở tab

    // Empty state

    // Mảng tham chiếu tới các control meta (khai báo từng control nằm trong Designer.cs)
    private Label[] _metaCaptions = null!;
    private RoundedPanel[] _metaBoxes = null!;

    public ContentDetailPanel()
    {
        InitializeComponent();   // dựng control: xem ContentDetailPanel.Designer.cs (kéo thả được)
        SetupRuntime();          // phần chỉ chạy lúc chạy app: nạp dữ liệu, gắn sự kiện, thứ tự dòng Overview
        ShowEmptyState();
    }

    /// <summary>
    /// Mọi thứ Designer không biểu diễn được (lambda, danh sách động, SetStyle...) đặt ở đây,
    /// nên InitializeComponent trong Designer.cs giữ nguyên dạng đơn giản, Designer đọc được.
    /// </summary>
    private void SetupRuntime()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        _pnlEmptyState.BringToFront();   // Empty state phủ lên trên khi chưa chọn thẻ

        _metaCaptions = new[] { _lblMeta0, _lblMeta1, _lblMeta2, _lblMeta3, _lblMeta4, _lblMeta5, _lblMeta6, _lblMeta7 };
        _metaBoxes = new[] { _cellMeta0, _cellMeta1, _cellMeta2, _cellMeta3, _cellMeta4, _cellMeta5, _cellMeta6, _cellMeta7 };

        // Thứ tự + khoảng cách các dòng của tab Overview (LayoutOverview xếp theo danh sách này)
        _overviewRows.Clear();
        AddOverviewRow(_lblSecTitle, 6);
        AddOverviewRow(_boxTitle, 20);
        AddOverviewRow(_lblSecHook, 6);
        AddOverviewRow(_boxHook, 20);
        AddOverviewRow(_boxMeta, 20);
        AddOverviewRow(_lblSecLead, 6);
        AddOverviewRow(_boxLead, 20);
        AddOverviewRow(_lblSecWorkflow, 6);
        AddOverviewRow(_flowActions, 0);

        // Dữ liệu cho các ComboBox
        _cboPriorityEdit.Items.AddRange(new object[] { Priority.High, Priority.Medium, Priority.Low });
        _cboSprintEdit.Items.AddRange(ContentService.AvailableSprints);
        _cboTypeEdit.Items.AddRange(ContentService.AvailableContentTypes);
        _chkChecklist.SetItemChecked(0, true);
        _chkChecklist.SetItemChecked(1, true);
        UpdatePlatformButtonText();

        // Sự kiện
        _pnlHeader.CloseClicked += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        _pnlTabBar.LabelsProvider = GetTabLabels;
        _pnlTabBar.SelectedTabProvider = () => _selectedTabIndex;
        _pnlTabBar.TabSelected += idx => SwitchTab(idx);

        _pnlFooter.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
        };
        _btnDone.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        _btnSave.Click += (_, _) => SaveChanges();
        _contentReferences.PendingChangesChanged += (_, _) => UpdateSaveButtonForPendingReferences();
        RepositionFooterButtons();
        _pnlFooter.Resize += (_, _) => RepositionFooterButtons();

        _txtTitleVal.TextChanged += (_, _) => MarkDirty();
        _txtHookVal.TextChanged += (_, _) => MarkDirty();

        _mnuPlatforms.Closing += (_, e) =>
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) e.Cancel = true;
        };
        _btnPlatforms.Click += (_, _) => _mnuPlatforms.Show(_btnPlatforms, new Point(0, _btnPlatforms.Height));

        _cboPriorityEdit.SelectedIndexChanged += (_, _) => MarkDirty();
        _cboSprintEdit.SelectedIndexChanged += (_, _) => OnSprintSelectionChanged();
        _dtpTargetWeek.ValueChanged += (_, _) =>
        {
            if (!_isLoading) _deadlineEdited = true;
            MarkDirty();
        };
        _txtDurationEdit.TextChanged += (_, _) => MarkDirty();
        _cboTypeEdit.SelectedIndexChanged += (_, _) => MarkDirty();
        _dtpPlannedPublish.ValueChanged += (_, _) => MarkDirty();

        _boxLead.Paint += PaintLeadCard;
        _boxLead.Resize += (_, _) => PositionAssigneeCombo();
        _cboAssigneeEdit.SelectedIndexChanged += (_, _) =>
        {
            _leadName = (_cboAssigneeEdit.SelectedItem as ProjectMemberInfo)?.Name ?? "";
            _boxLead.Invalidate();
            MarkDirty();
        };

        _pnlTabOverview.Resize += (_, _) => LayoutOverview();
        _scriptEditor.ScriptSaved += (_, text) => _persistedScript = text;
        // ItemCheck xảy ra TRƯỚC khi trạng thái đổi → cập nhật nhãn tab sau đó
        _chkChecklist.ItemCheck += (_, _) => BeginInvoke(new Action(() => _pnlTabBar.Invalidate()));
    }

    private void RepositionFooterButtons()
    {
        _btnDone.Location = new Point(_pnlFooter.Width - PadX - _btnDone.Width, 18);
        _btnSave.Location = new Point(_btnDone.Left - 12 - _btnSave.Width, 18);
    }

    // ---------- Helpers dựng Overview ----------

    private static Label MakeSectionLabel(string text) => new Label
    {
        Text = text,
        Font = UITheme.FontLabelBold,
        ForeColor = UITheme.Neutral600,
        AutoSize = false,
        Height = 18,
        BackColor = UITheme.White, // Luôn nằm trực tiếp trên _overviewHost (nền trắng)
        UseMnemonic = false, // Label coi "&" là ký tự phím tắt và ẩn nó đi (VD "DIRECTOR & CREATOR LEAD" mất dấu &) — tắt để hiện đúng
    };

    private static RoundedPanel MakeBox(int height, Control inner)
    {
        var box = new RoundedPanel { Height = height };
        box.Controls.Add(inner);
        return box;
    }

    /// <summary>
    /// Bọc control trong 1 Panel đệm lề (RoundedPanel.Controls.Add trực tiếp sẽ dán sát viền).
    /// LƯU Ý: WinForms không cho Panel/TextBox/ComboBox có BackColor = Color.Transparent
    /// (ném ArgumentException "Control does not support transparent background colors").
    /// Mọi RoundedPanel cha ở đây đều giữ màu nền mặc định (White), nên tô đặc màu White thay vì Transparent.
    /// </summary>
    private static Panel Pad(Control inner, int horizontal, int vertical)
    {
        var wrap = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.White, Padding = new Padding(horizontal, vertical, horizontal, vertical) };
        inner.Dock = DockStyle.Fill;
        wrap.Controls.Add(inner);
        return wrap;
    }

    private static ComboBox MakeFlatCombo() => new ComboBox
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10F),
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
    };

    private void PositionAssigneeCombo()
    {
        int w = Math.Min(220, _boxLead.Width - 90 - 18);
        if (w < 100) w = 100;
        _cboAssigneeEdit.SetBounds(_boxLead.Width - w - 18, (_boxLead.Height - 29) / 2, w, 29);
    }

    private void MarkDirty()
    {
        if (_isLoading || _current == null) return;
        _isDirty = true;
        _btnSave.Enabled = true;
        _lblSaveHint.Text = "Có thay đổi chưa lưu";
        _lblSaveHint.ForeColor = UITheme.Danger;
    }

    private void UpdateSaveButtonForPendingReferences()
    {
        if (_isLoading || _current == null) return;
        bool hasPendingChanges = _isDirty || _contentReferences.HasPendingChanges;
        _btnSave.Enabled = hasPendingChanges;
        _lblSaveHint.Text = hasPendingChanges ? "Có thay đổi chưa lưu" : string.Empty;
        _lblSaveHint.ForeColor = hasPendingChanges ? UITheme.Danger : UITheme.Neutral600;
    }

    private void OnSprintSelectionChanged()
    {
        if (_isLoading || _current == null) return;

        string sprint = _cboSprintEdit.SelectedItem as string ?? _current.Sprint;
        _pnlHeader.SetData(_current.Code, SelectedPlatformNames(), sprint);
        MarkDirty();
        _lblSaveHint.Text = $"{sprint} đang chọn · Chưa lưu";
    }

    private void AddOverviewRow(Control ctl, int gapAfter)
    {
        _overviewRows.Add((ctl, gapAfter)); // control đã được thêm vào _overviewHost trong Designer.cs
    }

    /// <summary>Xếp các dòng của tab Overview theo chiều dọc (đơn giản, không phụ thuộc AutoSize của TableLayoutPanel).</summary>
    // Số lần đã tự lên lịch chạy lại LayoutOverview() do panel chưa đủ rộng — chặn vòng lặp vô hạn.
    private int _layoutRetryCount;
    private const int MaxLayoutRetries = 10;

    private void LayoutOverview()
    {
        // Trong Visual Studio Designer, panel có thể KHÔNG BAO GIỜ đạt đủ rộng (240px) ở một số bước
        // dựng layout thiết kế. Nếu vẫn tự gọi lại BeginInvoke như lúc chạy thật, nó lặp vô hạn và
        // treo cả Designer. LicenseManager.UsageMode phát hiện đúng "đang ở chế độ thiết kế" ngay cả
        // với control con (khác với thuộc tính DesignMode, vốn chỉ đúng cho control gốc đang được thiết kế).
        bool isDesignTime = System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;

        int w = _pnlTabOverview.ClientSize.Width - PadX * 2;
        if (w < 240)
        {
            if (!isDesignTime && IsHandleCreated && _layoutRetryCount < MaxLayoutRetries)
            {
                // Lần đầu drawer hiện ra, _pnlTabOverview có thể chưa được Dock/layout xong nên ClientSize
                // vẫn còn 0. Nếu return luôn ở đây mà không thử lại, mọi control trong _overviewHost sẽ
                // đứng yên ở vị trí mặc định (0,0) và chồng lên nhau. Xếp lịch chạy lại ở vòng lặp thông
                // điệp kế tiếp, khi WinForms đã tính xong layout thật. Giới hạn số lần thử để không bao
                // giờ lặp vô hạn kể cả khi có tình huống khác khiến panel mãi không đủ rộng.
                _layoutRetryCount++;
                BeginInvoke(new Action(LayoutOverview));
            }
            return;
        }
        _layoutRetryCount = 0;

        int y = 20;
        foreach (var (ctl, gap) in _overviewRows)
        {
            int h = ctl.Height;
            if (ReferenceEquals(ctl, _flowActions))
                h = Math.Max(44, _flowActions.GetPreferredSize(new Size(w, 0)).Height);
            ctl.SetBounds(PadX, y, w, h);
            y += h + gap;
        }
        y += 24;

        _overviewHost.SetBounds(0, 0, _pnlTabOverview.ClientSize.Width, y);
        _pnlTabOverview.AutoScrollMinSize = new Size(0, y);

        // Lưới 2 cột trong khung meta
        int inner = w - 40;
        int colW = (inner - 16) / 2;
        for (int i = 0; i < _metaBoxes.Length; i++)
        {
            int x = 20 + (i % 2) * (colW + 16);
            int cy = 18 + (i / 2) * 78;
            _metaCaptions[i].SetBounds(x, cy, colW, 18);
            _metaBoxes[i].SetBounds(x, cy + 22, colW, 44);
        }

        PositionAssigneeCombo();

        // Ép vẽ lại toàn bộ (đệ quy xuống control con) để tránh hình ảnh cũ của lần xếp trước còn sót lại
        // đè lên vị trí mới — hay gặp khi nhiều control bị SetBounds dời đi trong cùng một lượt layout.
        _overviewHost.Invalidate(true);
    }

    private void PaintLeadCard(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        string name = string.IsNullOrWhiteSpace(_leadName) ? "Chưa giao" : _leadName;
        string initials = "--";
        if (!string.IsNullOrWhiteSpace(_leadName))
        {
            var parts = _leadName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            initials = parts.Length >= 2 ? $"{parts[0][0]}{parts[^1][0]}" : (parts[0].Length >= 2 ? parts[0].Substring(0, 2) : parts[0]);
        }

        UITheme.DrawAvatar(g, initials, new Rectangle(18, (_boxLead.Height - 44) / 2, 44, 44), UITheme.Neutral800, UITheme.White, UITheme.FontLabelBold);
        const TextFormatFlags tf = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        int textW = Math.Max(40, _boxLead.Width - 74 - 18);
        TextRenderer.DrawText(g, name, UITheme.FontBodyBold, new Rectangle(74, 16, textW, 22), UITheme.Ink, tf);
        TextRenderer.DrawText(g, _leadCaption, UITheme.FontLabel, new Rectangle(74, 38, textW, 20), UITheme.Neutral600, tf);
    }

    private string[] GetTabLabels()
    {
        int total = _chkChecklist?.Items.Count ?? 0;
        int done = _chkChecklist?.CheckedItems.Count ?? 0;
        return new[] { "Overview", "Script", $"Checklist ({done}/{total})", "Activity", "References", "Metrics" };
    }

    // ---------- API công khai ----------

    public void Initialize(WorkflowService workflowService, IActivityRepository activityRepo, IProjectMemberRepository memberRepo, ContentService contentService, ScriptService scriptService, IMetricBackend? metricBackend = null, bool loadLocalPlatforms = true, IContentDetailBackend? detailBackend = null, IAssignmentBackend? assignmentBackend = null)
    {
        _scriptEditor.Initialize(scriptService, detailBackend);
        _detailBackend = detailBackend;
        _contentReferences.Initialize(detailBackend);
        _assignmentBackend = assignmentBackend;
        _apiPlatformsLoaded = false;
        _workflowService = workflowService;
        _activityRepo = activityRepo;
        _memberRepo = memberRepo;
        _contentService = contentService;
        _metricBackend = metricBackend;

        _mnuPlatforms.Items.Clear();
        if (detailBackend != null) return; // chế độ API: danh sách platform nạp ở lần LoadContent đầu tiên (EnsureApiPlatforms)
        if (!loadLocalPlatforms) return;
        foreach (string name in contentService.GetAvailablePlatforms())
            AddPlatformMenuItem(name);
    }

    private void AddPlatformMenuItem(string name)
    {
        var item = new ToolStripMenuItem(name) { CheckOnClick = true };
        item.CheckedChanged += (_, _) =>
        {
            UpdatePlatformButtonText();
            MarkDirty();
        };
        _mnuPlatforms.Items.Add(item);
    }

    /// <summary>Chế độ API: nạp menu platform từ API một lần (lỗi thì thử lại ở lần mở sau, không chặn việc mở drawer).</summary>
    private void EnsureApiPlatforms()
    {
        if (_detailBackend == null || _apiPlatformsLoaded) return;
        try
        {
            foreach (string name in _detailBackend.GetAvailablePlatforms())
                AddPlatformMenuItem(name);
            _apiPlatformsLoaded = true;
        }
        catch (Exception ex) when (ex is ContentValidationException or UnauthorizedWorkflowActionException or InvalidOperationException)
        {
            _mnuPlatforms.Items.Clear();
        }
    }

    /// <summary>Vai trò của người đăng nhập trong Project: qua API ở chế độ API, ngược lại đọc từ repository cục bộ.</summary>
    private ProjectRole? GetMyRole(long projectId)
    {
        if (_detailBackend == null)
            return _memberRepo!.GetRole(projectId, CurrentSession.CurrentUserId);

        if (_roleCache.TryGetValue(projectId, out var cached)) return cached;
        try
        {
            var role = _detailBackend.GetMyRole(projectId);
            _roleCache[projectId] = role;
            return role;
        }
        catch (Exception ex) when (ex is ContentValidationException or UnauthorizedWorkflowActionException or InvalidOperationException) { return null; }
    }

    private List<string> SelectedPlatformNames() =>
        _mnuPlatforms.Items.OfType<ToolStripMenuItem>().Where(i => i.Checked).Select(i => i.Text ?? string.Empty).ToList();

    private void UpdatePlatformButtonText()
    {
        var names = SelectedPlatformNames();
        _btnPlatforms.Text = (names.Count == 0 ? "Chọn nền tảng" : string.Join(" • ", names)) + "  ▾";
    }

    public void LoadContent(ContentBoardCard card)
    {
        if (_workflowService == null || _activityRepo == null || _memberRepo == null || _contentService == null)
            throw new InvalidOperationException("Phải gọi Initialize() trước khi LoadContent().");

        _isLoading = true;
        _current = card;
        _metricsLoadedFor = null;
        _metricsHost.Controls.Clear();
        _roleCache.Clear();
        EnsureApiPlatforms();
        _members = LoadMembers(card.ProjectId);

        _pnlEmptyState.Visible = false;
        _pnlHeader.Visible = true;
        _pnlTabBar.Visible = true;
        _pnlContent.Visible = true;
        _pnlFooter.Visible = true;

        _pnlHeader.SetData(card.Code, card.Platforms, card.Sprint);
        _lblFooterCode.Text = card.Code;

        _txtTitleVal.Text = card.Title;
        _txtHookVal.Text = card.Description;

        _lblStageVal.Text = UITheme.GetStageDisplayName(card.Status);

        foreach (var item in _mnuPlatforms.Items.OfType<ToolStripMenuItem>())
            item.Checked = card.Platforms.Contains(item.Text, StringComparer.OrdinalIgnoreCase);
        UpdatePlatformButtonText();

        _cboPriorityEdit.SelectedItem = card.Priority;

        _cboSprintEdit.SelectedItem = card.Sprint;
        if (_cboSprintEdit.SelectedIndex < 0) _cboSprintEdit.SelectedIndex = 0;

        _dtpTargetWeek.Value = (card.Deadline ?? DateTime.Today.AddDays(7)).Date;
        // Phân quyền deadline: chỉ Owner/Manager được sửa; Creator chỉ xem.
        _dtpTargetWeek.Enabled = TaskRules.CanChangeDeadline(GetMyRole(card.ProjectId));
        _deadlineEdited = false;

        _txtDurationEdit.Text = card.EstimatedDuration;

        // Script, loại nội dung và ngày dự kiến đăng không nằm trong thẻ Board nên đọc riêng từ DB khi mở Content Detail.
        var detail = TryLoadDetail(card.ContentId);
        _detailLoaded = detail != null;
        _persistedScript = detail?.Script ?? string.Empty;
        _contentReferences.LoadReferences(card.ContentId);
        _scriptEditor.LoadScript(card.ContentId, CurrentSession.CurrentUserId);
        SelectContentType(detail?.ContentType);
        SetPlannedPublish(detail?.PlannedPublishAt);

        // Gán lại DataSource = null rồi DataSource mới đôi khi làm ComboBox quên mất DisplayMember đã đặt
        // (WinForms quirk), khiến nó tự hiện Object.ToString() — tức tên đầy đủ của class thay vì tên người.
        // Đặt lại DisplayMember tường minh ngay trước khi gán DataSource mới để chắc chắn không bị mất.
        _cboAssigneeEdit.DataSource = null;
        _cboAssigneeEdit.DisplayMember = "Name";
        var currentMember = _members.FirstOrDefault(m => m.UserId == card.AssigneeUserId);
        _cboAssigneeEdit.DataSource = _members;
        _cboAssigneeEdit.Enabled = false; // chỉ hiển thị người đang phụ trách; giao việc dùng nút "Giao việc cho Creator" ở Workflow Actions
        _cboAssigneeEdit.SelectedItem = currentMember;
        // Nhiều Creator: hiện tất cả tên; caption cho biết bao nhiêu người đã hoàn thành.
        _leadName = card.Assignees.Count > 0
            ? string.Join(", ", card.Assignees.Select(a => a.Name))
            : currentMember?.Name ?? card.AssigneeName ?? "";
        _leadCaption = card.Assignees.Count switch
        {
            0 or 1 => "Assignee",
            _ => card.IsTeamCompleted
                ? $"Assignee · tất cả {card.TeamTotal} Creator đã hoàn thành"
                : $"Assignee · {card.TeamDone}/{card.TeamTotal} Creator hoàn thành",
        };
        _cboAssigneeEdit.Visible = false; // chỉ để lưu dữ liệu; giao việc dùng nút "Giao việc cho Creator"
        _boxLead.Invalidate();

        RebuildActionButtons();
        LayoutOverview();
        _pnlTabOverview.AutoScrollPosition = new Point(0, 0);
        RefreshActivity();
        if (_selectedTabIndex == 5) LoadMetricsIfNeeded();
        _pnlTabBar.Invalidate();

        _isLoading = false;
        _isDirty = false;
        _btnSave.Enabled = false;
        _lblSaveHint.Text = "";
    }

    /// <summary>Danh sách thành viên để hiển thị người phụ trách. Chế độ API lấy từ danh sách có thể giao việc (lỗi → rỗng, không chặn mở drawer).</summary>
    private List<ProjectMemberInfo> LoadMembers(long projectId)
    {
        if (_detailBackend == null)
            return _memberRepo!.GetMembers(projectId);
        if (_assignmentBackend == null) return new List<ProjectMemberInfo>();

        try { return _assignmentBackend.GetAssignableMembers(projectId); }
        catch (Exception ex) when (ex is ContentValidationException or UnauthorizedWorkflowActionException or InvalidOperationException) { return new List<ProjectMemberInfo>(); }
    }

    /// <summary>Đọc chi tiết Content qua ContentService. Lỗi (không có quyền, không tìm thấy, lỗi DB) được báo cho người dùng và trả về null.</summary>
    private Content? TryLoadDetail(long contentId)
    {
        try
        {
            return _detailBackend != null
                ? _detailBackend.GetDetail(contentId)
                : _contentService!.GetDetail(contentId, CurrentSession.CurrentUserId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Không tải được chi tiết nội dung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
    }

    /// <summary>Chọn loại nội dung; để trống → loại mặc định; loại lạ (dữ liệu cũ) được thêm tạm vào danh sách để không bị đổi âm thầm.</summary>
    private void SelectContentType(string? contentType)
    {
        string type = string.IsNullOrWhiteSpace(contentType) ? ContentService.DefaultContentType : contentType.Trim();

        int index = _cboTypeEdit.FindStringExact(type);
        if (index < 0)
            index = _cboTypeEdit.Items.Add(type);

        _cboTypeEdit.SelectedIndex = index;
    }

    /// <summary>Gán Value trước rồi mới đặt Checked, vì WinForms có thể tự bật tick khi gán Value.</summary>
    private void SetPlannedPublish(DateTime? plannedPublishAt)
    {
        _dtpPlannedPublish.Value = (plannedPublishAt ?? DateTime.Today).Date;
        _dtpPlannedPublish.Checked = plannedPublishAt.HasValue;
    }

    /// <summary>Trả về false (và không xoá gì) nếu người dùng chọn giữ lại thay đổi chưa lưu.</summary>
    public bool Clear()
    {
        if (HasUnsavedChanges && _current != null)
        {
            var result = MessageBox.Show(this,
                "Bạn có thay đổi chưa lưu. Đóng và bỏ qua thay đổi?",
                "Có thay đổi chưa lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return false;
        }

        _current = null;
        _isDirty = false;
        _scriptEditor.Reset();
        ShowEmptyState();
        return true;
    }

    /// <summary>true nếu có thay đổi chưa lưu — BoardForm dùng để hỏi trước khi reload/đóng.</summary>
    public bool HasUnsavedChanges => _isDirty || _scriptEditor.IsDirty || _contentReferences.HasPendingChanges;

    public void ResetProject()
    {
        InvalidateProjectPermissions();
        _current = null;
        _isDirty = false;
        _detailLoaded = false;
        _scriptEditor.Reset();
        _metricsLoadedFor = null;
        _metricsHost.Controls.Clear();
        ShowEmptyState();
    }

    public void InvalidateProjectPermissions()
    {
        _roleCache.Clear();
        _flowActions.Controls.Clear();
        _dtpTargetWeek.Enabled = false;
    }

    public void RefreshProjectPermissions()
    {
        if (_current is null) return;
        _roleCache.Clear();
        _dtpTargetWeek.Enabled = TaskRules.CanChangeDeadline(GetMyRole(_current.ProjectId));
        RebuildActionButtons();
    }

    private void SaveChanges()
    {
        if (_current == null || _contentService == null) return;

        if (_isDirty && !_detailLoaded)
        {
            MessageBox.Show(this, "Chưa tải được chi tiết nội dung (script, loại nội dung, ngày dự kiến đăng) nên không thể lưu. Hãy đóng và mở lại nội dung này.",
                "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var draft = new ContentDraft
        {
            Title = _txtTitleVal.Text,
            Description = _txtHookVal.Text,
            Script = _persistedScript, // không sửa script ở đây: tab Script lưu riêng qua ScriptService
            ContentType = _cboTypeEdit.SelectedItem as string ?? string.Empty,
            PlannedPublishAt = _dtpPlannedPublish.Checked ? _dtpPlannedPublish.Value.Date : null,
            Priority = _cboPriorityEdit.SelectedItem is Priority p ? p : _current.Priority,
            Platforms = SelectedPlatformNames(),
            Sprint = _cboSprintEdit.SelectedItem as string ?? _current.Sprint,
            Deadline = _dtpTargetWeek.Enabled && _deadlineEdited ? _dtpTargetWeek.Value.Date : _current.Deadline,
            EstimatedDuration = _txtDurationEdit.Text,
            AssigneeUserId = _current.AssigneeUserId, // giữ nguyên; đổi người phụ trách chỉ qua nút "Giao việc cho Creator"
        };

        try
        {
            if (_isDirty)
            {
                if (_detailBackend != null)
                    _detailBackend.Update(_current.ContentId, draft);
                else
                    _contentService.Update(_current.ContentId, draft, CurrentSession.CurrentUserId);
                _current.Sprint = draft.Sprint;
                _pnlHeader.SetData(_current.Code, draft.Platforms, draft.Sprint);
            }

            _contentReferences.SavePendingChanges();
            _deadlineEdited = false;
            _isDirty = false;
            _btnSave.Enabled = false;
            _lblSaveHint.Text = "Đã lưu";
            _lblSaveHint.ForeColor = UITheme.Success;
            ContentChanged?.Invoke(this, EventArgs.Empty);
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
            MessageBox.Show(this, ex.Message, "Không lưu được Content", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (ApiBusinessException ex)
        {
            MessageBox.Show(this, ex.Message, "Không lưu được thay đổi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (ApiException ex)
        {
            MessageBox.Show(this, ex.Message, "Không kết nối được API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowEmptyState()
    {
        _pnlHeader.Visible = false;
        _pnlTabBar.Visible = false;
        _pnlContent.Visible = false;
        _pnlFooter.Visible = false;
        _pnlEmptyState.Visible = true;
        _activityTimeline.Clear();
        _contentReferences.ClearReferences();
    }

    private void SwitchTab(int tabIndex)
    {
        _selectedTabIndex = tabIndex;
        _pnlTabOverview.Visible = (tabIndex == 0);
        _pnlTabScript.Visible = (tabIndex == 1);
        _pnlTabChecklist.Visible = (tabIndex == 2);
        _pnlTabActivity.Visible = (tabIndex == 3);
        _pnlTabReferences.Visible = (tabIndex == 4);
        _pnlTabMetrics.Visible = (tabIndex == 5);
        _pnlTabBar.Invalidate();
        if (tabIndex == 5) LoadMetricsIfNeeded();
    }

    private void RefreshActivity()
    {
        if (_current == null) return;
        if (_detailBackend == null)
        {
            _activityTimeline.LoadActivity(_activityRepo!.GetActivity(_current.ContentId));
            return;
        }

        try
        {
            _activityTimeline.LoadActivity(_detailBackend.GetActivity(_current.ContentId));
        }
        catch (Exception ex) when (ex is ContentValidationException or UnauthorizedWorkflowActionException or InvalidOperationException)
        {
            _activityTimeline.LoadActivity(new List<ActivityItem>()); // không tải được: để trống, không chặn mở drawer
        }
    }

    // ---------- Tab Metrics (theo ContentPlatform, qua API) ----------

    /// <summary>Nạp Metrics khi mở tab lần đầu cho mỗi Content. Quyền/nút Lưu lấy từ cờ CanRecord của API, không tự suy ở client.</summary>
    private void LoadMetricsIfNeeded()
    {
        if (_current == null || _metricsLoadedFor == _current.ContentId) return;
        _metricsHost.Controls.Clear();
        if (_metricBackend == null)
        {
            _metricsHost.Controls.Add(MakeMetricsInfo("Chưa kết nối API nên không tải được số liệu."));
            return;
        }
        try
        {
            var rows = _metricBackend.GetContentMetrics(_current.ContentId).Platforms
                .Select(ContentMetric.FromResponse).ToList();
            if (rows.Count == 0)
                _metricsHost.Controls.Add(MakeMetricsInfo("Content này chưa gắn platform nào."));
            else
                foreach (var row in rows) // Dock=Top: thêm trước hiện trước, giữ đúng thứ tự API
                    _metricsHost.Controls.Add(MakeMetricRow(row));
            _metricsLoadedFor = _current.ContentId;
        }
        catch (Exception ex)
        {
            _metricsHost.Controls.Add(MakeMetricsInfo("Không tải được số liệu: " + ex.Message));
            MessageBox.Show(this, ex.Message, "Không tải được số liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Label MakeMetricsInfo(string text) => new Label
    {
        Text = text,
        Font = UITheme.FontBody,
        ForeColor = UITheme.Neutral600,
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 8, 0, 8),
        UseMnemonic = false,
    };

    private sealed class MetricRowState
    {
        public ContentMetric Metric = null!;
        public TextBox TxtViews = null!;
        public TextBox TxtLikes = null!;
        public TextBox TxtComments = null!;
        public TextBox TxtShares = null!;
        public Label LblUpdated = null!;
    }

    /// <summary>Mỗi platform một hàng: tên + trạng thái, 4 ô số (trống = giữ nguyên), nút Lưu riêng, nhãn cập nhật lần cuối.</summary>
    private Panel MakeMetricRow(ContentMetric metric)
    {
        var state = new MetricRowState { Metric = metric };
        var row = new Panel { Dock = DockStyle.Top, Height = 172, BackColor = UITheme.White, Padding = new Padding(0, 10, 0, 10) };

        var header = new Label
        {
            Text = $"{metric.PlatformName} · {metric.PublicationStatus}",
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Neutral600,
            AutoSize = false,
            Height = 22,
            Dock = DockStyle.Top,
            UseMnemonic = false,
        };
        row.Controls.Add(header);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = UITheme.White,
        };
        for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        string[] names = { "Views", "Likes", "Comments", "Shares" };
        TextBox[] boxes = new TextBox[4];
        for (int i = 0; i < 4; i++)
        {
            grid.Controls.Add(new Label
            {
                Text = names[i],
                Font = UITheme.FontLabel,
                ForeColor = UITheme.Neutral600,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                UseMnemonic = false,
            }, i, 0);
            boxes[i] = new TextBox { Font = new Font("Segoe UI", 10F), Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            grid.Controls.Add(boxes[i], i, 1);
        }
        state.TxtViews = boxes[0];
        state.TxtLikes = boxes[1];
        state.TxtComments = boxes[2];
        state.TxtShares = boxes[3];
        row.Controls.Add(grid);

        var bottom = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UITheme.White };
        var btnSave = new ProjectActionButton
        {
            Text = "Lưu",
            Style = RoundButtonStyle.Primary,
            Size = new Size(110, 36),
            Location = new Point(0, 4),
            Enabled = metric.CanRecord && _metricBackend != null,
        };
        btnSave.Click += (_, _) => OnMetricSave(state);
        bottom.Controls.Add(btnSave);
        state.LblUpdated = new Label
        {
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral400,
            AutoSize = false,
            Location = new Point(122, 4),
            Size = new Size(600, 36),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
        };
        RefreshMetricLabel(state);
        bottom.Controls.Add(state.LblUpdated);
        row.Controls.Add(bottom);
        // Dock=Top xếp theo reverse z-order: control thêm trước nằm trên (header → grid → bottom).
        return row;
    }

    private static void RefreshMetricLabel(MetricRowState state)
    {
        string text = ContentMetric.UpdatedLabel(state.Metric);
        if (!state.Metric.CanRecord && state.Metric.HasData)
            text += " Chỉ Owner/Manager được nhập sau Published.";
        else if (!state.Metric.CanRecord)
            text = "Chưa có số liệu. Chỉ Owner/Manager được nhập sau Published.";
        state.LblUpdated.Text = text;
    }

    private void OnMetricSave(MetricRowState state)
    {
        if (_current == null || _metricBackend == null) return;
        if (!ContentMetric.TryParseField(state.TxtViews.Text, out long? views))
        { WarnMetric("Views phải là số nguyên ≥ 0 (để trống = giữ nguyên)."); return; }
        if (!ContentMetric.TryParseField(state.TxtLikes.Text, out long? likes))
        { WarnMetric("Likes phải là số nguyên ≥ 0 (để trống = giữ nguyên)."); return; }
        if (!ContentMetric.TryParseField(state.TxtComments.Text, out long? comments))
        { WarnMetric("Comments phải là số nguyên ≥ 0 (để trống = giữ nguyên)."); return; }
        if (!ContentMetric.TryParseField(state.TxtShares.Text, out long? shares))
        { WarnMetric("Shares phải là số nguyên ≥ 0 (để trống = giữ nguyên)."); return; }
        if (views is null && likes is null && comments is null && shares is null)
        { WarnMetric("Cần nhập ít nhất một chỉ số."); return; }
        try
        {
            var saved = _metricBackend.RecordMetric(_current.ContentId, state.Metric.PlatformId, views, likes, comments, shares);
            state.Metric = ContentMetric.FromResponse(saved);
            state.TxtViews.Text = state.TxtLikes.Text = state.TxtComments.Text = state.TxtShares.Text = "";
            RefreshMetricLabel(state);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Không lưu được số liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void WarnMetric(string message) =>
        MessageBox.Show(this, message, "Số liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    // ---------- Logic Workflow (GIỮ NGUYÊN) ----------

    private void RebuildActionButtons()
    {
        _flowActions.Controls.Clear();
        if (_current == null) return;

        var role = GetMyRole(_current.ProjectId);
        if (role is null) return;
        var canReview = role == ProjectRole.Owner || role == ProjectRole.Manager;

        if (TaskRules.CanAssign(role) && _current.Status is not (ContentStatus.Published or ContentStatus.Archived))
            AddActionButton("Giao việc cho Creator", RoundButtonStyle.Primary, OnAssignClicked);

        switch (_current.Status)
        {
            case ContentStatus.Editing:
                AddActionButton("Submit Review (Gửi kiểm duyệt)", RoundButtonStyle.Primary, OnSubmitReviewClicked);
                break;

            case ContentStatus.Review:
                if (canReview)
                {
                    // Một nút duy nhất: quyết định Duyệt/Từ chối nằm trong dialog kiểm duyệt.
                    AddActionButton("Kiểm duyệt nội dung", RoundButtonStyle.Primary, OnReviewDecisionClicked);
                }
                else
                {
                    AddInfoLabel("Đang chờ Owner/Manager duyệt...");
                }
                break;

            default:
                var next = _workflowService!.GetNextSimpleStatus(_current.Status);
                if (next.HasValue)
                {
                    string targetName = UITheme.GetStageDisplayName(next.Value);
                    AddActionButton($"Chuyển sang {targetName} →", RoundButtonStyle.Primary, OnMoveNextClicked);
                }
                break;
        }
    }

    private void AddActionButton(string text, RoundButtonStyle style, EventHandler onClick)
    {
        var button = new ProjectActionButton
        {
            Text = text,
            Style = style,
            AutoSize = true,
            Height = 36,
            Margin = new Padding(0, 0, 8, 8),
        };
        button.Click += onClick;
        _flowActions.Controls.Add(button);
    }

    private void AddInfoLabel(string text)
    {
        _flowActions.Controls.Add(new Label
        {
            Text = text,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
        });
    }

    private void OnAssignClicked(object? sender, EventArgs e)
    {
        if (_current == null || _contentService == null) return;
        if (!TaskRules.CanAssign(GetMyRole(_current.ProjectId))) return;
        if (_detailBackend != null && _assignmentBackend == null) return;

        // Sau khi giao, drawer được tải lại → thay đổi chưa lưu sẽ mất, nên hỏi trước.
        if (HasUnsavedChanges && MessageBox.Show(this,
                "Có thay đổi chưa lưu. Giao việc sẽ tải lại nội dung và bỏ các thay đổi này. Tiếp tục?",
                "Giao việc", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        List<ProjectMemberInfo> creators;
        List<MyTaskItem> assignments;
        try
        {
            if (_assignmentBackend != null)
            {
                creators = _assignmentBackend.GetAssignableMembers(_current.ProjectId);
                assignments = _assignmentBackend.GetAssignments(_current.ContentId);
            }
            else
            {
                creators = _contentService.GetAssignableMembers(_current.ProjectId, CurrentSession.CurrentUserId);
                assignments = _contentService.GetAssignments(_current.ContentId);
            }
        }
        catch (ContentValidationException ex)
        {
            MessageBox.Show(this, ex.Message, "Không tải được danh sách giao việc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Không tải được danh sách giao việc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new AssignContentDialog(
            $"CNT-{_current.ContentId:000}", _current.Title, creators,
            assignments, _current.Deadline);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            if (_assignmentBackend != null)
                _assignmentBackend.AssignContent(_current.ContentId, dlg.Assignments);
            else
                _contentService.AssignContent(_current.ContentId, dlg.Assignments, CurrentSession.CurrentUserId);
            ContentChanged?.Invoke(this, EventArgs.Empty);
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
            MessageBox.Show(this, ex.Message, "Không lưu được giao việc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnMoveNextClicked(object? sender, EventArgs e)
    {
        if (_current == null) return;
        var next = _workflowService!.GetNextSimpleStatus(_current.Status);
        if (!next.HasValue) return;

        var contentId = _current.ContentId;
        RunWorkflowAction(() =>
        {
            if (_detailBackend != null) _detailBackend.ChangeStatus(contentId, next.Value);
            else _workflowService!.ChangeStatus(contentId, next.Value, CurrentSession.CurrentUserId);
        });
    }

    private void OnSubmitReviewClicked(object? sender, EventArgs e)
    {
        if (_current == null) return;
        var contentId = _current.ContentId;
        RunWorkflowAction(() =>
        {
            if (_detailBackend != null) _detailBackend.SubmitForReview(contentId);
            else _workflowService!.SubmitForReview(contentId, CurrentSession.CurrentUserId);
        });
    }

    private void OnReviewDecisionClicked(object? sender, EventArgs e)
    {
        if (_current == null) return;
        if (GetMyRole(_current.ProjectId) is not (ProjectRole.Owner or ProjectRole.Manager)) return;

        using var dialog = new Forms.Board.ReviewDecisionDialog(
            $"{_current.Code} — {_current.Title}",
            "Xem chi tiết Review trong Tab Activity.");

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var contentId = _current.ContentId;
        if (dialog.Approved)
        {
            RunWorkflowAction(() =>
            {
                if (_detailBackend != null) _detailBackend.DecideReview(contentId, true, dialog.Feedback);
                else _workflowService!.ApproveReview(contentId, CurrentSession.CurrentUserId, dialog.Feedback);
            });
        }
        else
        {
            RunWorkflowAction(() =>
            {
                if (_detailBackend != null) _detailBackend.DecideReview(contentId, false, dialog.Feedback);
                else _workflowService!.RejectReview(contentId, CurrentSession.CurrentUserId, dialog.Feedback);
            });
        }
    }

    private void RunWorkflowAction(Action action)
    {
        try
        {
            action();
            ContentChanged?.Invoke(this, EventArgs.Empty);
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
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Không thể chuyển trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

// ---------- Header vẽ tay: [Mã] [Nền tảng] [Sprint] ........ [X] ----------
public sealed class ContentDetailHeaderPanel : Panel
{
    private const int PadX = 24;
    private string _code = "";
    private IReadOnlyList<string> _platforms = Array.Empty<string>();
    private string _sprint = "";
    private bool _closeHover;
    private Rectangle _closeRect;

    public event EventHandler? CloseClicked;

    public ContentDetailHeaderPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = UITheme.White;
    }

    public void SetData(string code, IReadOnlyList<string> platforms, string sprint)
    {
        _code = code; _platforms = platforms; _sprint = sprint;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool h = _closeRect.Contains(e.Location);
        if (h != _closeHover) { _closeHover = h; Cursor = h ? Cursors.Hand : Cursors.Default; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_closeHover) { _closeHover = false; Cursor = Cursors.Default; Invalidate(); }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (_closeRect.Contains(e.Location)) CloseClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.White);

        int cy = Height / 2;
        int x = PadX;

        // Chip mã
        var codeSize = TextRenderer.MeasureText(g, _code, UITheme.FontMonoLarge, new Size(int.MaxValue, 30), TextFormatFlags.NoPadding);
        var codeRect = new Rectangle(x, cy - 16, codeSize.Width + 24, 32);
        using (var b = new SolidBrush(UITheme.Neutral200))
        using (var p = UITheme.CreateRoundedRectanglePath(codeRect, 6))
            g.FillPath(b, p);
        TextRenderer.DrawText(g, _code, UITheme.FontMonoLarge, codeRect, UITheme.Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        x = codeRect.Right + 10;

        // Pill nền tảng đầu tiên + "+N" nếu Content có nhiều nền tảng (danh sách đầy đủ nằm ở ô Platform)
        if (_platforms.Count > 0)
        {
            int pw = UIIcons.MeasurePlatformPill(g, _platforms[0], 34);
            var pr = new Rectangle(x, cy - 17, pw, 34);
            UIIcons.DrawPlatformPill(g, _platforms[0], pr);
            x = pr.Right + 10;

            if (_platforms.Count > 1)
            {
                string more = $"+{_platforms.Count - 1}";
                var moreSize = TextRenderer.MeasureText(g, more, UITheme.FontBodyBold, new Size(int.MaxValue, 30), TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, more, UITheme.FontBodyBold, new Rectangle(x, cy - 15, moreSize.Width, 30), UITheme.Neutral600,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x += moreSize.Width + 10;
            }
        }

        // Chip sprint
        var sprintSize = TextRenderer.MeasureText(g, _sprint, UITheme.FontBodyBold, new Size(int.MaxValue, 30), TextFormatFlags.NoPadding);
        var sr = new Rectangle(x, cy - 15, sprintSize.Width + 24, 30);
        using (var b = new SolidBrush(UITheme.White))
        using (var pen = new Pen(UITheme.Neutral200, 1f))
        using (var p = UITheme.CreateRoundedRectanglePath(new Rectangle(sr.X, sr.Y, sr.Width - 1, sr.Height - 1), 6))
        {
            g.FillPath(b, p);
            g.DrawPath(pen, p);
        }
        TextRenderer.DrawText(g, _sprint, UITheme.FontBodyBold, sr, UITheme.Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        // Nút đóng
        _closeRect = new Rectangle(Width - PadX - 32, cy - 16, 32, 32);
        if (_closeHover)
        {
            using var hb = new SolidBrush(UITheme.Neutral100);
            using var hp = UITheme.CreateRoundedRectanglePath(_closeRect, 8);
            g.FillPath(hb, hp);
        }
        UIIcons.Close(g, _closeRect, UITheme.Neutral600, 2f);

        using (var borderPen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
    }
}

// ---------- Tab bar: căn trái, độ rộng theo chữ, gạch chân đen dưới tab đang chọn ----------
public sealed class ContentDetailTabBar : Panel
{
    private const int PadX = 24;
    private const int Gap = 28;
    private static readonly string[] DefaultLabels = { "Overview", "Script", "Checklist", "Activity", "References", "Metrics" };

    // Gán lúc chạy (SetupRuntime) — không lưu vào Designer.
    [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<string[]>? LabelsProvider { get; set; }

    [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<int>? SelectedTabProvider { get; set; }

    public event Action<int>? TabSelected;

    public ContentDetailTabBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = UITheme.White;
        Cursor = Cursors.Hand;
    }

    private string[] Labels => LabelsProvider?.Invoke() ?? DefaultLabels;
    private int SelectedTab => SelectedTabProvider?.Invoke() ?? 0;

    private List<Rectangle> GetRects(Graphics g, string[] labels)
    {
        var list = new List<Rectangle>();
        int x = PadX;
        for (int i = 0; i < labels.Length; i++)
        {
            var font = i == SelectedTab ? UITheme.FontBodyBold : UITheme.FontBody;
            int w = TextRenderer.MeasureText(g, labels[i], font, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding).Width;
            list.Add(new Rectangle(x, 0, w, Height));
            x += w + Gap;
        }
        return list;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        using var g = CreateGraphics();
        var rects = GetRects(g, Labels);
        for (int i = 0; i < rects.Count; i++)
        {
            var hit = Rectangle.Inflate(rects[i], Gap / 2, 0);
            if (hit.Contains(e.Location)) { TabSelected?.Invoke(i); return; }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.White);

        using (var borderPen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);

        var labels = Labels;
        var rects = GetRects(g, labels);
        int selected = SelectedTab;

        for (int i = 0; i < labels.Length; i++)
        {
            bool active = i == selected;
            TextRenderer.DrawText(g, labels[i], active ? UITheme.FontBodyBold : UITheme.FontBody,
                rects[i], active ? UITheme.Black : UITheme.Neutral600,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            if (active)
            {
                using var pen = new Pen(UITheme.Black, 2.5f);
                g.DrawLine(pen, rects[i].Left, Height - 2, rects[i].Right, Height - 2);
            }
        }
    }
}
