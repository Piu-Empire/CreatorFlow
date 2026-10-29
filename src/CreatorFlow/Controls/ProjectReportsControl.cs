using CreatorFlow.Contracts.Metrics;
using CreatorFlow.Contracts.Statistics;
using CreatorFlow.Services.Backends;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Màn Báo cáo của Project:
/// - SCRUM-52: hiệu suất Views/Likes/Comments/Shares và Engagement theo Content / Creator / Platform / Project.
/// - SCRUM-55: thống kê số Content (Total / In Progress / Published / Overdue) theo Creator, lọc được theo Creator.
/// Số liệu lấy từ Backend (SQL); control chỉ hiển thị, không tự tính hay tự đổi phạm vi project.
/// </summary>
public sealed class ProjectReportsControl : UserControl
{
    private static readonly (string Key, string Label)[] Dimensions =
    [
        ("content", "Theo Content"),
        ("creator", "Theo Creator"),
        ("platform", "Theo Platform"),
        ("project", "Toàn Project"),
    ];

    private readonly IMetricBackend _backend;
    private readonly IStatisticsBackend? _statisticsBackend;
    private readonly long _projectId;

    private readonly Panel _metricsView;
    private readonly DataGridView _grid;
    private readonly Label _status;
    private readonly Dictionary<string, RoundedButton> _buttons = new();

    private readonly Panel _statsView;
    private readonly DataGridView _statsGrid;
    private readonly Label _statsStatus;
    private readonly ComboBox _creatorFilter;
    private readonly RoundedButton _metricsViewButton;
    private readonly RoundedButton _statsViewButton;
    private readonly List<CreatorFilterOption> _creatorOptions = new();

    private bool _loadingCreators;
    private bool _statsLoaded;
    private string _current = "content";
    private long? SelectedCreatorId => (_creatorFilter.SelectedItem as CreatorFilterOption)?.CreatorId;

    public ProjectReportsControl(IMetricBackend backend, long projectId, IStatisticsBackend? statisticsBackend = null)
    {
        _backend = backend;
        _statisticsBackend = statisticsBackend;
        _projectId = projectId;
        BackColor = UITheme.Neutral50;
        Padding = new Padding(32, 24, 32, 24);

        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = UITheme.Neutral50 };
        header.Controls.Add(new Label
        {
            Text = "Tổng hợp số Content hoàn thành, hiệu suất và tình trạng Published/Overdue theo Creator và toàn Project. "
                 + "Engagement = (Likes + Comments + Shares) / Views × 100.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(1, 40),
            BackColor = Color.Transparent,
        });
        header.Controls.Add(new Label
        {
            Text = "Báo cáo Project",
            Font = UITheme.FontPageTitle,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(0, 0),
            BackColor = Color.Transparent,
        });

