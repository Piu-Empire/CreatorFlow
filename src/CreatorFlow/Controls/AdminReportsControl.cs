using CreatorFlow.Contracts.Admin;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Màn hình admin xử lý báo cáo vi phạm theo style guide.</summary>
public class AdminReportsControl : UserControl
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
    private readonly Panel _stateBox = new();
    private readonly Label _stateTitle = new();
    private readonly Label _stateSub = new();
    private readonly Panel _stateBtnWrap = new();
    private readonly RoundedButton _stateButton = new();
    private readonly Panel _pager = new();
    private readonly Label _summaryLabel = new();
    private readonly RoundedButton _btnPrev = new();
    private readonly RoundedButton _btnNext = new();
    private readonly Label _pageLabel = new();
    private List<AdminReportSummaryResponse> _rows = new();
    private int _totalCount;
    private int _page;
    private int _hoverRow = -1;
    private bool _busy;
    private string _stateAction = string.Empty;

    public AdminReportsControl(AuthApiFacade auth, ToastNotification? toast = null)
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
            Text = "Báo cáo vi phạm",
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
            Text = "Xem xét tố cáo từ người dùng và ghi nhận kết quả xử lý.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(20, 52),
        };

        _searchBox.FillColor = UITheme.White;
        _searchBox.BorderColor = UITheme.Neutral300;
        _searchBox.Radius = 8;
        _searchBox.Size = new Size(250, 36);
        _txtSearch.PlaceholderText = "Tìm email người tố cáo/bị tố cáo...";
        _txtSearch.Font = UITheme.FontBody;
        _txtSearch.BorderStyle = BorderStyle.None;
        _txtSearch.BackColor = UITheme.White;
        _txtSearch.Location = new Point(12, 8);
        _txtSearch.Width = 226;
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _page = 0; _ = LoadReportsAsync(); }
        };
        _searchBox.Controls.Add(_txtSearch);

        _segStatus.Items.Clear();
        _segStatus.Items.AddRange(["Tất cả", "OPEN", "IN_REVIEW", "RESOLVED", "REJECTED"]);
        _segStatus.Width = _segStatus.MeasureTotalWidth();
        _segStatus.SelectedIndexChanged += (_, _) => { _page = 0; _ = LoadReportsAsync(); };

        _btnRefresh.Text = "Làm mới";
        _btnRefresh.Style = RoundButtonStyle.Primary;
        _btnRefresh.Size = new Size(110, 36);
        _btnRefresh.Click += (_, _) => _ = LoadReportsAsync();

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
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Reporter", HeaderText = "NGƯỜI TỐ CÁO", FillWeight = 22 },
            new DataGridViewTextBoxColumn { Name = "Reported", HeaderText = "BỊ TỐ CÁO", FillWeight = 22 },
            new DataGridViewTextBoxColumn { Name = "Reason", HeaderText = "LÝ DO", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "TRẠNG THÁI", FillWeight = 16 },
            new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "NGÀY GỬI", FillWeight = 16 },
            new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "THAO TÁC", FillWeight = 12 },
        })
        {
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
        _stateBox.Size = new Size(460, 180);
        _stateBox.BackColor = UITheme.White;
        _stateTitle.Font = UITheme.FontH3;
        _stateTitle.ForeColor = UITheme.Ink;
        _stateTitle.Dock = DockStyle.Top;
        _stateTitle.Height = 40;
        _stateTitle.TextAlign = ContentAlignment.MiddleCenter;
        _stateSub.Font = UITheme.FontBody;
        _stateSub.ForeColor = UITheme.Neutral600;
        _stateSub.Dock = DockStyle.Top;
        _stateSub.Height = 48;
        _stateSub.TextAlign = ContentAlignment.TopCenter;
        _stateBtnWrap.Dock = DockStyle.Top;
        _stateBtnWrap.Height = 52;
        _stateBtnWrap.BackColor = UITheme.White;
        _stateButton.Style = RoundButtonStyle.Secondary;
        _stateButton.Size = new Size(130, 36);
        _stateButton.Anchor = AnchorStyles.None;
        _stateButton.Click += (_, _) =>
        {
            if (_stateAction == "retry") _ = LoadReportsAsync();
            else if (_stateAction == "clear")
            {
                _txtSearch.Text = string.Empty;
                _segStatus.SelectedIndex = 0;
                _page = 0;
                _ = LoadReportsAsync();
            }
        };
        _stateBtnWrap.Controls.Add(_stateButton);
        _stateBox.Controls.Add(_stateBtnWrap);
        _stateBox.Controls.Add(_stateSub);
        _stateBox.Controls.Add(_stateTitle);
        _statePanel.Controls.Add(_stateBox);
        _statePanel.Resize += (_, _) => ArrangeState();

        _gridCard.Controls.Add(_grid);
        _gridCard.Controls.Add(_statePanel);
    }

    private void ArrangeState()
    {
        _stateBox.Location = new Point(
            Math.Max(0, (_statePanel.Width - _stateBox.Width) / 2),
            Math.Max(0, (_statePanel.Height - _stateBox.Height) / 2));
        _stateButton.Location = new Point(
            Math.Max(0, (_stateBtnWrap.Width - _stateButton.Width) / 2), 6);
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
        _btnPrev.Click += (_, _) => { if (_page > 0) { _page--; _ = LoadReportsAsync(); } };
        _pageLabel.Font = UITheme.FontBodyBold;
        _pageLabel.ForeColor = UITheme.Ink;
        _pageLabel.AutoSize = true;
        _pageLabel.Margin = new Padding(0, 12, 8, 0);
        _btnNext.Text = "Sau";
        _btnNext.Style = RoundButtonStyle.Secondary;
        _btnNext.Size = new Size(90, 32);
        _btnNext.Margin = new Padding(0, 6, 0, 0);
        _btnNext.Click += (_, _) => { _page++; _ = LoadReportsAsync(); };
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
        _summaryLabel.Text = $"Đang hiện {from}–{to} trong tổng {_totalCount} báo cáo";
        _pageLabel.Text = $"Trang {_page + 1}/{totalPages}";
        _btnPrev.Enabled = _page > 0;
        _btnNext.Enabled = (_page + 1) * PageSize < _totalCount;
    }

    /// <summary>Tải trang báo cáo hiện tại từ API, trả về true khi thành công.</summary>
    public async Task<bool> LoadReportsAsync()
    {
        if (_busy) return false;
        _busy = true;
        SetLoading(true);
        ShowState("loading", "Đang tải dữ liệu…", "Vui lòng đợi trong giây lát.", null, string.Empty);
        try
        {
            AdminReportsResult result = await _auth.GetAdminReportsAsync(
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
                ShowState("empty", "Chưa có báo cáo nào", "Thử đổi từ khóa tìm kiếm hoặc bộ lọc trạng thái.", "Xóa bộ lọc", "clear");
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
        foreach (AdminReportSummaryResponse row in _rows)
        {
            int index = _grid.Rows.Add(row.ReportId, row.ReporterEmail, row.ReportedEmail,
                row.Reason, string.Empty,
                row.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm"), string.Empty);
            _grid.Rows[index].Cells["Reason"].ToolTipText =
                string.IsNullOrWhiteSpace(row.Details) ? row.Reason : $"{row.Reason}\n{row.Details}";
        }
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || e.Graphics is null) return;
        string? name = _grid.Columns[e.ColumnIndex].Name;
        if (name != "Status" && name != "Action") return;
        AdminReportSummaryResponse row = _rows[e.RowIndex];

        (string text, Color bg, Color fg, bool ghost) = name switch
        {
            "Status" => row.Status switch
            {
                "OPEN" => ("Mới", UITheme.Neutral800, UITheme.White, false),
                "IN_REVIEW" => ("Đang xem xét", UITheme.Warning, UITheme.Neutral900, false),
                "RESOLVED" => ("Đã xử lý", ColorTranslator.FromHtml("#D1FAE5"), ColorTranslator.FromHtml("#065F46"), false),
                _ => ("Đã bác bỏ", UITheme.Neutral200, UITheme.Neutral600, false),
            },
            _ => ("Xử lý", UITheme.White, UITheme.Neutral800, true),
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
        AdminReportSummaryResponse row = _rows[e.RowIndex];

        using var dialog = new HandleReportDialog(row.ReportedEmail);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _busy = true;
        SetLoading(true);
        try
        {
            AdminReportHandleResult result = await _auth.HandleReportAsync(
                row.ReportId, dialog.SelectedStatus, dialog.Note, _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return;
            if (!result.Succeeded)
            {
                ShowState("error", "Không thực hiện được", result.ErrorMessage ?? "Kiểm tra kết nối và thử lại.", "Thử lại", "retry");
                return;
            }

            if (await LoadReportsAsync())
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
}
