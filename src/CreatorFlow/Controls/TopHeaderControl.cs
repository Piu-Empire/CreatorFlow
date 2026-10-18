using System.Drawing.Drawing2D;
using CreatorFlow.Contracts.Search;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Thanh trên cùng: breadcrumb và toolbar chung; avatar mở Account ở góc phải.
/// </summary>
public class TopHeaderControl : UserControl, IMessageFilter
{
    private const int PadX = 32;
    private const int WmLeftButtonDown = 0x0201;
    private const int WmRightButtonDown = 0x0204;
    private const int WmMiddleButtonDown = 0x0207;
    private readonly Button _accountButton;
    private readonly ImeAwareTextBox _globalSearchBox;
    private readonly System.Windows.Forms.Timer _globalSearchTimer = new() { Interval = 650 };
    private Panel? _globalSearchPopup;
    private TableLayoutPanel? _globalSearchPopupLayout;
    private FlowLayoutPanel? _globalSearchResultsPanel;
    private Label? _globalSearchStatus;
    private readonly List<Button> _globalSearchResultButtons = new();
    private Rectangle _globalSearchRect;
    private bool _globalSearchEnabled;
    private bool _globalSearchOutsideClickFilterAdded;
    private string _globalSearchQuery = string.Empty;
    private string _accountName = string.Empty;
    private Image? _accountAvatar;
    private bool _accountHovered;
    public event EventHandler? ProfileRequested;

    private string _statsText = "";
    private Rectangle _newContentRect;
    private bool _newContentHover;
    private string _authenticatedPage = "Board";
    private string _projectName = "Chưa chọn dự án";
    private bool _projectActionsEnabled;

    public event EventHandler<GlobalSearchQueryEventArgs>? GlobalSearchRequested;
    public event EventHandler<GlobalSearchResultEventArgs>? GlobalSearchResultSelected;

    public string GlobalSearchText => _globalSearchBox.Text;

    public void SetGlobalSearchEnabled(bool enabled)
    {
        _globalSearchEnabled = enabled;
        _globalSearchBox.Enabled = enabled;
        _globalSearchBox.PlaceholderText = enabled
            ? "Tìm Project, Content, ý tưởng, công việc"
            : "Tìm kiếm toàn cục cần đăng nhập";
        if (!enabled) CloseGlobalSearchPopup();
        LayoutGlobalSearchBox();
        Invalidate();
    }

    public void SetProjectContext(string? projectName, bool actionsEnabled)
    {
        _projectName = string.IsNullOrWhiteSpace(projectName) ? "Chưa chọn dự án" : projectName;
        _projectActionsEnabled = actionsEnabled;
        _newContentHover = false;
        Cursor = Cursors.Default;
        LayoutGlobalSearchBox();
        Invalidate();
    }

    public void SetAuthenticatedPage(string page)
    {
        _authenticatedPage = page;
        LayoutGlobalSearchBox();
        Invalidate();
    }

    public void ClearStats() { _statsText = string.Empty; Invalidate(); }

    public event EventHandler? NewContentClicked;

