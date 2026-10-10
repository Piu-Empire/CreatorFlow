using CreatorFlow.Contracts.Admin;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>Màn hình quản trị người dùng: tìm kiếm, lọc trạng thái, khóa/mở khóa.</summary>
public class AdminUserManagementControl : UserControl
{
    private const int PageSize = 100;

    private readonly AuthApiFacade _auth;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TextBox _txtSearch = new();
    private readonly ComboBox _cboStatus = new();
    private readonly RoundedButton _btnRefresh = new();
    private readonly Label _messageLabel = new();
    private readonly Label _countLabel = new();
    private readonly DataGridView _grid = new();
    private List<AdminUserSummaryResponse> _rows = new();
    private bool _busy;

    public AdminUserManagementControl(AuthApiFacade auth)
    {
        _auth = auth;
        BackColor = UITheme.Neutral50;
        Dock = DockStyle.Fill;

        var title = new Label
        {
            Text = "Quản trị người dùng",
            Font = UITheme.FontH2,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(24, 16),
        };

        _txtSearch.PlaceholderText = "Tìm kiếm theo email hoặc tên hiển thị...";
        _txtSearch.Font = UITheme.FontBody;
        _txtSearch.Location = new Point(24, 52);
        _txtSearch.Width = 320;
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = LoadUsersAsync(); }
        };

        _cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboStatus.Font = UITheme.FontBody;
        _cboStatus.Items.AddRange(["Tất cả", "ACTIVE", "LOCKED", "DISABLED"]);
        _cboStatus.SelectedIndex = 0;
        _cboStatus.Location = new Point(356, 52);
        _cboStatus.Width = 150;
        _cboStatus.SelectedIndexChanged += (_, _) => _ = LoadUsersAsync();

        _btnRefresh.Text = "Làm mới";
        _btnRefresh.Style = RoundButtonStyle.Primary;
        _btnRefresh.Width = 110;
        _btnRefresh.Height = 32;
        _btnRefresh.Location = new Point(518, 48);
        _btnRefresh.Click += (_, _) => _ = LoadUsersAsync();

        _countLabel.Font = UITheme.FontLabel;
        _countLabel.ForeColor = UITheme.Neutral600;
        _countLabel.AutoSize = true;
        _countLabel.Location = new Point(24, 88);

        _messageLabel.Font = UITheme.FontBody;
        _messageLabel.ForeColor = UITheme.Danger;
        _messageLabel.AutoSize = true;
        _messageLabel.Location = new Point(24, 110);

        _grid.Location = new Point(24, 136);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _grid.Width = Math.Max(400, Width - 48);
        _grid.Height = Math.Max(200, Height - 160);
        _grid.BackgroundColor = UITheme.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.RowHeadersVisible = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Font = UITheme.FontBody;
        _grid.ColumnHeadersDefaultCellStyle.Font = UITheme.FontBodyBold;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Neutral100;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = UITheme.Neutral50;
        _grid.AutoGenerateColumns = false;
        foreach (DataGridViewColumn column in new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Tên hiển thị", FillWeight = 24 },
            new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "Quyền", FillWeight = 16 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", FillWeight = 16 },
            new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "Ngày đăng ký", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "LastLogin", HeaderText = "Đăng nhập cuối", FillWeight = 20 },
            new DataGridViewButtonColumn { Name = "Action", HeaderText = "Thao tác", FillWeight = 16, UseColumnTextForButtonValue = false },
        })
        {
            // Khóa sắp xếp: _rows tra theo RowIndex nên thứ tự grid không được đổi.
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            _grid.Columns.Add(column);
        }
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.CellClick += Grid_CellClick;

        Controls.Add(title);
        Controls.Add(_txtSearch);
        Controls.Add(_cboStatus);
        Controls.Add(_btnRefresh);
        Controls.Add(_countLabel);
        Controls.Add(_messageLabel);
        Controls.Add(_grid);
        Resize += (_, _) =>
        {
            _grid.Width = Math.Max(400, Width - 48);
            _grid.Height = Math.Max(200, Height - 160);
        };
    }

    /// <summary>Tải trang danh sách người dùng đầu tiên từ API, trả về true khi thành công.</summary>
    public async Task<bool> LoadUsersAsync()
    {
        if (_busy) return false;
        _busy = true;
        SetLoading(true);
        _messageLabel.ForeColor = UITheme.Danger;
        _messageLabel.Text = "Đang tải danh sách...";
        try
        {
            AdminUsersResult result = await _auth.GetAdminUsersAsync(
                string.IsNullOrWhiteSpace(_txtSearch.Text) ? null : _txtSearch.Text.Trim(),
                SelectedStatus(), PageSize, 0, _lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return false;
            if (!result.Succeeded || result.Response is null)
            {
                _messageLabel.Text = result.ErrorMessage;
                return false;
            }

            _rows = [.. result.Response.Items];
            RenderRows();
            _countLabel.Text = result.Response.TotalCount > _rows.Count
                ? $"Tổng: {result.Response.TotalCount} tài khoản (hiển thị {_rows.Count} đầu)"
                : $"Tổng: {result.Response.TotalCount} tài khoản";
            _messageLabel.Text = string.Empty;
            return true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return false; }
        finally { if (!IsDisposed) { _busy = false; SetLoading(false); } }
    }

    private string? SelectedStatus() =>
        _cboStatus.SelectedIndex <= 0 ? null : _cboStatus.SelectedItem?.ToString();

    private void SetLoading(bool loading)
    {
        _txtSearch.Enabled = !loading;
        _cboStatus.Enabled = !loading;
        _btnRefresh.Enabled = !loading;
        UseWaitCursor = loading;
    }

    private void RenderRows()
    {
        _grid.Rows.Clear();
        foreach (AdminUserSummaryResponse row in _rows)
        {
            _grid.Rows.Add(row.UserId, row.DisplayName, row.Email,
                row.IsSystemAdmin ? "Admin" : "User", row.AccountStatus,
                row.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm"),
                row.LastLoginAt is null ? "—" : row.LastLoginAt.Value.LocalDateTime.ToString("dd/MM/yyyy HH:mm"),
                ActionText(row));
        }
    }

    private string ActionText(AdminUserSummaryResponse row)
    {
        // Tài khoản của chính mình và DISABLED không thao tác ở màn hình này (xem AC2/AC3).
        if (row.UserId == _auth.Session.CurrentUser?.UserId || row.AccountStatus == "DISABLED") return "—";
        return row.AccountStatus == "LOCKED" ? "Mở khóa" : "Khóa";
    }

    private bool IsActionable(AdminUserSummaryResponse row) => ActionText(row) != "—";

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;
        AdminUserSummaryResponse row = _rows[e.RowIndex];
        string? name = _grid.Columns[e.ColumnIndex].Name;
        if (name == "Status")
        {
            e.CellStyle.ForeColor = row.AccountStatus switch
            {
                "ACTIVE" => UITheme.Success,
                "LOCKED" => UITheme.Danger,
                _ => UITheme.Neutral600,
            };
            e.CellStyle.Font = UITheme.FontBodyBold;
        }
        else if (name == "Role" && row.IsSystemAdmin)
        {
            e.CellStyle.ForeColor = UITheme.Info;
            e.CellStyle.Font = UITheme.FontBodyBold;
        }
        else if (name == "Action" && !IsActionable(row))
        {
            e.CellStyle.ForeColor = UITheme.Neutral400;
        }
    }

    private async void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || _busy) return;
        if (_grid.Columns[e.ColumnIndex].Name != "Action") return;
        AdminUserSummaryResponse row = _rows[e.RowIndex];
        if (row.UserId == _auth.Session.CurrentUser?.UserId)
        {
            _messageLabel.Text = "Không thể tự khóa tài khoản của chính mình.";
            return;
        }

        if (!IsActionable(row))
        {
            _messageLabel.Text = "Tài khoản vô hiệu hóa (DISABLED) không thao tác được ở màn hình này.";
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
                _messageLabel.Text = result.ErrorMessage;
                return;
            }

            // Chỉ báo thành công khi tải lại cũng thành công, tránh grid cũ + tin vui giả.
            if (await LoadUsersAsync())
            {
                _messageLabel.ForeColor = UITheme.Success;
                _messageLabel.Text = result.Response!.Message;
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
