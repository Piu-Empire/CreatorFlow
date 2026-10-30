using CreatorFlow.Contracts.Admin;
using CreatorFlow.Services;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// SCRUM-23: thống kê cơ bản toàn hệ thống cho System Admin.
/// Toàn bộ con số do API tổng hợp bằng SQL; màn hình chỉ hiển thị.
/// </summary>
public sealed class AdminStatisticsControl : UserControl
{
    private readonly AuthApiFacade _auth;
    private readonly CancellationTokenSource _lifetime = new();

    private readonly RoundedPanel _headerCard = new();
    private readonly RoundedButton _btnRefresh = new();
    private readonly Label _statusLabel = new();
    private readonly Label _usersValue = ValueLabel();
    private readonly Label _usersSub = SubLabel();
    private readonly Label _projectsValue = ValueLabel();
    private readonly Label _projectsSub = SubLabel();
    private readonly Label _contentsValue = ValueLabel();
    private readonly Label _contentsSub = SubLabel();
    private readonly Label _subscriptionsValue = ValueLabel();
    private readonly Label _subscriptionsSub = SubLabel();
    private readonly Label _reportsValue = ValueLabel();
    private readonly Label _reportsSub = SubLabel();
    private readonly DataGridView _contentsGrid = NewGrid();
    private readonly DataGridView _plansGrid = NewGrid();
    private readonly DataGridView _reportsGrid = NewGrid();
    private bool _busy;

    public AdminStatisticsControl(AuthApiFacade auth)
    {
        _auth = auth;
        BackColor = UITheme.Neutral50;
        Dock = DockStyle.Fill;
        Padding = new Padding(24, 20, 24, 20);

        BuildHeader();
        BuildTables();

        Controls.Add(BuildTablesHost());
        Controls.Add(BuildCardsHost());
        Controls.Add(_headerCard);
    }

    private void BuildHeader()
    {
        _headerCard.FillColor = UITheme.White;
        _headerCard.BorderColor = UITheme.Neutral200;
        _headerCard.Radius = 12;
        _headerCard.Dock = DockStyle.Top;
        _headerCard.Height = 116;
        _headerCard.Margin = new Padding(0, 0, 0, 12);

        var title = new Label
        {
            Text = "Thống kê hệ thống",
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
            Text = "Số liệu tổng hợp trực tiếp từ cơ sở dữ liệu của hệ thống.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(20, 52),
        };

        _btnRefresh.Text = "Tải lại";
        _btnRefresh.Style = RoundButtonStyle.Primary;
        _btnRefresh.Size = new Size(110, 36);
        _btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnRefresh.Click += (_, _) => _ = LoadAsync();

        _statusLabel.Font = UITheme.FontLabel;
        _statusLabel.ForeColor = UITheme.Neutral600;
        _statusLabel.AutoSize = true;
        _statusLabel.Location = new Point(20, 84);

        _headerCard.Controls.Add(title);
        _headerCard.Controls.Add(badge);
        _headerCard.Controls.Add(desc);
        _headerCard.Controls.Add(_statusLabel);
        _headerCard.Controls.Add(_btnRefresh);
        _headerCard.Resize += (_, _) =>
            _btnRefresh.Location = new Point(_headerCard.Width - _btnRefresh.Width - 20, 24);
    }

    private void BuildCards()
    {
        // Không cần dựng ở đây; host dựng trong BuildCardsHost để giữ thứ tự Dock.
    }