    public TopHeaderControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        Height = 68;
        BackColor = UITheme.White;
        _accountButton = new Button { Name = "authenticatedAccount", Text = string.Empty,
            Bounds = new Rectangle(Math.Max(0, Width - PadX - 40), 14, 40, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.White, UseVisualStyleBackColor = false, Cursor = Cursors.Hand,
            Visible = false, TabIndex = 0, AccessibleRole = AccessibleRole.PushButton };
        _accountButton.FlatAppearance.BorderSize = 0;
        _accountButton.Paint += PaintAccount;
        _accountButton.MouseEnter += (_, _) => { _accountHovered = true; _accountButton.Invalidate(); };
        _accountButton.MouseLeave += (_, _) => { _accountHovered = false; _accountButton.Invalidate(); };
        _accountButton.GotFocus += (_, _) => _accountButton.Invalidate();
        _accountButton.LostFocus += (_, _) => _accountButton.Invalidate();
        _accountButton.Click += (_, _) => ProfileRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(_accountButton);

        _globalSearchBox = new ImeAwareTextBox
        {
            Name = "globalSearch",
            BorderStyle = BorderStyle.None,
            BackColor = UITheme.Neutral50,
            ForeColor = UITheme.Ink,
            Font = UITheme.FontBody,
            Enabled = false,
            MaxLength = 100,
            TabIndex = 1,
            AccessibleName = "Tìm kiếm toàn cục",
            PlaceholderText = "Tìm kiếm toàn cục cần đăng nhập"
        };
        _globalSearchBox.TextChanged += GlobalSearchBox_TextChanged;
        _globalSearchBox.KeyDown += GlobalSearchBox_KeyDown;
        _globalSearchBox.ImeCompositionStarted += (_, _) =>
        {
            _globalSearchTimer.Stop();
            _globalSearchQuery = string.Empty;
            CloseGlobalSearchPopup();
        };
        _globalSearchBox.ImeCompositionEnded += (_, _) => GlobalSearchBox_TextChanged(_globalSearchBox, EventArgs.Empty);
        _globalSearchBox.GotFocus += (_, _) =>
        {
            if (_globalSearchBox.Text.Trim().Length >= 2) _globalSearchTimer.Start();
        };
        _globalSearchTimer.Tick += (_, _) =>
        {
            _globalSearchTimer.Stop();
            RequestGlobalSearch(_globalSearchBox.Text.Trim());
        };
        Controls.Add(_globalSearchBox);
        LayoutGlobalSearchBox();
    }

    private void GlobalSearchBox_TextChanged(object? sender, EventArgs e)
    {
        _globalSearchTimer.Stop();
        if (!_globalSearchEnabled) return;

        if (_globalSearchBox.IsImeComposing)
        {
            _globalSearchQuery = string.Empty;
            CloseGlobalSearchPopup();
            return;
        }

        string query = _globalSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            _globalSearchQuery = string.Empty;
            CloseGlobalSearchPopup();
            return;
        }
        if (query.Length < 2)
        {
            _globalSearchQuery = query;
            CloseGlobalSearchPopup();
            return;
        }

