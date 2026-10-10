using CreatorFlow.Contracts.Admin;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Màn hình quản trị người dùng theo style guide: header card, grid pill, pager.</summary>
public class AdminUserManagementControl : UserControl
{
    private const int PageSize = 20;

    private readonly AuthApiFacade _auth;
    private readonly ToastNotification? _toast;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly RoundedPanel _headerCard = new();
    private readonly RoundedPanel _searchBox = new();
    private readonly TextBox _txtSearch = new();
    private readonly SegmentedControl _segStatus = new();
    private readonly RoundedButton _btnRefresh = new();
    private readonly RoundedPanel _gridCard = new();
    private readonly DataGridView _grid = new();
    private readonly Panel _statePanel = new();
    private readonly Label _stateTitle = new();
    private readonly Label _stateSub = new();
    private readonly RoundedButton _stateButton = new();
    private readonly Panel _pager = new();
    private readonly Label _summaryLabel = new();
    private readonly RoundedButton _btnPrev = new();
    private readonly RoundedButton _btnNext = new();
    private readonly Label _pageLabel = new();
    private List<AdminUserSummaryResponse> _rows = new();
    private int _totalCount;
    private int _page;
    private int _hoverRow = -1;
    private bool _busy;
    private string _stateAction = string.Empty;

    public AdminUserManagementControl(AuthApiFacade auth, ToastNotification? toast = null)
    {
        _auth = auth;
        _toast = toast;
        BackColor = UITheme.Neutral50;
        Dock = DockStyle.Fill;
        Padding = new Padding(24, 20, 24, 20);
        BuildHeader();
        BuildGrid();
        BuildPager();
        Controls.Add(_pager);
        Controls.Add(_headerCard);
        Controls.Add(_gridCard);
    }

    // ---------- Header card (mục 26 style guide) ----------

