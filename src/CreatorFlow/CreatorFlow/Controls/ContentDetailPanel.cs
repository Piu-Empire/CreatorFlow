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

    private ContentBoardCard? _current;
    private int _selectedTabIndex = 0; // 0: Overview, 1: Script, 2: Checklist, 3: Activity

    public event EventHandler? ContentChanged;
    public event EventHandler? CloseRequested;

    // Khung chính
    private readonly HeaderPanel _pnlHeader;
    private readonly TabBarPanel _pnlTabBar;
    private readonly Panel _pnlContent;
    private readonly Panel _pnlFooter;
    private readonly Label _lblFooterCode;
    private readonly RoundedButton _btnDone;

    // Tab 1: Overview
    private readonly Panel _pnlTabOverview;
    private readonly Panel _overviewHost;
    private readonly List<(Control Ctl, int GapAfter)> _overviewRows = new();
    private readonly Label _lblTitleVal;
    private readonly Label _lblHookVal;
    private readonly RoundedPanel _boxMeta;
    private readonly Label[] _metaCaptions = new Label[6];
    private readonly RoundedPanel[] _metaBoxes = new RoundedPanel[6];
    private readonly Label[] _metaValues = new Label[6];
    private readonly RoundedPanel _boxLead;
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
            Size = new Size(260, 80),
            BackColor = Color.Transparent,
        };
        _btnDone = new RoundedButton
        {
            Text = "Done & Close",
            Style = RoundButtonStyle.Primary,
            Size = new Size(140, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _btnDone.Location = new Point(_pnlFooter.Width - PadX - _btnDone.Width, 18);
        _btnDone.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        _pnlFooter.Controls.Add(_lblFooterCode);
        _pnlFooter.Controls.Add(_btnDone);
        _pnlFooter.Resize += (_, _) => _btnDone.Location = new Point(_pnlFooter.Width - PadX - _btnDone.Width, 18);

        // 4. Vùng nội dung
        _pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.White };

        // ---- TAB 1: OVERVIEW ----
        _pnlTabOverview = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UITheme.White };
        _overviewHost = new Panel { Location = new Point(0, 0), BackColor = UITheme.White };
        _pnlTabOverview.Controls.Add(_overviewHost);

        AddOverviewRow(MakeSectionLabel("PRODUCTION TITLE"), 6);
        _lblTitleVal = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = UITheme.Ink,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 16, 0),
            AutoEllipsis = true,
            BackColor = Color.Transparent,
        };
        AddOverviewRow(MakeBox(60, _lblTitleVal), 20);

        AddOverviewRow(MakeSectionLabel("CONCEPT HOOK"), 6);
        _lblHookVal = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = UITheme.Ink,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(16, 14, 16, 12),
            BackColor = Color.Transparent,
        };
        AddOverviewRow(MakeBox(104, _lblHookVal), 20);

        _boxMeta = new RoundedPanel { Height = 18 + 3 * 78 + 6, FillColor = UITheme.Neutral50, Radius = 10 };
        string[] captions = { "Pipeline Stage", "Platform", "Priority", "Sprint Allocation", "Target Week", "Estimated Duration" };
        bool[] chevrons = { true, true, true, true, false, false };
        for (int i = 0; i < 6; i++)
        {
            _metaCaptions[i] = new Label
            {
                Text = captions[i],
                Font = UITheme.FontLabelBold,
                ForeColor = UITheme.Neutral700,
                AutoSize = false,
                Height = 18,
                BackColor = Color.Transparent,
            };
            _metaValues[i] = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                ForeColor = UITheme.Ink,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 36, 0),
                AutoEllipsis = true,
                BackColor = Color.Transparent,
            };
            _metaBoxes[i] = new RoundedPanel { Height = 44, ShowChevron = chevrons[i] };
            _metaBoxes[i].Controls.Add(_metaValues[i]);
            _boxMeta.Controls.Add(_metaCaptions[i]);
            _boxMeta.Controls.Add(_metaBoxes[i]);
        }
        AddOverviewRow(_boxMeta, 20);

        AddOverviewRow(MakeSectionLabel("DIRECTOR & CREATOR LEAD"), 6);
        _boxLead = new RoundedPanel { Height = 76, FillColor = UITheme.Neutral50, Radius = 10 };
        _boxLead.Paint += PaintLeadCard;
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
            Text = "Kịch bản / Screenplay Notes:\r\n\r\n[Hook 0-3s]: Giới thiệu điểm ấn tượng nhất của sản phẩm.\r\n[Body 3-30s]: Chi tiết tính năng nổi bật + góc quay cận.\r\n[Call To Action]: Đăng ký và theo dõi Studio!",
        };
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

        // Thứ tự Add: control Add SAU CÙNG được dock TRƯỚC (Top/Bottom trước, Fill sau cùng)
        Controls.Add(_pnlContent);
        Controls.Add(_pnlFooter);
        Controls.Add(_pnlTabBar);
        Controls.Add(_pnlHeader);
        Controls.Add(_pnlEmptyState);

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
        BackColor = Color.Transparent,
    };

    private static RoundedPanel MakeBox(int height, Control inner)
    {
        var box = new RoundedPanel { Height = height };
        box.Controls.Add(inner);
        return box;
    }

    private void AddOverviewRow(Control ctl, int gapAfter)
    {
        _overviewHost.Controls.Add(ctl);
        _overviewRows.Add((ctl, gapAfter));
    }

    /// <summary>Xếp các dòng của tab Overview theo chiều dọc (đơn giản, không phụ thuộc AutoSize của TableLayoutPanel).</summary>
    private void LayoutOverview()
    {
        int w = _pnlTabOverview.ClientSize.Width - PadX * 2;
        if (w < 240) return;

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
        for (int i = 0; i < 6; i++)
        {
            int x = 20 + (i % 2) * (colW + 16);
            int cy = 18 + (i / 2) * 78;
            _metaCaptions[i].SetBounds(x, cy, colW, 18);
            _metaBoxes[i].SetBounds(x, cy + 22, colW, 44);
        }
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
        TextRenderer.DrawText(g, name, UITheme.FontBodyBold, new Rectangle(74, 16, _boxLead.Width - 90, 22), UITheme.Ink, tf);
        TextRenderer.DrawText(g, "Assignee", UITheme.FontLabel, new Rectangle(74, 38, _boxLead.Width - 90, 20), UITheme.Neutral600, tf);
    }

    private string[] GetTabLabels()
    {
        int total = _chkChecklist?.Items.Count ?? 0;
        int done = _chkChecklist?.CheckedItems.Count ?? 0;
        return new[] { "Overview", "Script", $"Checklist ({done}/{total})", "Activity" };
    }

    // ---------- API công khai ----------

    public void Initialize(WorkflowService workflowService, IActivityRepository activityRepo, IProjectMemberRepository memberRepo)
    {
        _workflowService = workflowService;
        _activityRepo = activityRepo;
        _memberRepo = memberRepo;
    }

    public void LoadContent(ContentBoardCard card)
    {
        if (_workflowService == null || _activityRepo == null || _memberRepo == null)
            throw new InvalidOperationException("Phải gọi Initialize() trước khi LoadContent().");

        _current = card;

        _pnlEmptyState.Visible = false;
        _pnlHeader.Visible = true;
        _pnlTabBar.Visible = true;
        _pnlContent.Visible = true;
        _pnlFooter.Visible = true;

        string platform = card.Platforms.Count > 0 ? card.Platforms[0] : "";
        _pnlHeader.SetData(card.Code, platform, "Sprint 25");
        _lblFooterCode.Text = card.Code;

        _lblTitleVal.Text = card.Title;
        if (string.IsNullOrWhiteSpace(card.Description))
        {
            _lblHookVal.Text = "Chưa có mô tả / concept hook.";
            _lblHookVal.ForeColor = UITheme.Neutral400;
        }
        else
        {
            _lblHookVal.Text = card.Description;
            _lblHookVal.ForeColor = UITheme.Ink;
        }

        _metaValues[0].Text = UITheme.GetStageDisplayName(card.Status);
        _metaValues[1].Text = card.Platforms.Count > 0 ? string.Join(" • ", card.Platforms) : "—";
        _metaValues[2].Text = card.Priority switch { Priority.High => "High", Priority.Medium => "Med", _ => "Low" };
        _metaValues[3].Text = "Sprint 25";
        _metaValues[4].Text = card.Deadline.HasValue
            ? $"{card.Deadline.Value:MMM} W{(card.Deadline.Value.Day - 1) / 7 + 1}"
            : "—";
        _metaValues[4].ForeColor = card.IsOverdue ? UITheme.Danger : UITheme.Ink;
        _metaValues[5].Text = "—";

        _leadName = card.AssigneeName ?? "";
        _boxLead.Invalidate();

        RebuildActionButtons();
        LayoutOverview();
        _pnlTabOverview.AutoScrollPosition = new Point(0, 0);
        RefreshActivity();
        _pnlTabBar.Invalidate();
    }

    public void Clear()
    {
        _current = null;
        ShowEmptyState();
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
        private string _platform = "";
        private string _sprint = "";
        private bool _closeHover;
        private Rectangle _closeRect;

        public event EventHandler? CloseClicked;

        public HeaderPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = UITheme.White;
        }

        public void SetData(string code, string platform, string sprint)
        {
            _code = code; _platform = platform; _sprint = sprint;
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

            // Pill nền tảng
            if (!string.IsNullOrEmpty(_platform))
            {
                int pw = UIIcons.MeasurePlatformPill(g, _platform, 34);
                var pr = new Rectangle(x, cy - 17, pw, 34);
                UIIcons.DrawPlatformPill(g, _platform, pr);
                x = pr.Right + 10;
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
