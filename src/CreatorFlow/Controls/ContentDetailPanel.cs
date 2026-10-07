using CreatorFlow.Models.Enums;
using System.Drawing.Drawing2D;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
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

    private ContentBoardCard? _current;
    private List<ProjectMemberInfo> _members = new();
    private int _selectedTabIndex = 0; // 0: Overview, 1: Script, 2: Checklist, 3: Activity
    private bool _isDirty;
    private bool _isLoading; // true trong lúc LoadContent() đang gán giá trị → bỏ qua sự kiện TextChanged/SelectedIndexChanged
    private bool _detailLoaded; // false nếu đọc chi tiết (script, loại, ngày đăng) từ DB thất bại → chặn Lưu để không ghi đè dữ liệu bằng giá trị rỗng

    public event EventHandler? ContentChanged;
    public event EventHandler? CloseRequested;

    // Khung chính
    private readonly HeaderPanel _pnlHeader;
    private readonly TabBarPanel _pnlTabBar;
    private readonly Panel _pnlContent;
    private readonly Panel _pnlFooter;
    private readonly Label _lblFooterCode;
    private readonly Label _lblSaveHint;
    private readonly RoundedButton _btnSave;
    private readonly RoundedButton _btnDone;

    // Tab 1: Overview
    private readonly Panel _pnlTabOverview;
    private readonly Panel _overviewHost;
    private readonly List<(Control Ctl, int GapAfter)> _overviewRows = new();
    private readonly TextBox _txtTitleVal;
    private readonly TextBox _txtHookVal;
    private readonly RoundedPanel _boxMeta;
    private readonly Label[] _metaCaptions = new Label[8];
    private readonly RoundedPanel[] _metaBoxes = new RoundedPanel[8];

    // Index: 0 Pipeline Stage (chỉ đọc) · 1 Platform · 2 Priority · 3 Sprint · 4 Target Week · 5 Estimated Duration · 6 Content Type · 7 Planned Publish Date
    private readonly Label _lblStageVal;
    private readonly Button _btnPlatforms;
    private readonly ContextMenuStrip _mnuPlatforms;
    private readonly ComboBox _cboPriorityEdit;
    private readonly ComboBox _cboSprintEdit;
    private readonly DateTimePicker _dtpTargetWeek;
    private readonly TextBox _txtDurationEdit;
    private readonly ComboBox _cboTypeEdit;
    private readonly DateTimePicker _dtpPlannedPublish;

    private readonly RoundedPanel _boxLead;
    private readonly ComboBox _cboAssigneeEdit;
    private string _leadName = "";
    private readonly FlowLayoutPanel _flowActions;

    // Tab 2: Script
    private readonly Panel _pnlTabScript;
    private readonly TextBox _txtScript;

    // Tab 3: Checklist
    private readonly Panel _pnlTabChecklist;
    private readonly CheckedListBox _chkChecklist;

    // Tab 4: Activity
    private readonly Panel _pnlTabActivity;
    private readonly ActivityTimelineControl _activityTimeline;

    // Empty state
    private readonly Panel _pnlEmptyState;

    public ContentDetailPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Dock = DockStyle.Fill;
        BackColor = UITheme.White;
        Font = UITheme.FontBody;

        // 1. Header
        _pnlHeader = new HeaderPanel { Dock = DockStyle.Top, Height = 76 };
        _pnlHeader.CloseClicked += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        // 2. Tab bar
        _pnlTabBar = new TabBarPanel(GetTabLabels, () => _selectedTabIndex, idx => SwitchTab(idx))
        {
            Dock = DockStyle.Top,
            Height = 50,
        };

        // 3. Footer
        _pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = UITheme.White };
        _pnlFooter.Paint += (_, e) =>
        {
            using var pen = new Pen(UITheme.Neutral200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
        };
        _lblFooterCode = new Label
        {
            Font = UITheme.FontMono,
            ForeColor = UITheme.Neutral400,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(PadX, 0),
            Size = new Size(96, 80),
            BackColor = UITheme.White,
        };
        _lblSaveHint = new Label
        {
            Text = "",
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral400,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(PadX + 96, 0),
            Size = new Size(150, 80),
            BackColor = UITheme.White,
        };
        _btnDone = new RoundedButton
        {
            Text = "Done & Close",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(130, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _btnDone.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        _btnSave = new RoundedButton
        {
            Text = "Lưu thay đổi",
            Style = RoundButtonStyle.Primary,
            Size = new Size(130, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false,
        };
        _btnSave.Click += (_, _) => SaveChanges();
        void RepositionFooterButtons()
        {
            _btnDone.Location = new Point(_pnlFooter.Width - PadX - _btnDone.Width, 18);
            _btnSave.Location = new Point(_btnDone.Left - 12 - _btnSave.Width, 18);
        }
        _pnlFooter.Controls.Add(_lblFooterCode);
        _pnlFooter.Controls.Add(_lblSaveHint);
        _pnlFooter.Controls.Add(_btnDone);
        _pnlFooter.Controls.Add(_btnSave);
        RepositionFooterButtons();
        _pnlFooter.Resize += (_, _) => RepositionFooterButtons();

        // 4. Vùng nội dung
        _pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.White };

        // ---- TAB 1: OVERVIEW ----
        _pnlTabOverview = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UITheme.White };
        _overviewHost = new Panel { Location = new Point(0, 0), BackColor = UITheme.White };
        _pnlTabOverview.Controls.Add(_overviewHost);

        AddOverviewRow(MakeSectionLabel("PRODUCTION TITLE"), 6);
        _txtTitleVal = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = UITheme.Ink,
            BorderStyle = BorderStyle.None,
            Multiline = true,
        };
        _txtTitleVal.TextChanged += (_, _) => MarkDirty();
        AddOverviewRow(MakeBox(60, Pad(_txtTitleVal, 16, 10)), 20);

        AddOverviewRow(MakeSectionLabel("CONCEPT HOOK"), 6);
        _txtHookVal = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = UITheme.Ink,
            BorderStyle = BorderStyle.None,
            Multiline = true,
        };
        _txtHookVal.TextChanged += (_, _) => MarkDirty();
        AddOverviewRow(MakeBox(104, Pad(_txtHookVal, 16, 12)), 20);

        _boxMeta = new RoundedPanel { Height = 18 + 4 * 78 + 6, FillColor = UITheme.Neutral50, Radius = 10 };
        string[] captions = { "Pipeline Stage", "Platform", "Priority", "Sprint Allocation", "Target Week", "Estimated Duration", "Content Type", "Planned Publish Date" };
        for (int i = 0; i < 8; i++)
        {
            _metaCaptions[i] = new Label
            {
                Text = captions[i],
                Font = UITheme.FontLabelBold,
                ForeColor = UITheme.Neutral700,
                AutoSize = false,
                Height = 18,
                BackColor = UITheme.Neutral50, // Nền của _boxMeta (cha trực tiếp)
            };
            _metaBoxes[i] = new RoundedPanel { Height = 44 };
            _boxMeta.Controls.Add(_metaCaptions[i]);
            _boxMeta.Controls.Add(_metaBoxes[i]);
        }

        // 0. Pipeline Stage — chỉ đọc, đổi trạng thái phải qua nút hành động Workflow bên dưới.
        _lblStageVal = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            ForeColor = UITheme.Neutral600,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 14, 0),
            BackColor = UITheme.Neutral100, // Khớp FillColor của _metaBoxes[0] đặt ngay dưới đây
            UseMnemonic = false, // GetStageDisplayName() có thể trả về chuỗi chứa "&" (VD "Ideas & Discovery")
        };
        _metaBoxes[0].FillColor = UITheme.Neutral100;
        _metaBoxes[0].Controls.Add(_lblStageVal);

        // 1. Platform — chọn nhiều: bấm nút để mở menu tick, menu không đóng sau mỗi lần tick.
        // Các mục của menu được nạp từ bảng platforms trong Initialize().
        _mnuPlatforms = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = true, Font = new Font("Segoe UI", 10F) };
        _mnuPlatforms.Closing += (_, e) =>
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) e.Cancel = true;
        };
        _btnPlatforms = new Button
        {
            Font = new Font("Segoe UI", 10F),
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.White,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            UseMnemonic = false,
        };
        _btnPlatforms.FlatAppearance.BorderSize = 0;
        _btnPlatforms.Click += (_, _) => _mnuPlatforms.Show(_btnPlatforms, new Point(0, _btnPlatforms.Height));
        _metaBoxes[1].Controls.Add(Pad(_btnPlatforms, 12, 7));
        UpdatePlatformButtonText();

        // 2. Priority
        _cboPriorityEdit = MakeFlatCombo();
        _cboPriorityEdit.Items.AddRange(new object[] { Priority.High, Priority.Medium, Priority.Low });
        _cboPriorityEdit.SelectedIndexChanged += (_, _) => MarkDirty();
        _metaBoxes[2].Controls.Add(Pad(_cboPriorityEdit, 12, 7));

        // 3. Sprint Allocation
        _cboSprintEdit = MakeFlatCombo();
        _cboSprintEdit.Items.AddRange(ContentService.AvailableSprints);
        _cboSprintEdit.SelectedIndexChanged += (_, _) => MarkDirty();
        _metaBoxes[3].Controls.Add(Pad(_cboSprintEdit, 12, 7));

        // 4. Target Week (Deadline)
        _dtpTargetWeek = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            Format = DateTimePickerFormat.Short,
        };
        _dtpTargetWeek.ValueChanged += (_, _) => MarkDirty();
        _metaBoxes[4].Controls.Add(Pad(_dtpTargetWeek, 10, 7));

        // 5. Estimated Duration
        _txtDurationEdit = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            BorderStyle = BorderStyle.None,
            PlaceholderText = "VD: 24 min",
        };
        _txtDurationEdit.TextChanged += (_, _) => MarkDirty();
        _metaBoxes[5].Controls.Add(Pad(_txtDurationEdit, 14, 11));

        // 6. Content Type — danh sách cố định ContentService.AvailableContentTypes
        _cboTypeEdit = MakeFlatCombo();
        _cboTypeEdit.Items.AddRange(ContentService.AvailableContentTypes);
        _cboTypeEdit.SelectedIndexChanged += (_, _) => MarkDirty();
        _metaBoxes[6].Controls.Add(Pad(_cboTypeEdit, 12, 7));

        // 7. Planned Publish Date — tùy chọn: bỏ tick = chưa lên lịch đăng
        _dtpPlannedPublish = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false,
        };
        _dtpPlannedPublish.ValueChanged += (_, _) => MarkDirty();
        _metaBoxes[7].Controls.Add(Pad(_dtpPlannedPublish, 10, 7));

        AddOverviewRow(_boxMeta, 20);

        AddOverviewRow(MakeSectionLabel("DIRECTOR & CREATOR LEAD"), 6);
        _boxLead = new RoundedPanel { Height = 76, FillColor = UITheme.Neutral50, Radius = 10 };
        _boxLead.Paint += PaintLeadCard;
        _cboAssigneeEdit = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.FontBody,
            DisplayMember = "Name",
        };
        _cboAssigneeEdit.SelectedIndexChanged += (_, _) =>
        {
            _leadName = (_cboAssigneeEdit.SelectedItem as ProjectMemberInfo)?.Name ?? "";
            _boxLead.Invalidate();
            MarkDirty();
        };
        _boxLead.Controls.Add(_cboAssigneeEdit);
        _boxLead.Resize += (_, _) => PositionAssigneeCombo();
        AddOverviewRow(_boxLead, 20);

        AddOverviewRow(MakeSectionLabel("WORKFLOW ACTIONS"), 6);
        _flowActions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = false,
            Height = 44,
            BackColor = UITheme.White,
        };
        AddOverviewRow(_flowActions, 0);

        _pnlTabOverview.Resize += (_, _) => LayoutOverview();

        // ---- TAB 2: SCRIPT ----
        _pnlTabScript = new Panel { Dock = DockStyle.Fill, Padding = new Padding(PadX, 20, PadX, 20), Visible = false };
        _txtScript = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 10.5F),
            BorderStyle = BorderStyle.FixedSingle,
            AcceptsReturn = true,
            PlaceholderText = "Kịch bản: [Hook 0-3s] ... [Body] ... [Call To Action] ...",
        };
        _txtScript.TextChanged += (_, _) => MarkDirty();
        _pnlTabScript.Controls.Add(_txtScript);

        // ---- TAB 3: CHECKLIST ----
        _pnlTabChecklist = new Panel { Dock = DockStyle.Fill, Padding = new Padding(PadX, 20, PadX, 20), Visible = false };
        _chkChecklist = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            BorderStyle = BorderStyle.FixedSingle,
        };
        _chkChecklist.Items.AddRange(new object[]
        {
            "1. Lên dàn ý kịch bản (Outline)",
            "2. Duyệt kịch bản chính thức",
            "3. Chuẩn bị đạo cụ & thiết bị quay",
            "4. Quay phân cảnh chính (Studio / Hiện trường)",
            "5. Thu âm lồng tiếng (Voiceover)",
            "6. Dựng nháp (Rough cut)",
            "7. Hậu kỳ màu sắc & âm thanh (Color & Mix)",
            "8. Kiểm tra bản quyền âm nhạc & nhãn dán"
        });
        _chkChecklist.SetItemChecked(0, true);
        _chkChecklist.SetItemChecked(1, true);
        // ItemCheck xảy ra TRƯỚC khi trạng thái đổi → cập nhật nhãn tab sau đó
        _chkChecklist.ItemCheck += (_, _) => BeginInvoke(new Action(() => _pnlTabBar.Invalidate()));
        _pnlTabChecklist.Controls.Add(_chkChecklist);

        // ---- TAB 4: ACTIVITY ----
        _pnlTabActivity = new Panel { Dock = DockStyle.Fill, Padding = new Padding(PadX, 20, PadX, 20), Visible = false };
        _activityTimeline = new ActivityTimelineControl { Dock = DockStyle.Fill };
        _pnlTabActivity.Controls.Add(_activityTimeline);

        _pnlContent.Controls.Add(_pnlTabOverview);
        _pnlContent.Controls.Add(_pnlTabScript);
        _pnlContent.Controls.Add(_pnlTabChecklist);
        _pnlContent.Controls.Add(_pnlTabActivity);

        // 5. Empty state
        _pnlEmptyState = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.White };
        var lblEmpty = new Label
        {
            Text = "Chưa chọn nội dung nào\r\n\r\nBấm vào một thẻ bất kỳ trên Board để xem chi tiết và thực hiện chuyển trạng thái.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
        };
        _pnlEmptyState.Controls.Add(lblEmpty);

        // Bố cục 4 hàng cố định bằng TableLayoutPanel (Header / Tab / Nội dung / Footer).
        // Không dùng Dock + thứ tự Add nữa vì dễ bị đảo hàng và để nội dung chui xuống dưới Header/Tab bar.
        var layoutRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UITheme.White,
        };
        layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));    // Header
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));    // Tab bar
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // Nội dung
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));    // Footer
        foreach (var part in new Control[] { _pnlHeader, _pnlTabBar, _pnlContent, _pnlFooter })
        {
            part.Dock = DockStyle.Fill;
            part.Margin = Padding.Empty;
        }
        layoutRoot.Controls.Add(_pnlHeader, 0, 0);
        layoutRoot.Controls.Add(_pnlTabBar, 0, 1);
        layoutRoot.Controls.Add(_pnlContent, 0, 2);
        layoutRoot.Controls.Add(_pnlFooter, 0, 3);

        Controls.Add(layoutRoot);
        Controls.Add(_pnlEmptyState);
        _pnlEmptyState.BringToFront();   // Empty state phủ lên trên khi chưa chọn thẻ

        ShowEmptyState();
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

    private void AddOverviewRow(Control ctl, int gapAfter)
    {
        _overviewHost.Controls.Add(ctl);
        _overviewRows.Add((ctl, gapAfter));
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
        int textW = Math.Max(40, _cboAssigneeEdit.Left - 74 - 10);
        TextRenderer.DrawText(g, name, UITheme.FontBodyBold, new Rectangle(74, 16, textW, 22), UITheme.Ink, tf);
        TextRenderer.DrawText(g, "Assignee", UITheme.FontLabel, new Rectangle(74, 38, textW, 20), UITheme.Neutral600, tf);
    }

    private string[] GetTabLabels()
    {
        int total = _chkChecklist?.Items.Count ?? 0;
        int done = _chkChecklist?.CheckedItems.Count ?? 0;
        return new[] { "Overview", "Script", $"Checklist ({done}/{total})", "Activity" };
    }

    // ---------- API công khai ----------

    public void Initialize(WorkflowService workflowService, IActivityRepository activityRepo, IProjectMemberRepository memberRepo, ContentService contentService)
    {
        _workflowService = workflowService;
        _activityRepo = activityRepo;
        _memberRepo = memberRepo;
        _contentService = contentService;

        _mnuPlatforms.Items.Clear();
        foreach (string name in contentService.GetAvailablePlatforms())
        {
            var item = new ToolStripMenuItem(name) { CheckOnClick = true };
            item.CheckedChanged += (_, _) =>
            {
                UpdatePlatformButtonText();
                MarkDirty();
            };
            _mnuPlatforms.Items.Add(item);
        }
    }

    private List<string> SelectedPlatformNames() =>
        _mnuPlatforms.Items.OfType<ToolStripMenuItem>().Where(i => i.Checked).Select(i => i.Text).ToList();

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
        _members = _memberRepo.GetMembers(card.ProjectId);

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

        _txtDurationEdit.Text = card.EstimatedDuration;

        // Script, loại nội dung và ngày dự kiến đăng không nằm trong thẻ Board nên đọc riêng từ DB khi mở Content Detail.
        var detail = TryLoadDetail(card.ContentId);
        _detailLoaded = detail != null;
        _txtScript.Text = detail?.Script ?? string.Empty;
        SelectContentType(detail?.ContentType);
        SetPlannedPublish(detail?.PlannedPublishAt);

        // Gán lại DataSource = null rồi DataSource mới đôi khi làm ComboBox quên mất DisplayMember đã đặt
        // (WinForms quirk), khiến nó tự hiện Object.ToString() — tức tên đầy đủ của class thay vì tên người.
        // Đặt lại DisplayMember tường minh ngay trước khi gán DataSource mới để chắc chắn không bị mất.
        _cboAssigneeEdit.DataSource = null;
        _cboAssigneeEdit.DisplayMember = "Name";
        _cboAssigneeEdit.DataSource = _members;
        var currentMember = _members.FirstOrDefault(m => m.UserId == card.AssigneeUserId);
        _cboAssigneeEdit.SelectedItem = currentMember;
        _leadName = currentMember?.Name ?? card.AssigneeName ?? "";
        _boxLead.Invalidate();

        RebuildActionButtons();
        LayoutOverview();
        _pnlTabOverview.AutoScrollPosition = new Point(0, 0);
        RefreshActivity();
        _pnlTabBar.Invalidate();

        _isLoading = false;
        _isDirty = false;
        _btnSave.Enabled = false;
        _lblSaveHint.Text = "";
    }

    /// <summary>Đọc chi tiết Content qua ContentService. Lỗi (không có quyền, không tìm thấy, lỗi DB) được báo cho người dùng và trả về null.</summary>
    private Content? TryLoadDetail(long contentId)
    {
        try
        {
            return _contentService!.GetDetail(contentId, CurrentSession.CurrentUserId);
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
        if (_isDirty && _current != null)
        {
            var result = MessageBox.Show(this,
                "Bạn có thay đổi chưa lưu. Đóng và bỏ qua thay đổi?",
                "Có thay đổi chưa lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return false;
        }

        _current = null;
        _isDirty = false;
        ShowEmptyState();
        return true;
    }

    /// <summary>true nếu có thay đổi chưa lưu — BoardForm dùng để hỏi trước khi reload/đóng.</summary>
    public bool HasUnsavedChanges => _isDirty;

    private void SaveChanges()
    {
        if (_current == null || _contentService == null) return;

        if (!_detailLoaded)
        {
            MessageBox.Show(this, "Chưa tải được chi tiết nội dung (script, loại nội dung, ngày dự kiến đăng) nên không thể lưu. Hãy đóng và mở lại nội dung này.",
                "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var draft = new ContentDraft
        {
            Title = _txtTitleVal.Text,
            Description = _txtHookVal.Text,
            Script = _txtScript.Text,
            ContentType = _cboTypeEdit.SelectedItem as string ?? string.Empty,
            PlannedPublishAt = _dtpPlannedPublish.Checked ? _dtpPlannedPublish.Value.Date : null,
            Priority = _cboPriorityEdit.SelectedItem is Priority p ? p : _current.Priority,
            Platforms = SelectedPlatformNames(),
            Sprint = _cboSprintEdit.SelectedItem as string ?? _current.Sprint,
            Deadline = _dtpTargetWeek.Value.Date,
            EstimatedDuration = _txtDurationEdit.Text,
            AssigneeUserId = (_cboAssigneeEdit.SelectedItem as ProjectMemberInfo)?.UserId,
        };

        try
        {
            _contentService.Update(_current.ContentId, draft, CurrentSession.CurrentUserId);
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
    }

    private void ShowEmptyState()
    {
        _pnlHeader.Visible = false;
        _pnlTabBar.Visible = false;
        _pnlContent.Visible = false;
        _pnlFooter.Visible = false;
        _pnlEmptyState.Visible = true;
        _activityTimeline.Clear();
    }

    private void SwitchTab(int tabIndex)
    {
        _selectedTabIndex = tabIndex;
        _pnlTabOverview.Visible = (tabIndex == 0);
        _pnlTabScript.Visible = (tabIndex == 1);
        _pnlTabChecklist.Visible = (tabIndex == 2);
        _pnlTabActivity.Visible = (tabIndex == 3);
        _pnlTabBar.Invalidate();
    }

    private void RefreshActivity()
    {
        if (_current == null) return;
        _activityTimeline.LoadActivity(_activityRepo!.GetActivity(_current.ContentId));
    }

    // ---------- Logic Workflow (GIỮ NGUYÊN) ----------

    private void RebuildActionButtons()
    {
        _flowActions.Controls.Clear();
        if (_current == null) return;

        var role = _memberRepo!.GetRole(_current.ProjectId, CurrentSession.CurrentUserId);
        var canReview = role == ProjectRole.Owner || role == ProjectRole.Manager;

        switch (_current.Status)
        {
            case ContentStatus.Editing:
                AddActionButton("Submit Review (Gửi kiểm duyệt)", RoundButtonStyle.Primary, OnSubmitReviewClicked);
                break;

            case ContentStatus.Review:
                if (canReview)
                {
                    AddActionButton("✓ Approve (Phê duyệt)", RoundButtonStyle.Primary, OnReviewDecisionClicked);
                    AddActionButton("✕ Reject (Từ chối)", RoundButtonStyle.Danger, OnReviewDecisionClicked);
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
        var button = new RoundedButton
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

    private void OnMoveNextClicked(object? sender, EventArgs e)
    {
        if (_current == null) return;
        var next = _workflowService!.GetNextSimpleStatus(_current.Status);
        if (!next.HasValue) return;

        RunWorkflowAction(() => _workflowService!.ChangeStatus(_current.ContentId, next.Value, CurrentSession.CurrentUserId));
    }

    private void OnSubmitReviewClicked(object? sender, EventArgs e)
    {
        if (_current == null) return;
        RunWorkflowAction(() => _workflowService!.SubmitForReview(_current.ContentId, CurrentSession.CurrentUserId));
    }

    private void OnReviewDecisionClicked(object? sender, EventArgs e)
    {
        if (_current == null) return;

        using var dialog = new Forms.Board.ReviewDecisionDialog(
            $"{_current.Code} — {_current.Title}",
            "Xem chi tiết Review trong Tab Activity.");

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var contentId = _current.ContentId;
        if (dialog.Approved)
        {
            RunWorkflowAction(() => _workflowService!.ApproveReview(contentId, CurrentSession.CurrentUserId, dialog.Feedback));
        }
        else
        {
            RunWorkflowAction(() => _workflowService!.RejectReview(contentId, CurrentSession.CurrentUserId, dialog.Feedback));
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
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ---------- Header vẽ tay: [Mã] [Nền tảng] [Sprint] ........ [X] ----------
    private sealed class HeaderPanel : Panel
    {
        private string _code = "";
        private IReadOnlyList<string> _platforms = Array.Empty<string>();
        private string _sprint = "";
        private bool _closeHover;
        private Rectangle _closeRect;

        public event EventHandler? CloseClicked;

        public HeaderPanel()
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
    private sealed class TabBarPanel : Panel
    {
        private const int Gap = 28;
        private readonly Func<string[]> _getLabels;
        private readonly Func<int> _getSelectedTab;
        private readonly Action<int> _onSelectTab;

        public TabBarPanel(Func<string[]> getLabels, Func<int> getSelectedTab, Action<int> onSelectTab)
        {
            _getLabels = getLabels;
            _getSelectedTab = getSelectedTab;
            _onSelectTab = onSelectTab;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = UITheme.White;
            Cursor = Cursors.Hand;
        }

        private List<Rectangle> GetRects(Graphics g, string[] labels)
        {
            var list = new List<Rectangle>();
            int x = PadX;
            for (int i = 0; i < labels.Length; i++)
            {
                var font = i == _getSelectedTab() ? UITheme.FontBodyBold : UITheme.FontBody;
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
            var rects = GetRects(g, _getLabels());
            for (int i = 0; i < rects.Count; i++)
            {
                var hit = Rectangle.Inflate(rects[i], Gap / 2, 0);
                if (hit.Contains(e.Location)) { _onSelectTab(i); return; }
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

            var labels = _getLabels();
            var rects = GetRects(g, labels);
            int selected = _getSelectedTab();

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
}