        _globalSearchQuery = query;
        _globalSearchTimer.Start();
    }

    private void GlobalSearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && _globalSearchResultButtons.Count > 0)
        {
            _globalSearchResultButtons[0].Focus();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            _globalSearchTimer.Stop();
            if (_globalSearchResultButtons.Count > 0)
                _globalSearchResultButtons[0].PerformClick();
            else if (_globalSearchBox.Text.Trim().Length >= 2)
                RequestGlobalSearch(_globalSearchBox.Text.Trim());
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _globalSearchQuery = string.Empty;
            CloseGlobalSearchPopup();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void RequestGlobalSearch(string query)
    {
        if (!_globalSearchEnabled || query.Length < 2 || IsDisposed) return;
        _globalSearchQuery = query;
        SetGlobalSearchStatus(query, "Đang tìm kiếm…");
        GlobalSearchRequested?.Invoke(this, new GlobalSearchQueryEventArgs(query));
    }

    public void SetGlobalSearchResults(string query, IReadOnlyList<GlobalSearchItemResponse> results)
    {
        if (!IsCurrentGlobalSearch(query)) return;
        ArgumentNullException.ThrowIfNull(results);
        EnsureGlobalSearchPopup();
        ClearGlobalSearchRows();
        _globalSearchStatus!.Text = results.Count == 0
            ? "Không tìm thấy kết quả phù hợp."
            : $"{results.Count} kết quả · nhấn Enter để mở kết quả đầu tiên";
        _globalSearchStatus.ForeColor = UITheme.Neutral600;
        _globalSearchStatus.Visible = true;

        foreach (GlobalSearchItemResponse result in results)
        {
            var button = new Button
            {
                Name = $"globalSearchResult_{result.Type}_{result.EntityId}",
                Text = $"{result.Title}\r\n{TypeLabel(result.Type)} · {result.ProjectName} · {result.Subtitle}",
                Tag = result,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = UITheme.FontBody,
                ForeColor = UITheme.Ink,
                BackColor = UITheme.White,
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Height = 62,
                Margin = new Padding(0, 0, 0, 4),
                Padding = new Padding(10, 4, 8, 4),
                Width = Math.Max(260, Math.Clamp(_globalSearchRect.Width, 280, 440) - 20),
                AccessibleName = $"{TypeLabel(result.Type)}: {result.Title}, {result.ProjectName}"
            };
            button.FlatAppearance.BorderColor = UITheme.Neutral200;
            button.FlatAppearance.MouseOverBackColor = UITheme.Neutral50;
            button.Click += (_, _) =>
            {
                GlobalSearchResultSelected?.Invoke(this, new GlobalSearchResultEventArgs(result));
                if (IsHandleCreated) BeginInvoke(new Action(ClearGlobalSearch));
                else ClearGlobalSearch();
            };
            button.KeyDown += (_, e) =>
            {
                int index = _globalSearchResultButtons.IndexOf(button);
                if (e.KeyCode == Keys.Down && index + 1 < _globalSearchResultButtons.Count)
                {
                    _globalSearchResultButtons[index + 1].Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Up)
                {
                    if (index > 0) _globalSearchResultButtons[index - 1].Focus();
                    else _globalSearchBox.Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    CloseGlobalSearchPopup();
                    _globalSearchBox.Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            _globalSearchResultButtons.Add(button);
            _globalSearchResultsPanel!.Controls.Add(button);
        }

        OpenGlobalSearchPopup();
    }

    public void SetGlobalSearchError(string query, string message)
    {
        if (!IsCurrentGlobalSearch(query)) return;
        SetGlobalSearchStatus(query, string.IsNullOrWhiteSpace(message)
            ? "Không thể tìm kiếm lúc này. Hãy thử lại."
            : message);
        if (_globalSearchStatus is not null) _globalSearchStatus.ForeColor = UITheme.Danger;
    }

    public void ClearGlobalSearch()
    {
        _globalSearchTimer.Stop();
        _globalSearchQuery = string.Empty;
        _globalSearchBox.Clear();
        CloseGlobalSearchPopup();
    }

    private bool IsCurrentGlobalSearch(string query) =>
        !IsDisposed && _globalSearchEnabled && query.Equals(_globalSearchQuery, StringComparison.Ordinal) &&
        query.Equals(_globalSearchBox.Text.Trim(), StringComparison.Ordinal);

    private void SetGlobalSearchStatus(string query, string message)
    {
        if (!_globalSearchEnabled || query.Length == 0) return;
        _globalSearchQuery = query;
        EnsureGlobalSearchPopup();
        ClearGlobalSearchRows();
        _globalSearchStatus!.Text = message;
        _globalSearchStatus.ForeColor = UITheme.Neutral600;
        _globalSearchStatus.Visible = true;
        OpenGlobalSearchPopup();
    }

    private void EnsureGlobalSearchPopup()
    {
        if (_globalSearchPopup is { IsDisposed: false }) return;

        int width = Math.Clamp(_globalSearchRect.Width, 280, 440);
        var surface = new Panel
        {
            Name = "globalSearchPopup",
            BackColor = UITheme.White,
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(width, 360),
            Padding = new Padding(1),
            Visible = false
        };
        _globalSearchPopupLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.White,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _globalSearchPopupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _globalSearchPopupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _globalSearchPopupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _globalSearchStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 2, 8, 2),
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            BackColor = UITheme.White,
            AutoEllipsis = true,
            Text = "Đang tìm kiếm…"
        };
        _globalSearchResultsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(6),
            Margin = Padding.Empty,
            BackColor = UITheme.White
        };
        _globalSearchPopupLayout.Controls.Add(_globalSearchStatus, 0, 0);
        _globalSearchPopupLayout.Controls.Add(_globalSearchResultsPanel, 0, 1);
        surface.Controls.Add(_globalSearchPopupLayout);
        _globalSearchPopup = surface;
    }

    private void OpenGlobalSearchPopup()
    {
        if (_globalSearchPopup is null || !_globalSearchBox.Visible || _globalSearchRect.IsEmpty || Parent is null) return;
        if (!ReferenceEquals(_globalSearchPopup.Parent, Parent))
        {
            _globalSearchPopup.Parent?.Controls.Remove(_globalSearchPopup);
            Parent.Controls.Add(_globalSearchPopup);
        }
        PositionGlobalSearchPopup();
        _globalSearchPopup.Visible = true;
        _globalSearchPopup.BringToFront();
        if (!_globalSearchOutsideClickFilterAdded)
        {
            Application.AddMessageFilter(this);
            _globalSearchOutsideClickFilterAdded = true;
        }
    }

    private void PositionGlobalSearchPopup()
    {
        if (_globalSearchPopup is null || Parent is null || _globalSearchRect.IsEmpty) return;
        int width = Math.Clamp(_globalSearchRect.Width, 280, 440);
        _globalSearchPopup.Size = new Size(width, 360);
        Point screenLocation = PointToScreen(new Point(_globalSearchRect.X, _globalSearchRect.Bottom + 4));
        _globalSearchPopup.Location = Parent.PointToClient(screenLocation);
    }

    private void ClearGlobalSearchRows()
    {
        _globalSearchResultButtons.Clear();
        if (_globalSearchResultsPanel is null) return;
        foreach (Control control in _globalSearchResultsPanel.Controls.Cast<Control>().ToArray())
        {
            _globalSearchResultsPanel.Controls.Remove(control);
            control.Dispose();
        }
    }

    private void CloseGlobalSearchPopup()
    {
        Panel? popup = _globalSearchPopup;
        _globalSearchPopup = null;
        _globalSearchPopupLayout = null;
        _globalSearchResultsPanel = null;
        _globalSearchStatus = null;
        _globalSearchResultButtons.Clear();
        if (_globalSearchOutsideClickFilterAdded)
        {
            Application.RemoveMessageFilter(this);
            _globalSearchOutsideClickFilterAdded = false;
        }
        if (popup is null) return;
        popup.Parent?.Controls.Remove(popup);
        popup.Dispose();
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (_globalSearchPopup is not { Visible: true } popup ||
            m.Msg is not (WmLeftButtonDown or WmRightButtonDown or WmMiddleButtonDown))
            return false;

        Point point = Cursor.Position;
        Rectangle searchBounds = _globalSearchBox.RectangleToScreen(_globalSearchBox.ClientRectangle);
        Rectangle popupBounds = popup.RectangleToScreen(popup.ClientRectangle);
        if (!searchBounds.Contains(point) && !popupBounds.Contains(point)) CloseGlobalSearchPopup();
        return false;
    }

    private static string TypeLabel(string type) => type switch
    {
        "Project" => "Project",
        "Content" => "Content",
        "Idea" => "Ý tưởng",
        "Task" => "Công việc được giao",
        _ => type
    };

    private void LayoutGlobalSearchBox()
    {
        if (IsDisposed || !IsHandleCreated) return;
        _globalSearchRect = CalculateGlobalSearchRectangle();
        bool visible = !_globalSearchRect.IsEmpty;
        _globalSearchBox.Visible = visible;
        if (!visible) CloseGlobalSearchPopup();
        if (visible)
        {
            _globalSearchBox.Bounds = new Rectangle(_globalSearchRect.X + 40, _globalSearchRect.Y + 9,
                Math.Max(0, _globalSearchRect.Width - 52), 22);
            PositionGlobalSearchPopup();
        }
    }

    private Rectangle CalculateGlobalSearchRectangle()
    {
        int right = Width - PadX - (_accountButton.Visible ? 40 + 20 : 0);
        right -= 26 + 20;
        const int btnW = 156;
        int newContentX = right - btnW;
        right = newContentX - 24;
        int x = PadX;
        string[] crumbs = { "CreatorFlow", _projectName, _authenticatedPage };
        using var bodyFont = new Font("Segoe UI", 10F);
        using var bodyBold = new Font("Segoe UI", 10F, FontStyle.Bold);
        for (int i = 0; i < crumbs.Length; i++)
        {
            Font font = i == crumbs.Length - 1 ? bodyBold : bodyFont;
            int availableWidth = Math.Max(0, right - x - (crumbs.Length - i - 1) * 28);
            int measured = TextRenderer.MeasureText(crumbs[i], font, new Size(int.MaxValue, 24),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            x += Math.Min(availableWidth, measured) + 8;
            if (i < crumbs.Length - 1) x += 20 + 8;
        }

        int searchX = x + 56;
        int searchWidth = Math.Min(400, right - searchX);
        return searchWidth >= 200 ? new Rectangle(searchX, Height / 2 - 20, searchWidth, 40) : Rectangle.Empty;
    }

    // The shell transfers ownership of its decoded avatar to this control.
    public void SetAuthenticatedAccount(string name, Image? avatar)
    {
        _accountName = name;
        Image? previous = _accountAvatar;
        _accountAvatar = avatar;
        if (!ReferenceEquals(previous, avatar)) previous?.Dispose();
        _accountButton.AccessibleName = $"Mở hồ sơ cá nhân của {name}";
        _accountButton.Visible = true;
        _accountButton.Invalidate();
        Invalidate();
    }

    private void PaintAccount(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(_accountHovered ? UITheme.Neutral50 : UITheme.White);
        var bounds = new Rectangle(4, 4, 32, 32);
        if (_accountAvatar is null)
        {
            string[] words = _accountName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = words.Length == 0 ? "?" :
                (System.Globalization.StringInfo.GetNextTextElement(words[0]) +
                 (words.Length > 1 ? System.Globalization.StringInfo.GetNextTextElement(words[^1]) : string.Empty)).ToUpperInvariant();
            UITheme.DrawAvatar(g, initials, bounds, UITheme.Black, UITheme.White, UITheme.FontLabelBold);
        }
        else
        {
            var state = g.Save();
            using var circle = new GraphicsPath();
            circle.AddEllipse(bounds);
            g.SetClip(circle);
            g.DrawImage(_accountAvatar, bounds);
            g.Restore(state);
        }
        using var border = new Pen(UITheme.Neutral200);
        g.DrawEllipse(border, bounds);
        if (_accountButton.Focused)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(1, 1, 37, 37), UITheme.Black, UITheme.White);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _globalSearchTimer.Stop();
            _globalSearchTimer.Dispose();
            CloseGlobalSearchPopup();
            _accountAvatar?.Dispose();
            _accountAvatar = null;
        }
        base.Dispose(disposing);
    }

    private sealed class ImeAwareTextBox : TextBox
    {
        private const int WmImeStartComposition = 0x010D;
        private const int WmImeEndComposition = 0x010E;

        public bool IsImeComposing { get; private set; }
        public event EventHandler? ImeCompositionStarted;
        public event EventHandler? ImeCompositionEnded;

        protected override void WndProc(ref Message m)
        {
            bool started = m.Msg == WmImeStartComposition;
            bool ended = m.Msg == WmImeEndComposition;
            if (started)
            {
                IsImeComposing = true;
                ImeCompositionStarted?.Invoke(this, EventArgs.Empty);
            }

            base.WndProc(ref m);

            if (ended)
            {
                IsImeComposing = false;
                ImeCompositionEnded?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void UpdateStats(int totalCards, int overdueCount)
    {
        _statsText = overdueCount > 0
            ? $"{totalCards} items · {overdueCount} overdue"
            : $"{totalCards} items in production";
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hover = _projectActionsEnabled && _newContentRect.Contains(e.Location);
        if (hover != _newContentHover)
        {
            _newContentHover = hover;
            Cursor = hover ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_newContentHover) { _newContentHover = false; Cursor = Cursors.Default; Invalidate(); }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && _projectActionsEnabled && _newContentRect.Contains(e.Location))
            NewContentClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(UITheme.White);

        using (var borderPen = new Pen(UITheme.Neutral200, 1f))
            g.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);

        int cy = Height / 2;
        using var bodyFont = new Font("Segoe UI", 10F);
        using var bodyBold = new Font("Segoe UI", 10F, FontStyle.Bold);
        const TextFormatFlags tf = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        // Reserve the rightmost position for the authenticated avatar.
        int right = Width - PadX - (_accountButton.Visible ? 40 + 20 : 0);
        var bellRect = new Rectangle(right - 26, cy - 13, 26, 26);
        UIIcons.Bell(g, bellRect, UITheme.Neutral600, 1.8f);
        right -= 26 + 20;
        // Nút + New Content
        const int btnW = 156, btnH = 42;
        _newContentRect = new Rectangle(right - btnW, cy - btnH / 2, btnW, btnH);
        using (var btnBrush = new SolidBrush(!_projectActionsEnabled ? UITheme.Neutral200 :
            _newContentHover ? UITheme.Neutral800 : UITheme.Black))
        using (var btnPath = UITheme.CreateRoundedRectanglePath(_newContentRect, 8))
            g.FillPath(btnBrush, btnPath);
        int tW = TextRenderer.MeasureText(g, "New Content", UITheme.FontBodyBold, new Size(int.MaxValue, 20), TextFormatFlags.NoPadding).Width;
        int contentX = _newContentRect.X + (btnW - (18 + 10 + tW)) / 2;
        Color actionColor = _projectActionsEnabled ? UITheme.White : UITheme.Neutral600;
        UIIcons.Plus(g, new Rectangle(contentX, cy - 9, 18, 18), actionColor, 2f);
        TextRenderer.DrawText(g, "New Content", UITheme.FontBodyBold, new Rectangle(contentX + 28, _newContentRect.Y, tW + 6, btnH), actionColor,
            tf | TextFormatFlags.SingleLine);
        right = _newContentRect.X - 24;

        // ===== Trái → phải =====
        int x = PadX;
        string[] crumbs = { "CreatorFlow", _projectName, _authenticatedPage };
        for (int i = 0; i < crumbs.Length; i++)
        {
            bool last = i == crumbs.Length - 1;
            var font = last ? bodyBold : bodyFont;
            int availableWidth = Math.Max(0, right - x - (crumbs.Length - i - 1) * 28);
            int w = Math.Min(availableWidth, TextRenderer.MeasureText(g, crumbs[i], font, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding).Width);
            TextRenderer.DrawText(g, crumbs[i], font, new Rectangle(x, cy - 12, w, 24), last ? UITheme.Ink : UITheme.Neutral600,
                tf | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            x += w + 8;
            if (!last)
            {
                if (x + 20 <= right)
                    UIIcons.Chevron(g, new Rectangle(x, cy - 10, 20, 20), ChevronDir.Right, UITheme.Neutral400, 1.6f);
                x += 20 + 8;
            }
        }

        // Ô tìm kiếm toàn cục dùng TextBox thật; phần nền và icon vẫn theo visual header.
        _globalSearchRect = CalculateGlobalSearchRectangle();
        LayoutGlobalSearchBox();
        if (!_globalSearchRect.IsEmpty)
        {
            var sr = _globalSearchRect;
            using (var sBrush = new SolidBrush(UITheme.Neutral50))
            using (var sPen = new Pen(UITheme.Neutral200, 1f))
            using (var sPath = UITheme.CreateRoundedRectanglePath(new Rectangle(sr.X, sr.Y, sr.Width - 1, sr.Height - 1), 8))
            {
                g.FillPath(sBrush, sPath);
                g.DrawPath(sPen, sPath);
            }
            UIIcons.Search(g, new Rectangle(sr.X + 12, sr.Y + 11, 18, 18), UITheme.Neutral400);
        }
    }
}

public sealed class GlobalSearchQueryEventArgs(string query) : EventArgs
{
    public string Query { get; } = query;
}

public sealed class GlobalSearchResultEventArgs(GlobalSearchItemResponse result) : EventArgs
{
    public GlobalSearchItemResponse Result { get; } = result;
}