    private Control BuildCardsHost()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = UITheme.Neutral50,
            Margin = new Padding(0, 0, 0, 12),
            Padding = Padding.Empty,
        };
        for (int i = 0; i < 5; i++)
        {
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        }

        host.Controls.Add(Card("Người dùng", _usersValue, _usersSub), 0, 0);
        host.Controls.Add(Card("Project", _projectsValue, _projectsSub), 1, 0);
        host.Controls.Add(Card("Content", _contentsValue, _contentsSub), 2, 0);
        host.Controls.Add(Card("Đăng ký gói", _subscriptionsValue, _subscriptionsSub), 3, 0);
        host.Controls.Add(Card("Báo cáo vi phạm", _reportsValue, _reportsSub), 4, 0);
        return host;
    }

    private static Control Card(string title, Label value, Label sub)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            FillColor = UITheme.White,
            BorderColor = UITheme.Neutral200,
            Radius = 12,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(14, 10, 14, 10),
        };
        card.Controls.Add(sub);
        card.Controls.Add(value);
        card.Controls.Add(new Label
        {
            Text = title,
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Dock = DockStyle.Top,
            Height = 22,
        });
        value.Dock = DockStyle.Top;
        value.Height = 38;
        sub.Dock = DockStyle.Fill;
        return card;
    }

    private void BuildTables()
    {
        AddColumns(_contentsGrid, ("Key", "TRẠNG THÁI", 60), ("Count", "SỐ CONTENT", 40));
        AddColumns(_plansGrid, ("Plan", "GÓI", 55), ("Total", "TỔNG", 22), ("Active", "ACTIVE", 23));
        AddColumns(_reportsGrid, ("Key", "TRẠNG THÁI", 60), ("Count", "SỐ BÁO CÁO", 40));
    }

    private Control BuildTablesHost()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = UITheme.Neutral50,
            Padding = Padding.Empty,
        };
        for (int i = 0; i < 3; i++)
        {
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        }

        host.Controls.Add(TableCard("Content theo trạng thái", _contentsGrid), 0, 0);
        host.Controls.Add(TableCard("Gói dịch vụ đang dùng", _plansGrid), 1, 0);
        host.Controls.Add(TableCard("Báo cáo theo trạng thái", _reportsGrid), 2, 0);
        return host;
    }

    private static Control TableCard(string title, DataGridView grid)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            FillColor = UITheme.White,
            BorderColor = UITheme.Neutral200,
            Radius = 12,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 10, 0),
        };
        card.Controls.Add(grid);
        card.Controls.Add(new Label
        {
            Text = title,
            Font = UITheme.FontH3,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Dock = DockStyle.Top,
            Height = 34,
        });
        return card;
    }

    private static void AddColumns(DataGridView grid, params (string Name, string Header, int Weight)[] columns)
    {
        foreach ((string name, string header, int weight) in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
        }
    }

    private static Label ValueLabel() => new()
    {
        Font = UITheme.FontPageTitle,
        ForeColor = UITheme.Ink,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static Label SubLabel() => new()
    {
        Font = UITheme.FontLabel,
        ForeColor = UITheme.Neutral600,
        AutoSize = false,
        TextAlign = ContentAlignment.TopLeft,
    };

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
        RowTemplate = { Height = 34 },
        EnableHeadersVisualStyles = false,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 32,
        AutoGenerateColumns = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Ink,
            BackColor = UITheme.Neutral50,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = UITheme.Neutral100,
            SelectionForeColor = UITheme.Ink,
        },
    };

    /// <summary>Tải lại toàn bộ số liệu; gọi khi mở màn hoặc bấm "Tải lại".</summary>
    public async Task<bool> LoadAsync()
    {
        if (_busy) return false;
        _busy = true;
        _btnRefresh.Enabled = false;
        _statusLabel.ForeColor = UITheme.Neutral600;
        _statusLabel.Text = "Đang tải số liệu...";
        UseWaitCursor = true;
        try
        {
            AdminStatisticsResult result = await _auth.GetAdminStatisticsAsync(_lifetime.Token);
            if (IsDisposed || _lifetime.IsCancellationRequested) return false;
            if (!result.Succeeded || result.Response is null)
            {
                _statusLabel.ForeColor = UITheme.Danger;
                _statusLabel.Text = result.ErrorMessage ?? "Không tải được thống kê hệ thống.";
                return false;
            }

            Render(result.Response);
            _statusLabel.ForeColor = UITheme.Neutral600;
            _statusLabel.Text = $"Cập nhật lúc {result.Response.GeneratedAt.LocalDateTime:HH:mm:ss dd/MM/yyyy}.";
            return true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return false; }
        finally
        {
            if (!IsDisposed)
            {
                _busy = false;
                _btnRefresh.Enabled = true;
                UseWaitCursor = false;
            }
        }
    }

    private void Render(AdminStatisticsResponse data)
    {
        _usersValue.Text = data.Users.Total.ToString("N0");
        _usersSub.Text = $"Active {data.Users.Active} · Locked {data.Users.Locked} · "
                       + $"Disabled {data.Users.Disabled} · Admin {data.Users.SystemAdmins}";

        _projectsValue.Text = data.Projects.Total.ToString("N0");
        _projectsSub.Text = $"Có content Published 30 ngày: {data.Projects.ActiveLast30Days}";

        int contents = data.ContentsByStatus.Sum(row => row.Count);
        _contentsValue.Text = contents.ToString("N0");
        _contentsSub.Text = data.ContentsByStatus.Count == 0
            ? "Chưa có content nào"
            : string.Join(" · ", data.ContentsByStatus.Select(row => $"{row.Key} {row.Count}"));

        _subscriptionsValue.Text = data.SubscriptionsByPlan.Sum(row => row.Active).ToString("N0");
        _subscriptionsSub.Text = data.SubscriptionsByPlan.Count == 0
            ? "Chưa có gói nào"
            : string.Join(" · ", data.SubscriptionsByPlan.Where(row => row.Total > 0)
                .Select(row => $"{row.Code} {row.Active}/{row.Total}"));

        int reports = data.ReportsByStatus.Sum(row => row.Count);
        _reportsValue.Text = reports.ToString("N0");
        _reportsSub.Text = data.ReportsByStatus.Count == 0
            ? "Chưa có báo cáo nào"
            : string.Join(" · ", data.ReportsByStatus.Select(row => $"{row.Key} {row.Count}"));

        RenderRows(_contentsGrid, data.ContentsByStatus.Select(row => new[] { row.Key, row.Count.ToString("N0") }));
        RenderRows(_plansGrid, data.SubscriptionsByPlan.Select(row =>
            new[] { $"{row.Code} — {row.Name}", row.Total.ToString("N0"), row.Active.ToString("N0") }));
        RenderRows(_reportsGrid, data.ReportsByStatus.Select(row => new[] { row.Key, row.Count.ToString("N0") }));
    }

    private static void RenderRows(DataGridView grid, IEnumerable<string[]> rows)
    {
        grid.Rows.Clear();
        foreach (string[] values in rows)
        {
            grid.Rows.Add(values);
        }
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
