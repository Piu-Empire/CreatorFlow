using CreatorFlow.Contracts.Admin;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// SCRUM-23: màn System Admin quản lý gói dịch vụ và đăng ký gói.
/// Chỉ hiển thị dữ liệu API trả về; mọi validation/permission nằm ở CreatorFlow.Api.
/// </summary>
public sealed class AdminPlansControl : UserControl
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
    private readonly RoundedButton _btnCreatePlan = new();
    private readonly RoundedButton _btnEditPlan = new();
    private readonly RoundedButton _btnAssignPlan = new();
    private readonly RoundedButton _btnChangeStatus = new();

    private readonly RoundedPanel _plansCard = new();
    private readonly RoundedPanel _subsCard = new();
    private readonly DataGridView _plansGrid = NewGrid();
    private readonly DataGridView _subsGrid = NewGrid();
    private readonly Label _plansEmpty = new();
    private readonly Label _subsEmpty = new();
    private readonly Label _statusLabel = new();
    private readonly Label _pageLabel = new();
    private readonly RoundedButton _btnPrev = new();
    private readonly RoundedButton _btnNext = new();

    private List<AdminPlanResponse> _plans = [];
    private List<AdminSubscriptionResponse> _subs = [];
    private int _totalCount;
    private int _page;
    private bool _busy;

    public AdminPlansControl(AuthApiFacade auth, ToastNotification? toast = null)
    {
        _auth = auth;
        _toast = toast;
        BackColor = UITheme.Neutral50;
        Dock = DockStyle.Fill;
        Padding = new Padding(24, 20, 24, 20);

        BuildHeader();
        BuildPlansCard();
        BuildSubsCard();

        // Dock đáy trước để các card phía trên xếp đúng thứ tự.
        Controls.Add(_subsCard);
        Controls.Add(_plansCard);
        Controls.Add(_headerCard);
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
            Text = "Gói dịch vụ",
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
            Text = "Quản lý gói dịch vụ của hệ thống và gán gói cho từng người dùng.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(20, 52),
        };

        _searchBox.FillColor = UITheme.White;
        _searchBox.BorderColor = UITheme.Neutral300;
        _searchBox.Radius = 8;
        _searchBox.Size = new Size(230, 36);
        _txtSearch.PlaceholderText = "Tìm email người dùng...";
        _txtSearch.Font = UITheme.FontBody;
        _txtSearch.BorderStyle = BorderStyle.None;
        _txtSearch.BackColor = UITheme.White;
        _txtSearch.Location = new Point(12, 8);
        _txtSearch.Width = 206;
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _page = 0;
                _ = LoadAsync();
            }
        };
        _searchBox.Controls.Add(_txtSearch);

        _segStatus.Items.Clear();
        _segStatus.Items.AddRange(["Tất cả", "ACTIVE", "EXPIRED", "CANCELLED"]);
        _segStatus.Width = _segStatus.MeasureTotalWidth();
        _segStatus.SelectedIndexChanged += (_, _) =>
        {
            _page = 0;
            _ = LoadSubscriptionsAsync();
        };

        _btnRefresh.Text = "Làm mới";
        _btnRefresh.Style = RoundButtonStyle.Secondary;
        _btnRefresh.Size = new Size(100, 36);
        _btnRefresh.Click += (_, _) => _ = LoadAsync();

        _btnCreatePlan.Text = "Tạo gói";
        _btnCreatePlan.Style = RoundButtonStyle.Primary;
        _btnCreatePlan.Size = new Size(110, 36);
        _btnCreatePlan.Click += async (_, _) => await CreatePlanAsync();

        _btnEditPlan.Text = "Sửa gói";
        _btnEditPlan.Style = RoundButtonStyle.Secondary;
        _btnEditPlan.Size = new Size(100, 36);
        _btnEditPlan.Enabled = false;
        _btnEditPlan.Click += async (_, _) => await EditPlanAsync();

        _btnAssignPlan.Text = "Gán gói";
        _btnAssignPlan.Style = RoundButtonStyle.Primary;
        _btnAssignPlan.Size = new Size(100, 36);
        _btnAssignPlan.Click += async (_, _) => await AssignPlanAsync();

        _btnChangeStatus.Text = "Đổi trạng thái";
        _btnChangeStatus.Style = RoundButtonStyle.Secondary;
        _btnChangeStatus.Size = new Size(140, 36);
        _btnChangeStatus.Enabled = false;
        _btnChangeStatus.Click += async (_, _) => await ChangeStatusAsync();

        var toolbar = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(20, 84),
            BackColor = UITheme.White,
        };
        _searchBox.Margin = new Padding(0, 0, 10, 0);
        _segStatus.Margin = new Padding(0, 0, 10, 0);
        _btnRefresh.Margin = new Padding(0, 0, 10, 0);
        _btnCreatePlan.Margin = new Padding(0, 0, 10, 0);
        _btnEditPlan.Margin = new Padding(0, 0, 10, 0);
        _btnAssignPlan.Margin = new Padding(0, 0, 10, 0);
        _btnChangeStatus.Margin = Padding.Empty;
        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_segStatus);
        toolbar.Controls.Add(_btnRefresh);
        toolbar.Controls.Add(_btnCreatePlan);
        toolbar.Controls.Add(_btnEditPlan);
        toolbar.Controls.Add(_btnAssignPlan);
        toolbar.Controls.Add(_btnChangeStatus);

        _headerCard.Controls.Add(title);
        _headerCard.Controls.Add(badge);
        _headerCard.Controls.Add(desc);
        _headerCard.Controls.Add(toolbar);
    }

    private void BuildPlansCard()
    {
        _plansCard.FillColor = UITheme.White;
        _plansCard.BorderColor = UITheme.Neutral200;
        _plansCard.Radius = 12;
        _plansCard.Dock = DockStyle.Top;
        _plansCard.Height = 250;
        _plansCard.Padding = new Padding(16, 12, 16, 12);
        _plansCard.Margin = new Padding(0, 0, 0, 12);

        var title = new Label
        {
            Text = "Danh sách gói",
            Font = UITheme.FontH3,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Dock = DockStyle.Top,
            Height = 32,
        };

        foreach (var column in new[]
        {
            new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "MÃ GÓI", FillWeight = 14 },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "TÊN GÓI", FillWeight = 24 },
            new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "GIÁ/THÁNG", FillWeight = 14 },
            new DataGridViewTextBoxColumn { Name = "Projects", HeaderText = "PROJECT", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Members", HeaderText = "THÀNH VIÊN", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Ai", HeaderText = "LƯỢT AI", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "State", HeaderText = "TRẠNG THÁI", FillWeight = 12 },
        })
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            _plansGrid.Columns.Add(column);
        }

        _plansGrid.SelectionChanged += (_, _) =>
            _btnEditPlan.Enabled = !_busy && _plansGrid.CurrentRow is not null;

        _plansEmpty.Text = "Chưa có gói dịch vụ nào. Bấm \"Tạo gói\" để thêm.";
        _plansEmpty.Font = UITheme.FontBody;
        _plansEmpty.ForeColor = UITheme.Neutral600;
        _plansEmpty.Dock = DockStyle.Fill;
        _plansEmpty.TextAlign = ContentAlignment.MiddleCenter;
        _plansEmpty.Visible = false;

        _plansCard.Controls.Add(_plansGrid);
        _plansCard.Controls.Add(_plansEmpty);
        _plansCard.Controls.Add(title);
    }

    private void BuildSubsCard()
    {
        _subsCard.FillColor = UITheme.White;
        _subsCard.BorderColor = UITheme.Neutral200;
        _subsCard.Radius = 12;
        _subsCard.Dock = DockStyle.Fill;
        _subsCard.Padding = new Padding(16, 12, 16, 12);

        var head = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UITheme.White };
        var title = new Label
        {
            Text = "Đăng ký gói",
            Font = UITheme.FontH3,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(0, 0),
        };

        _pageLabel.Font = UITheme.FontBody;
        _pageLabel.ForeColor = UITheme.Neutral600;
        _pageLabel.AutoSize = true;
        _pageLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _btnPrev.Text = "Trước";
        _btnPrev.Style = RoundButtonStyle.Secondary;
        _btnPrev.Size = new Size(80, 30);
        _btnPrev.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnPrev.Click += (_, _) =>
        {
            if (_page > 0)
            {
                _page--;
                _ = LoadSubscriptionsAsync();
            }
        };

        _btnNext.Text = "Sau";
        _btnNext.Style = RoundButtonStyle.Secondary;
        _btnNext.Size = new Size(80, 30);
        _btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnNext.Click += (_, _) =>
        {
            _page++;
            _ = LoadSubscriptionsAsync();
        };

        head.Controls.Add(title);
        head.Controls.Add(_pageLabel);
        head.Controls.Add(_btnPrev);
        head.Controls.Add(_btnNext);
        head.Resize += (_, _) =>
        {
            _btnNext.Location = new Point(head.Width - _btnNext.Width, 0);
            _btnPrev.Location = new Point(_btnNext.Left - _btnPrev.Width - 8, 0);
            _pageLabel.Location = new Point(
                Math.Max(0, _btnPrev.Left - _pageLabel.Width - 12), 6);
        };

        foreach (var column in new[]
        {
            new DataGridViewTextBoxColumn { Name = "User", HeaderText = "NGƯỜI DÙNG", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "Plan", HeaderText = "GÓI", FillWeight = 22 },
            new DataGridViewTextBoxColumn { Name = "State", HeaderText = "TRẠNG THÁI", FillWeight = 14 },
            new DataGridViewTextBoxColumn { Name = "Started", HeaderText = "BẮT ĐẦU", FillWeight = 16 },
            new DataGridViewTextBoxColumn { Name = "Expires", HeaderText = "HẾT HẠN", FillWeight = 16 },
        })
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            _subsGrid.Columns.Add(column);
        }

        _subsGrid.SelectionChanged += (_, _) =>
            _btnChangeStatus.Enabled = !_busy && _subsGrid.CurrentRow is not null;

        _subsEmpty.Text = "Chưa có đăng ký gói nào.";
        _subsEmpty.Font = UITheme.FontBody;
        _subsEmpty.ForeColor = UITheme.Neutral600;
        _subsEmpty.Dock = DockStyle.Fill;
        _subsEmpty.TextAlign = ContentAlignment.MiddleCenter;
        _subsEmpty.Visible = false;

        _statusLabel.Font = UITheme.FontLabel;
        _statusLabel.ForeColor = UITheme.Neutral600;
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 24;

        _subsCard.Controls.Add(_subsGrid);
        _subsCard.Controls.Add(_subsEmpty);
        _subsCard.Controls.Add(_statusLabel);
        _subsCard.Controls.Add(head);
    }

    private static DataGridView NewGrid() => new()
    {
        Dock = DockStyle.Fill,
        BackgroundColor = UITheme.White,
        BorderStyle = BorderStyle.None,
        CellBorderStyle = DataGridViewCellBorderStyle.Single,
        GridColor = UITheme.Neutral200,
        RowHeadersVisible = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        Font = UITheme.FontBody,
        RowTemplate = { Height = 38 },
        EnableHeadersVisualStyles = false,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 34,
        AutoGenerateColumns = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Ink,
            BackColor = UITheme.Neutral50,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = UITheme.Neutral50 },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = UITheme.Neutral100,
            SelectionForeColor = UITheme.Ink,
        },
    };

    /// <summary>Tải cả gói và đăng ký gói; gọi một lần khi mở màn.</summary>
    public async Task LoadAsync()
    {
        await LoadPlansAsync();
        await LoadSubscriptionsAsync();
    }

    private async Task LoadPlansAsync()
    {
        AdminPlansResult result = await _auth.GetAdminPlansAsync(_lifetime.Token);
        if (IsDisposed || _lifetime.IsCancellationRequested) return;
        if (!result.Succeeded || result.Response is null)
        {
            _statusLabel.ForeColor = UITheme.Danger;
            _statusLabel.Text = result.ErrorMessage ?? "Không tải được danh sách gói.";
            return;
        }

        _plans = [.. result.Response.Items];
        _plansGrid.Rows.Clear();
        foreach (AdminPlanResponse plan in _plans)
        {
            _plansGrid.Rows.Add(
                plan.Code,
                plan.Name,
                plan.PriceMonthly.ToString("N0") + " đ",
                plan.MaxProjects?.ToString() ?? "Không giới hạn",
                plan.MaxMembers?.ToString() ?? "Không giới hạn",
                plan.AiRequestLimit?.ToString() ?? "Không giới hạn",
                plan.IsActive ? "Đang bật" : "Đã tắt");
        }

        _plansGrid.Visible = _plans.Count > 0;
        _plansEmpty.Visible = _plans.Count == 0;
    }

    private async Task LoadSubscriptionsAsync()
    {
        if (_busy) return;
        _busy = true;
        SetBusy(true);
        try
        {
            AdminSubscriptionsResult result = await _auth.GetAdminSubscriptionsAsync(
                SelectedStatus(),
                null,
                string.IsNullOrWhiteSpace(_txtSearch.Text) ? null : _txtSearch.Text.Trim(),
                PageSize, _page * PageSize, _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return;
            if (!result.Succeeded || result.Response is null)
            {
                _statusLabel.ForeColor = UITheme.Danger;
                _statusLabel.Text = result.ErrorMessage ?? "Không tải được danh sách đăng ký gói.";
                return;
            }

            _subs = [.. result.Response.Items];
            _totalCount = result.Response.TotalCount;
            _subsGrid.Rows.Clear();
            foreach (AdminSubscriptionResponse row in _subs)
            {
                _subsGrid.Rows.Add(
                    row.UserEmail,
                    $"{row.PlanCode} — {row.PlanName}",
                    row.Status,
                    row.StartedAt.LocalDateTime.ToString("dd/MM/yyyy"),
                    row.ExpiresAt?.LocalDateTime.ToString("dd/MM/yyyy") ?? "Không hết hạn");
            }

            _subsGrid.Visible = _subs.Count > 0;
            _subsEmpty.Visible = _subs.Count == 0;
            RenderPager();
            _statusLabel.ForeColor = UITheme.Neutral600;
            _statusLabel.Text = "Dữ liệu đọc trực tiếp từ hệ thống.";
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally
        {
            if (!IsDisposed)
            {
                _busy = false;
                SetBusy(false);
            }
        }
    }

    private void RenderPager()
    {
        int totalPages = Math.Max(1, (_totalCount + PageSize - 1) / PageSize);
        int from = _totalCount == 0 ? 0 : _page * PageSize + 1;
        int to = Math.Min(_totalCount, (_page + 1) * PageSize);
        _pageLabel.Text = $"{from}-{to} / {_totalCount} · Trang {_page + 1}/{totalPages}";
        _btnPrev.Enabled = !_busy && _page > 0;
        _btnNext.Enabled = !_busy && (_page + 1) * PageSize < _totalCount;
    }

    private string? SelectedStatus() =>
        _segStatus.SelectedIndex <= 0 ? null : _segStatus.SelectedValue;

    private void SetBusy(bool busy)
    {
        _txtSearch.Enabled = !busy;
        _segStatus.Enabled = !busy;
        _btnRefresh.Enabled = !busy;
        _btnCreatePlan.Enabled = !busy;
        _btnAssignPlan.Enabled = !busy;
        _btnEditPlan.Enabled = !busy && _plansGrid.CurrentRow is not null;
        _btnChangeStatus.Enabled = !busy && _subsGrid.CurrentRow is not null;
        _btnPrev.Enabled = !busy && _page > 0;
        _btnNext.Enabled = !busy && (_page + 1) * PageSize < _totalCount;
        UseWaitCursor = busy;
    }

    private async Task CreatePlanAsync()
    {
        using var dialog = new PlanEditorDialog(null);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        AdminPlanSaveResult result = await _auth.CreateAdminPlanAsync(new CreatePlanRequest(
            dialog.Code, dialog.PlanName, dialog.Description, dialog.Price,
            dialog.MaxProjects, dialog.MaxMembers, dialog.AiRequestLimit), _lifetime.Token);
        if (IsDisposed || _lifetime.IsCancellationRequested) return;
        if (!result.Succeeded)
        {
            MessageBox.Show(this, result.ErrorMessage ?? "Không tạo được gói.", "Gói dịch vụ",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _toast?.ShowToast(this, $"Đã tạo gói {result.Response!.Code}.");
        await LoadPlansAsync();
    }

    private async Task EditPlanAsync()
    {
        if (_plansGrid.CurrentRow is null || _plansGrid.CurrentRow.Index >= _plans.Count) return;
        AdminPlanResponse plan = _plans[_plansGrid.CurrentRow.Index];

        using var dialog = new PlanEditorDialog(plan);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        AdminPlanSaveResult result = await _auth.UpdateAdminPlanAsync(plan.PlanId, new UpdatePlanRequest(
            dialog.Price, dialog.MaxProjects, dialog.MaxMembers, dialog.AiRequestLimit, dialog.IsActive),
            _lifetime.Token);
        if (IsDisposed || _lifetime.IsCancellationRequested) return;
        if (!result.Succeeded)
        {
            MessageBox.Show(this, result.ErrorMessage ?? "Không cập nhật được gói.", "Gói dịch vụ",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _toast?.ShowToast(this, $"Đã cập nhật gói {result.Response!.Code}.");
        await LoadPlansAsync();
    }

    private async Task AssignPlanAsync()
    {
        if (_plans.Count == 0)
        {
            MessageBox.Show(this, "Chưa có gói nào để gán.", "Gói dịch vụ",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SubscriptionDialog(_auth, _lifetime.Token, _plans);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (dialog.SelectedUserId is not { } userId || dialog.SelectedPlanId is not { } planId) return;

        AdminSubscriptionSaveResult result = await _auth.CreateAdminSubscriptionAsync(
            new CreateSubscriptionRequest(userId, planId, dialog.ExpiresAt), _lifetime.Token);
        if (IsDisposed || _lifetime.IsCancellationRequested) return;
        if (!result.Succeeded)
        {
            MessageBox.Show(this, result.ErrorMessage ?? "Không gán được gói.", "Đăng ký gói",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _toast?.ShowToast(this, $"Đã gán gói {result.Response!.PlanCode} cho {result.Response.UserEmail}.");
        await LoadSubscriptionsAsync();
    }

    private async Task ChangeStatusAsync()
    {
        if (_subsGrid.CurrentRow is null || _subsGrid.CurrentRow.Index >= _subs.Count) return;
        AdminSubscriptionResponse sub = _subs[_subsGrid.CurrentRow.Index];

        using var dialog = new SubscriptionStatusDialog(sub);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        AdminSubscriptionSaveResult result = await _auth.UpdateAdminSubscriptionAsync(
            sub.SubscriptionId, new UpdateSubscriptionRequest(dialog.Status, dialog.ExpiresAt), _lifetime.Token);
        if (IsDisposed || _lifetime.IsCancellationRequested) return;
        if (!result.Succeeded)
        {
            MessageBox.Show(this, result.ErrorMessage ?? "Không cập nhật được đăng ký gói.", "Đăng ký gói",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _toast?.ShowToast(this, "Đã cập nhật đăng ký gói.");
        await LoadSubscriptionsAsync();
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