        // Chuyển giữa bảng hiệu suất (SCRUM-52) và bảng thống kê Creator (SCRUM-55).
        var viewBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UITheme.Neutral50,
            Padding = new Padding(0, 4, 0, 0),
        };
        _metricsViewButton = new RoundedButton
        {
            Text = "Hiệu suất",
            Style = RoundButtonStyle.Primary,
            Size = new Size(150, 34),
            Margin = new Padding(0, 0, 10, 0),
        };
        _statsViewButton = new RoundedButton
        {
            Text = "Creator & Project",
            Style = RoundButtonStyle.Secondary,
            Size = new Size(170, 34),
            Margin = new Padding(0, 0, 10, 0),
        };
        _metricsViewButton.Click += (_, _) => ShowView(showStatistics: false);
        _statsViewButton.Click += (_, _) => ShowView(showStatistics: true);
        viewBar.Controls.Add(_metricsViewButton);
        viewBar.Controls.Add(_statsViewButton);

        _grid = CreateGrid();
        AddTextColumn(_grid, "name", "Tên", 40, DataGridViewContentAlignment.MiddleLeft);
        AddTextColumn(_grid, "views", "Views", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_grid, "likes", "Likes", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_grid, "comments", "Comments", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_grid, "shares", "Shares", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_grid, "engagement", "Engagement", 14, DataGridViewContentAlignment.MiddleRight);

        _status = CreateStatus();

        var metricsToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UITheme.Neutral50,
            Padding = new Padding(0, 4, 0, 0),
        };
        foreach (var (key, label) in Dimensions)
        {
            string captured = key;
            var button = new RoundedButton
            {
                Text = label,
                Style = RoundButtonStyle.Secondary,
                Size = new Size(140, 34),
                Margin = new Padding(0, 0, 10, 0),
            };
            button.Click += (_, _) => LoadSummary(captured);
            _buttons[key] = button;
            metricsToolbar.Controls.Add(button);
        }

        _metricsView = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Neutral50 };
        _metricsView.Controls.Add(_grid);
        _metricsView.Controls.Add(_status);
        _metricsView.Controls.Add(metricsToolbar);

        // SCRUM-55: bộ lọc Creator chỉ thu hẹp trong project hiện tại; "Tất cả Creator" là mặc định.
        _creatorFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.FontBody,
            Width = 260,
            Height = 30,
            Margin = new Padding(0, 4, 12, 0),
            Enabled = false,
        };
        _creatorFilter.SelectedIndexChanged += (_, _) => { if (!_loadingCreators) LoadStatistics(); };

        var statsToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UITheme.Neutral50,
            Padding = new Padding(0, 4, 0, 0),
        };
        statsToolbar.Controls.Add(new Label
        {
            Text = "Creator",
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Neutral700,
            AutoSize = true,
            Margin = new Padding(0, 9, 8, 0),
        });
        statsToolbar.Controls.Add(_creatorFilter);

        _statsGrid = CreateGrid();
        AddTextColumn(_statsGrid, "name", "Creator", 24, DataGridViewContentAlignment.MiddleLeft);
        AddTextColumn(_statsGrid, "total", "Total", 9, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "inProgress", "In Progress", 10, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "published", "Published", 10, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "overdue", "Overdue", 9, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "views", "Views", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "likes", "Likes", 9, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "comments", "Comments", 9, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "shares", "Shares", 9, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn(_statsGrid, "engagement", "Engagement", 11, DataGridViewContentAlignment.MiddleRight);

        _statsStatus = CreateStatus();

        _statsView = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Neutral50, Visible = false };
        _statsView.Controls.Add(_statsGrid);
        _statsView.Controls.Add(_statsStatus);
        _statsView.Controls.Add(statsToolbar);

        Controls.Add(_metricsView);
        Controls.Add(_statsView);
        Controls.Add(viewBar);
        Controls.Add(header);

        ShowView(showStatistics: false);
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        BackgroundColor = UITheme.White,
        BorderStyle = BorderStyle.None,
        EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = UITheme.Neutral100,
            ForeColor = UITheme.Neutral700,
            Font = UITheme.FontLabelBold,
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = UITheme.FontBody,
            SelectionBackColor = UITheme.Neutral100,
            SelectionForeColor = UITheme.Neutral800,
        },
    };

    private static Label CreateStatus() => new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = UITheme.FontBody,
        ForeColor = UITheme.Neutral600,
        BackColor = UITheme.Neutral50,
        Visible = false,
    };

    private static void AddTextColumn(DataGridView grid, string name, string header, int weight,
        DataGridViewContentAlignment alignment)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            FillWeight = weight,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = alignment },
        });
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadSummary(_current);
    }

    /// <summary>Đổi giữa bảng hiệu suất và bảng thống kê Creator. Bảng thống kê chỉ tải lần đầu mở.</summary>
    private void ShowView(bool showStatistics)
    {
        _metricsView.Visible = !showStatistics;
        _statsView.Visible = showStatistics;
        _metricsViewButton.Style = showStatistics ? RoundButtonStyle.Secondary : RoundButtonStyle.Primary;
        _statsViewButton.Style = showStatistics ? RoundButtonStyle.Primary : RoundButtonStyle.Secondary;

        if (showStatistics && !_statsLoaded)
        {
            _statsLoaded = true;
            LoadStatistics();
        }
    }

    /// <summary>Tải và hiển thị tổng hợp theo chiều <paramref name="groupBy"/> (content/creator/platform/project).</summary>
    public void LoadSummary(string groupBy)
    {
        _current = groupBy;
        UpdateButtons();

        MetricsSummaryResponse response;
        try
        {
            response = _backend.GetMetricsSummary(_projectId, groupBy);
        }
        catch (Exception ex)
        {
            ShowStatus(_status, _grid, "Không tải được số liệu: " + ex.Message);
            return;
        }

        Render(response);
    }

    private void Render(MetricsSummaryResponse response)
    {
        _grid.Rows.Clear();
        foreach (MetricsSummaryRow row in response.Rows)
        {
            // Views = 0: hiển thị "—" để không gây hiểu lầm là "0% tương tác" (AC1).
            string engagement = row.Views == 0 ? "—" : $"{row.EngagementPercent:0.##}%";
            _grid.Rows.Add(row.Name, row.Views.ToString("N0"), row.Likes.ToString("N0"),
                row.Comments.ToString("N0"), row.Shares.ToString("N0"), engagement);
        }

        if (response.Rows.Count == 0)
        {
            ShowStatus(_status, _grid, "Chưa có số liệu nào cho Project này.");
            return;
        }

        _status.Visible = false;
        _grid.Visible = true;
    }

    /// <summary>Tải thống kê Creator theo project hiện tại (và Creator đang chọn, nếu có).</summary>
    public void LoadStatistics()
    {
        if (_statisticsBackend is null)
        {
            ShowStatus(_statsStatus, _statsGrid, "Thống kê cần chạy ở chế độ máy chủ (đã kết nối database).");
            return;
        }

        long? creatorId = SelectedCreatorId;
        CreatorStatisticsResponse response;
        try
        {
            response = _statisticsBackend.GetCreatorStatistics(_projectId, creatorId);
        }
        catch (Exception ex)
        {
            ShowStatus(_statsStatus, _statsGrid, "Không tải được thống kê: " + ex.Message);
            return;
        }

        // Chỉ nạp lại danh sách chọn khi đang xem tất cả (lúc đó response có đủ Creator của project).
        if (creatorId is null) UpdateCreatorOptions(response.Creators);
        RenderStatistics(response, creatorId is not null);
    }

    private void UpdateCreatorOptions(IReadOnlyList<CreatorStatisticsRow> creators)
    {
        _loadingCreators = true;
        try
        {
            _creatorOptions.Clear();
            _creatorOptions.Add(new CreatorFilterOption(null, "Tất cả Creator"));
            _creatorOptions.AddRange(creators.Select(c => new CreatorFilterOption(c.CreatorId, c.Name)));

            _creatorFilter.Items.Clear();
            foreach (CreatorFilterOption option in _creatorOptions)
                _creatorFilter.Items.Add(option);
            _creatorFilter.SelectedIndex = 0;
            _creatorFilter.Enabled = creators.Count > 0;
        }
        finally
        {
            _loadingCreators = false;
        }
    }

    private void RenderStatistics(CreatorStatisticsResponse response, bool filteredByCreator)
    {
        _statsGrid.Rows.Clear();

        CreatorStatisticsRow project = response.Project;
        int projectRow = _statsGrid.Rows.Add(project.Name, project.Total.ToString("N0"),
            project.InProgress.ToString("N0"), project.Published.ToString("N0"), project.Overdue.ToString("N0"),
            project.Views.ToString("N0"), project.Likes.ToString("N0"), project.Comments.ToString("N0"),
            project.Shares.ToString("N0"), EngagementText(project));
        _statsGrid.Rows[projectRow].DefaultCellStyle.Font = UITheme.FontBodyBold;

        foreach (CreatorStatisticsRow row in response.Creators)
        {
            _statsGrid.Rows.Add(row.Name, row.Total.ToString("N0"), row.InProgress.ToString("N0"),
                row.Published.ToString("N0"), row.Overdue.ToString("N0"),
                row.Views.ToString("N0"), row.Likes.ToString("N0"), row.Comments.ToString("N0"),
                row.Shares.ToString("N0"), EngagementText(row));
        }

        if (response.Creators.Count == 0)
        {
            ShowStatus(_statsStatus, _statsGrid, filteredByCreator
                ? "Creator này chưa có Content nào trong Project."
                : "Project chưa có Creator nào có Content.");
            return;
        }

        _statsStatus.Visible = false;
        _statsGrid.Visible = true;
    }

    private static string EngagementText(CreatorStatisticsRow row) =>
        row.Views == 0 ? "—" : $"{row.EngagementPercent:0.##}%";

    private static void ShowStatus(Label status, DataGridView grid, string message)
    {
        grid.Visible = false;
        status.Text = message;
        status.Visible = true;
    }

    private void UpdateButtons()
    {
        foreach (var (key, button) in _buttons)
            button.Style = key == _current ? RoundButtonStyle.Primary : RoundButtonStyle.Secondary;
    }

    /// <summary>Một lựa chọn trong combo Creator; <see cref="CreatorId"/> null = tất cả Creator của project.</summary>
    private sealed record CreatorFilterOption(long? CreatorId, string Label)
    {
        public override string ToString() => Label;
    }
}