    private void BuildHeader()
    {
        _headerCard.FillColor = UITheme.White;
        _headerCard.BorderColor = UITheme.Neutral200;
        _headerCard.Radius = 12;
        _headerCard.Dock = DockStyle.Top;
        _headerCard.Height = 168;
        _headerCard.Margin = new Padding(0, 0, 0, 12);

        var title = new Label
        {
            Text = "Quản trị người dùng",
            Font = UITheme.FontPageTitle,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(20, 12),
        };
        int titleWidth = TextRenderer.MeasureText(title.Text, title.Font).Width;
        var badge = new BadgeLabel("System Admin", UITheme.Neutral800, UITheme.White)
        {
            Location = new Point(20 + titleWidth + 12, 22),
        };
        var desc = new Label
        {
            Text = "Xem, tìm kiếm và khóa/mở khóa tài khoản trong hệ thống.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(20, 52),
        };

        _searchBox.FillColor = UITheme.White;
        _searchBox.BorderColor = UITheme.Neutral300;
        _searchBox.Radius = 8;
        _searchBox.Size = new Size(250, 36);
        _txtSearch.PlaceholderText = "Tìm email hoặc tên...";
        _txtSearch.Font = UITheme.FontBody;
        _txtSearch.BorderStyle = BorderStyle.None;
        _txtSearch.BackColor = UITheme.White;
        _txtSearch.Location = new Point(12, 8);
        _txtSearch.Width = 226;
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _page = 0; _ = LoadUsersAsync(); }
        };
        _searchBox.Controls.Add(_txtSearch);

        _segStatus.Items.Clear();
        _segStatus.Items.AddRange(["Tất cả", "ACTIVE", "LOCKED", "DISABLED"]);
        _segStatus.Width = _segStatus.MeasureTotalWidth();
        _segStatus.SelectedIndexChanged += (_, _) => { _page = 0; _ = LoadUsersAsync(); };

        _btnRefresh.Text = "Làm mới";
        _btnRefresh.Style = RoundButtonStyle.Primary;
        _btnRefresh.Size = new Size(110, 36);
        _btnRefresh.Click += (_, _) => _ = LoadUsersAsync();

        var toolbar = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(20, 84),
            BackColor = UITheme.White,
        };
        _searchBox.Margin = new Padding(0, 0, 12, 0);
        _segStatus.Margin = new Padding(0, 0, 12, 0);
        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_segStatus);
        toolbar.Controls.Add(_btnRefresh);

        _headerCard.Controls.Add(title);
        _headerCard.Controls.Add(badge);
        _headerCard.Controls.Add(desc);
        _headerCard.Controls.Add(toolbar);
    }

    // ---------- Grid card (mục 11) + states (mục 13) ----------

    private void BuildGrid()
    {
        _gridCard.FillColor = UITheme.White;
        _gridCard.BorderColor = UITheme.Neutral200;
        _gridCard.Radius = 12;
        _gridCard.Dock = DockStyle.Fill;
        _gridCard.Padding = new Padding(12);

        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = UITheme.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        _grid.GridColor = UITheme.Neutral200;
        _grid.RowHeadersVisible = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Font = UITheme.FontBody;
        _grid.RowTemplate.Height = 42;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = 36;
        _grid.ColumnHeadersDefaultCellStyle.Font = UITheme.FontLabelBold;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.Ink;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FAFAFA");
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FAFAFA");
        _grid.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#F0F3FF");
        _grid.DefaultCellStyle.SelectionForeColor = UITheme.Ink;
        _grid.AutoGenerateColumns = false;
        foreach (DataGridViewColumn column in new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "TÊN HIỂN THỊ", FillWeight = 22 },
            new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "EMAIL", FillWeight = 28 },
            new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "QUYỀN", FillWeight = 14 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "TRẠNG THÁI", FillWeight = 18 },
            new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "NGÀY ĐĂNG KÝ", FillWeight = 18 },
            new DataGridViewTextBoxColumn { Name = "LastLogin", HeaderText = "ĐĂNG NHẬP CUỐI", FillWeight = 18 },
            new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "THAO TÁC", FillWeight = 14 },
        })
        {
            // Khóa sắp xếp: _rows tra theo RowIndex nên thứ tự grid không được đổi.
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            _grid.Columns.Add(column);
        }

        _grid.CellPainting += Grid_CellPainting;
        _grid.CellClick += Grid_CellClick;
        _grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex != _hoverRow)
            {
                int previous = _hoverRow;
                _hoverRow = e.RowIndex;
                if (previous >= 0 && previous < _grid.Rows.Count) _grid.InvalidateRow(previous);
                if (_hoverRow >= 0 && _hoverRow < _grid.Rows.Count) _grid.InvalidateRow(_hoverRow);
            }
        };
        _grid.CellMouseLeave += (_, _) =>
        {
            if (_hoverRow >= 0)
            {
                int previous = _hoverRow;
                _hoverRow = -1;
                if (previous < _grid.Rows.Count) _grid.InvalidateRow(previous);
            }
        };
        _grid.RowPrePaint += (_, e) =>
        {
            if (e.RowIndex == _hoverRow && (e.State & DataGridViewElementStates.Selected) == 0)
            {
                using var brush = new SolidBrush(ColorTranslator.FromHtml("#F0F3FF"));
                e.Graphics.FillRectangle(brush, e.RowBounds);
                e.PaintCells(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Background);
                e.Handled = true;
            }
        };

        _statePanel.Dock = DockStyle.Fill;
        _statePanel.BackColor = UITheme.White;
        _statePanel.Visible = false;
        _stateTitle.Font = UITheme.FontH3;
        _stateTitle.ForeColor = UITheme.Ink;
        _stateTitle.AutoSize = true;
        _stateSub.Font = UITheme.FontBody;
        _stateSub.ForeColor = UITheme.Neutral600;
        _stateSub.AutoSize = true;
        _stateButton.Style = RoundButtonStyle.Secondary;
        _stateButton.Size = new Size(130, 36);
        _stateButton.Click += (_, _) =>
        {
            if (_stateAction == "retry") _ = LoadUsersAsync();
            else if (_stateAction == "clear")
            {
                _txtSearch.Text = string.Empty;
                _segStatus.SelectedIndex = 0;
                _page = 0;
                _ = LoadUsersAsync();
            }
        };
        _statePanel.Controls.Add(_stateTitle);
        _statePanel.Controls.Add(_stateSub);
        _statePanel.Controls.Add(_stateButton);
        _statePanel.Resize += (_, _) => ArrangeState();

        _gridCard.Controls.Add(_grid);
        _gridCard.Controls.Add(_statePanel);
    }

    private void ArrangeState()
    {
        int centerX = _statePanel.Width / 2;
        _stateTitle.Location = new Point(centerX - _stateTitle.Width / 2, _statePanel.Height / 2 - 52);
        _stateSub.Location = new Point(centerX - _stateSub.Width / 2, _statePanel.Height / 2 - 20);
        _stateButton.Location = new Point(centerX - _stateButton.Width / 2, _statePanel.Height / 2 + 12);
    }

    private void ShowState(string mode, string title, string sub, string? buttonText, string action)
    {
        _stateAction = action;
        _stateTitle.Text = title;
        _stateSub.Text = sub;
        _stateButton.Visible = buttonText is not null;
        if (buttonText is not null) _stateButton.Text = buttonText;
        _statePanel.Visible = mode != "data";
        _grid.Visible = mode == "data" || mode == "loading";
        if (_statePanel.Visible) ArrangeState();
    }

    // ---------- Pager (mục 22) ----------

    private void BuildPager()
    {
        _pager.Height = 48;
        _pager.Dock = DockStyle.Bottom;
        _pager.Margin = new Padding(0, 12, 0, 0);
        _pager.BackColor = UITheme.Neutral50;

        _summaryLabel.Font = UITheme.FontBody;
        _summaryLabel.ForeColor = UITheme.Neutral600;
        _summaryLabel.AutoSize = true;
        _summaryLabel.Dock = DockStyle.Left;
        _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;

        var pagerButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Right,
        };
        _btnPrev.Text = "Trước";
        _btnPrev.Style = RoundButtonStyle.Secondary;
        _btnPrev.Size = new Size(90, 32);
        _btnPrev.Margin = new Padding(0, 6, 8, 0);
        _btnPrev.Click += (_, _) => { if (_page > 0) { _page--; _ = LoadUsersAsync(); } };
        _pageLabel.Font = UITheme.FontBodyBold;
        _pageLabel.ForeColor = UITheme.Ink;
        _pageLabel.AutoSize = true;
        _pageLabel.Margin = new Padding(0, 12, 8, 0);
        _btnNext.Text = "Sau";
        _btnNext.Style = RoundButtonStyle.Secondary;
        _btnNext.Size = new Size(90, 32);
        _btnNext.Margin = new Padding(0, 6, 0, 0);
        _btnNext.Click += (_, _) => { _page++; _ = LoadUsersAsync(); };
        pagerButtons.Controls.Add(_btnPrev);
        pagerButtons.Controls.Add(_pageLabel);
        pagerButtons.Controls.Add(_btnNext);

        _pager.Controls.Add(_summaryLabel);
        _pager.Controls.Add(pagerButtons);
    }

    private void RenderPager()
    {
        int totalPages = Math.Max(1, (_totalCount + PageSize - 1) / PageSize);
        int from = _totalCount == 0 ? 0 : _page * PageSize + 1;
        int to = Math.Min(_totalCount, (_page + 1) * PageSize);
        _summaryLabel.Text = $"Đang hiện {from}–{to} trong tổng {_totalCount} tài khoản";
        _pageLabel.Text = $"Trang {_page + 1}/{totalPages}";
        _btnPrev.Enabled = _page > 0;
        _btnNext.Enabled = (_page + 1) * PageSize < _totalCount;
    }

    // ---------- Dữ liệu ----------

    /// <summary>Tải trang danh sách hiện tại từ API, trả về true khi thành công.</summary>
    public async Task<bool> LoadUsersAsync()
    {
        if (_busy) return false;
        _busy = true;
        SetLoading(true);
        ShowState("loading", "Đang tải dữ liệu…", "Vui lòng đợi trong giây lát.", null, string.Empty);
        try
        {
            AdminUsersResult result = await _auth.GetAdminUsersAsync(
                string.IsNullOrWhiteSpace(_txtSearch.Text) ? null : _txtSearch.Text.Trim(),
                SelectedStatus(), PageSize, _page * PageSize, _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return false;
            if (!result.Succeeded || result.Response is null)
            {
                ShowState("error", "Không tải được dữ liệu", result.ErrorMessage ?? "Kiểm tra kết nối và thử lại.", "Thử lại", "retry");
                return false;
            }

            _rows = [.. result.Response.Items];
            _totalCount = result.Response.TotalCount;
            if (_rows.Count == 0)
            {
                ShowState("empty", "Chưa có tài khoản nào", "Thử đổi từ khóa tìm kiếm hoặc bộ lọc trạng thái.", "Xóa bộ lọc", "clear");
                RenderPager();
                return true;
            }

            RenderRows();
            RenderPager();
            ShowState("data", string.Empty, string.Empty, null, string.Empty);
            return true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return false; }
        finally { if (!IsDisposed) { _busy = false; SetLoading(false); } }
    }

    private string? SelectedStatus() =>
        _segStatus.SelectedIndex <= 0 ? null : _segStatus.SelectedValue;

    private void SetLoading(bool loading)
    {
        _txtSearch.Enabled = !loading;
        _segStatus.Enabled = !loading;
        _btnRefresh.Enabled = !loading;
        _btnPrev.Enabled = !loading && _page > 0;
        _btnNext.Enabled = !loading && (_page + 1) * PageSize < _totalCount;
        UseWaitCursor = loading;
    }

    private void RenderRows()
    {
        _grid.Rows.Clear();
        foreach (AdminUserSummaryResponse row in _rows)
        {
            _grid.Rows.Add(row.UserId, row.DisplayName, row.Email, string.Empty, string.Empty,
                row.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm"),
                row.LastLoginAt is null ? "—" : row.LastLoginAt.Value.LocalDateTime.ToString("dd/MM/yyyy HH:mm"),
                string.Empty);
        }
    }

    private string ActionText(AdminUserSummaryResponse row)
    {
        // Tài khoản của chính mình và DISABLED không thao tác ở màn hình này (xem AC2/AC3).
        if (row.UserId == _auth.Session.CurrentUser?.UserId || row.AccountStatus == "DISABLED") return "—";
        return row.AccountStatus == "LOCKED" ? "Mở khóa" : "Khóa";
    }

    private bool IsActionable(AdminUserSummaryResponse row) => ActionText(row) != "—";

    // ---------- Vẽ pill (mục 9) ----------

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || e.Graphics is null) return;
        string? name = _grid.Columns[e.ColumnIndex].Name;
        if (name != "Status" && name != "Role" && name != "Action") return;
        AdminUserSummaryResponse row = _rows[e.RowIndex];

        if (name == "Action" && !IsActionable(row))
        {
            e.PaintBackground(e.ClipBounds, true);
            TextRenderer.DrawText(e.Graphics, "—", UITheme.FontBody, e.CellBounds, UITheme.Neutral400,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            e.Handled = true;
            return;
        }

        (string text, Color bg, Color fg, bool ghost) = name switch
        {
            "Status" => row.AccountStatus switch
            {
                "ACTIVE" => ("Đang hoạt động", ColorTranslator.FromHtml("#D1FAE5"), ColorTranslator.FromHtml("#065F46"), false),
                "LOCKED" => ("Bị khóa", UITheme.DangerBg, UITheme.PriorityHighText, false),
                _ => ("Vô hiệu hóa", UITheme.Neutral200, UITheme.Neutral600, false),
            },
            "Role" => row.IsSystemAdmin
                ? ("Admin", UITheme.Black, UITheme.White, false)
                : ("User", UITheme.Neutral100, UITheme.Neutral600, false),
            _ => (ActionText(row), UITheme.White, UITheme.Neutral800, true),
        };
        e.PaintBackground(e.ClipBounds, true);
        var rect = new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y + 9, e.CellBounds.Width - 20, e.CellBounds.Height - 18);
        UITheme.DrawBadge(e.Graphics, text, bg, fg, rect, UITheme.FontMicro);
        if (ghost)
        {
            using var pen = new Pen(UITheme.Neutral200, 1f);
            using var path = UITheme.CreateRoundedRectanglePath(rect, rect.Height / 2);
            e.Graphics.DrawPath(pen, path);
        }

        e.Handled = true;
    }

    private async void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || _busy) return;
        if (_grid.Columns[e.ColumnIndex].Name != "Action") return;
        AdminUserSummaryResponse row = _rows[e.RowIndex];
        if (row.UserId == _auth.Session.CurrentUser?.UserId)
        {
            ShowState("error", "Không thể tự khóa", "Tài khoản của chính mình không thao tác được ở màn hình này.", "Thử lại", "retry");
            return;
        }

        if (!IsActionable(row))
        {
            ShowState("error", "Không thao tác được", "Tài khoản vô hiệu hóa (DISABLED) không thao tác được ở màn hình này.", "Thử lại", "retry");
            return;
        }

        bool locking = row.AccountStatus != "LOCKED";
        string action = locking ? "khóa" : "mở khóa";
        string prompt = row.IsSystemAdmin
            ? $"Đây là tài khoản quản trị viên ({row.Email}). Hành động sẽ được ghi audit.\nBạn chắc chắn muốn {action} tài khoản này?"
            : $"Bạn chắc chắn muốn {action} tài khoản {row.Email}?";
        if (MessageBox.Show(this, prompt, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _busy = true;
        SetLoading(true);
        try
        {
            AdminStatusResult result = await _auth.UpdateUserStatusAsync(
                row.UserId, locking ? "LOCKED" : "ACTIVE", _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return;
            if (!result.Succeeded)
            {
                ShowState("error", "Không thực hiện được", result.ErrorMessage ?? "Kiểm tra kết nối và thử lại.", "Thử lại", "retry");
                return;
            }

            // Chỉ báo thành công khi tải lại cũng thành công, tránh grid cũ + tin vui giả.
            if (await LoadUsersAsync())
            {
                _toast?.ShowToast(this, result.Response!.Message);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { if (!IsDisposed) { _busy = false; SetLoading(false); } }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Nhãn pill nhỏ dùng cho badge trong header.</summary>
    private sealed class BadgeLabel : Control
    {
        private readonly Color _bg;
        private readonly Color _fg;

        public BadgeLabel(string text, Color bg, Color fg)
        {
            Text = text;
            _bg = bg;
            _fg = fg;
            Font = UITheme.FontMicro;
            BackColor = UITheme.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(TextRenderer.MeasureText(text, Font).Width + 20, 20);
        }

        protected override void OnPaint(PaintEventArgs e) =>
            UITheme.DrawBadge(e.Graphics, Text, _bg, _fg, new Rectangle(0, 0, Width - 1, Height - 1));
    }
}
