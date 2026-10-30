using CreatorFlow.Contracts.Metrics;
using CreatorFlow.Contracts.Statistics;
using CreatorFlow.Controls.Charts;
using CreatorFlow.Models;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Backends;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Màn Báo cáo của Project:
/// - SCRUM-52: hiệu suất Views/Likes/Comments/Shares và Engagement theo Content / Creator / Platform / Project.
/// - SCRUM-55: thống kê số Content (Total / In Progress / Published / Overdue) theo Creator, lọc được theo Creator.
/// - SCRUM-60: AI nhận xét hiệu suất dựa trên đúng số liệu đã lưu (không tự lấy dữ liệu nền tảng, không ghi DB).
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
    private readonly IPerformanceAiService? _performanceAi;
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
    private readonly RoundedButton _chartsViewButton = new();
    private readonly RoundedButton _aiAnalyzeButton = new();
    private readonly RoundedButton _aiCopyButton = new();
    private readonly RoundedButton _aiCloseButton = new();
    private Panel _aiPanel = null!;
    private Label _aiScopeLabel = null!;
    private Label _aiSourceLabel = null!;
    private Label _aiStatus = null!;
    private RichTextBox _aiText = null!;
    private readonly List<CreatorFilterOption> _creatorOptions = new();

    // SCRUM-54: view Biểu đồ dùng dữ liệu Board + Metrics đã lưu; mỗi ô là một BarChartPanel.
    private readonly Panel _chartsView;
    private readonly Label _chartsStatus;
    private readonly BarChartPanel _statusChart = new();
    private readonly BarChartPanel _platformChart = new();
    private readonly BarChartPanel _timelineChart = new();
    private readonly BarChartPanel _performanceChart = new();
    private readonly IBoardBackend? _boardBackend;
    private CancellationTokenSource? _chartCts;
    private bool _chartsLoaded;

    private CancellationTokenSource? _aiCts;
    private bool _aiBusy;
    private bool _loadingCreators;
    private bool _statsLoaded;
    private string _current = "content";
    private long? SelectedCreatorId => (_creatorFilter.SelectedItem as CreatorFilterOption)?.CreatorId;

    public ProjectReportsControl(
        IMetricBackend backend, long projectId, IStatisticsBackend? statisticsBackend = null,
        IPerformanceAiService? performanceAi = null, IBoardBackend? boardBackend = null)
    {
        _backend = backend;
        _statisticsBackend = statisticsBackend;
        _performanceAi = performanceAi;
        _boardBackend = boardBackend;
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
        var viewBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UITheme.Neutral50,
            Padding = new Padding(0, 4, 4, 0),
        };
        viewBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        viewBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var viewButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UITheme.Neutral50,
            Margin = Padding.Empty,
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
        _chartsViewButton.Text = "Biểu đồ";
        _chartsViewButton.Style = RoundButtonStyle.Secondary;
        _chartsViewButton.Size = new Size(120, 34);
        _chartsViewButton.Margin = new Padding(0, 0, 10, 0);
        _metricsViewButton.Click += (_, _) => ShowView(ReportView.Metrics);
        _statsViewButton.Click += (_, _) => ShowView(ReportView.Statistics);
        _chartsViewButton.Click += (_, _) => ShowView(ReportView.Charts);
        viewButtons.Controls.Add(_metricsViewButton);
        viewButtons.Controls.Add(_statsViewButton);
        viewButtons.Controls.Add(_chartsViewButton);

        // SCRUM-60: điểm vào AI phân tích, đặt bên phải thanh view.
        _aiAnalyzeButton.Text = "AI phân tích";
        _aiAnalyzeButton.Style = RoundButtonStyle.Secondary;
        _aiAnalyzeButton.Size = new Size(130, 34);
        _aiAnalyzeButton.Margin = new Padding(0, 0, 2, 0);
        _aiAnalyzeButton.Enabled = _performanceAi?.IsConfigured == true;
        _aiAnalyzeButton.Click += async (_, _) => await AnalyzeAsync();

        viewBar.Controls.Add(viewButtons, 0, 0);
        viewBar.Controls.Add(_aiAnalyzeButton, 1, 0);

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

        // SCRUM-54: lưới 2×2 biểu đồ; đặt trong panel cuộn để cửa sổ nhỏ không làm vỡ layout.
        var chartsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 680,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = UITheme.Neutral50,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };
        chartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        chartsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        chartsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        chartsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        AddChartPanel(chartsGrid, _statusChart, 0, 0);
        AddChartPanel(chartsGrid, _platformChart, 1, 0);
        AddChartPanel(chartsGrid, _timelineChart, 0, 1);
        AddChartPanel(chartsGrid, _performanceChart, 1, 1);

        var chartsScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UITheme.Neutral50,
        };
        chartsScroll.Controls.Add(chartsGrid);

        _chartsStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            BackColor = UITheme.Neutral50,
            Visible = false,
        };

        _chartsView = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Neutral50, Visible = false };
        _chartsView.Controls.Add(chartsScroll);
        _chartsView.Controls.Add(_chartsStatus);

        Controls.Add(_metricsView);
        Controls.Add(_statsView);
        Controls.Add(_chartsView);
        Controls.Add(BuildAiPanel());
        Controls.Add(viewBar);
        Controls.Add(header);

        ShowView(ReportView.Metrics);
    }

    private static void AddChartPanel(TableLayoutPanel grid, Control panel, int column, int row)
    {
        panel.Dock = DockStyle.Fill;
        panel.Margin = new Padding(0, 0, column == 0 ? 12 : 0, row == 0 ? 12 : 0);
        grid.Controls.Add(panel, column, row);
    }

    /// <summary>SCRUM-60: panel kết quả AI dưới bảng; ẩn cho tới khi người dùng bấm "AI phân tích".</summary>
    private Panel BuildAiPanel()
    {
        _aiPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 320,
            BackColor = UITheme.White,
            Padding = new Padding(0, 10, 0, 12),
            Visible = false,
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = UITheme.White,
            Padding = Padding.Empty,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // đường kẻ phân cách
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // tiêu đề + nguồn dữ liệu
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // trạng thái
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // nội dung AI
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // nút

        layout.Controls.Add(new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = UITheme.Neutral200,
            Margin = new Padding(0, 0, 0, 10),
        }, 0, 0);

        var title = new Label
        {
            Text = "AI phân tích hiệu suất",
            Font = UITheme.FontH3,
            ForeColor = UITheme.Neutral900,
            AutoSize = true,
            Margin = Padding.Empty,
        };

        _aiScopeLabel = new Label
        {
            Text = string.Empty,
            Font = UITheme.FontLabelBold,
            ForeColor = UITheme.Neutral700,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
        };

        // AC2: nói rõ nguồn số liệu và việc AI không truy cập nền tảng mạng xã hội.
        _aiSourceLabel = new Label
        {
            Text = string.Empty,
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Margin = new Padding(0, 2, 0, 8),
        };

        var head = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = UITheme.White,
            Margin = Padding.Empty,
        };
        head.Controls.Add(title);
        head.Controls.Add(_aiScopeLabel);
        head.Controls.Add(_aiSourceLabel);
        layout.Controls.Add(head, 0, 1);

        _aiStatus = new Label
        {
            Text = string.Empty,
            Font = UITheme.FontLabel,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };
        layout.Controls.Add(_aiStatus, 0, 2);

        _aiText = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = UITheme.Neutral50,
            ForeColor = UITheme.Neutral800,
            Font = UITheme.FontBody,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            DetectUrls = false,
            Margin = new Padding(0, 0, 0, 4),
        };
        layout.Controls.Add(_aiText, 0, 3);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = UITheme.White,
            Margin = new Padding(0, 10, 0, 0),
        };

        _aiCloseButton.Text = "Đóng";
        _aiCloseButton.Style = RoundButtonStyle.Secondary;
        _aiCloseButton.Size = new Size(100, 34);
        _aiCloseButton.Margin = Padding.Empty;
        _aiCloseButton.Click += (_, _) => CloseAiPanel();

        _aiCopyButton.Text = "Sao chép";
        _aiCopyButton.Style = RoundButtonStyle.Secondary;
        _aiCopyButton.Size = new Size(110, 34);
        _aiCopyButton.Margin = new Padding(0, 0, 8, 0);
        _aiCopyButton.Enabled = false;
        _aiCopyButton.Click += (_, _) => CopyAiResult();

        footer.Controls.Add(_aiCloseButton);
        footer.Controls.Add(_aiCopyButton);
        layout.Controls.Add(footer, 0, 4);

        _aiPanel.Controls.Add(layout);
        return _aiPanel;
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

    /// <summary>Đổi giữa bảng hiệu suất, bảng thống kê Creator và lưới biểu đồ. Mỗi view chỉ tải lần đầu mở.</summary>
    private void ShowView(ReportView view)
    {
        _metricsView.Visible = view == ReportView.Metrics;
        _statsView.Visible = view == ReportView.Statistics;
        _chartsView.Visible = view == ReportView.Charts;
        _metricsViewButton.Style = view == ReportView.Metrics ? RoundButtonStyle.Primary : RoundButtonStyle.Secondary;
        _statsViewButton.Style = view == ReportView.Statistics ? RoundButtonStyle.Primary : RoundButtonStyle.Secondary;
        _chartsViewButton.Style = view == ReportView.Charts ? RoundButtonStyle.Primary : RoundButtonStyle.Secondary;

        if (view == ReportView.Statistics && !_statsLoaded)
        {
            _statsLoaded = true;
            LoadStatistics();
        }

        if (view == ReportView.Charts && !_chartsLoaded)
        {
            _chartsLoaded = true;
            _ = LoadChartsAsync();
        }
    }

    /// <summary>
    /// SCRUM-54: tải card của Board và 2 tổng hợp Metrics (content/platform) rồi vẽ 4 biểu đồ.
    /// Card lỗi → cả view báo lỗi; Metrics lỗi → chỉ ô hiệu suất báo lỗi, 3 biểu đồ card vẫn hiện.
    /// </summary>
    private async Task LoadChartsAsync()
    {
        if (_boardBackend is null)
        {
            SetChartsUnavailable("Biểu đồ cần chạy ở chế độ máy chủ (đã kết nối database).", UITheme.Neutral600);
            return;
        }

        _chartsStatus.Text = "Đang tải biểu đồ...";
        _chartsStatus.ForeColor = UITheme.Neutral600;
        _chartsStatus.Visible = true;

        _chartCts?.Cancel();
        _chartCts = new CancellationTokenSource();
        CancellationToken token = _chartCts.Token;

        try
        {
            List<ContentBoardCard> cards = await _boardBackend.GetBoardCardsAsync(_projectId, token);

            string? metricsError = null;
            MetricsSummaryResponse? contentSummary = null;
            MetricsSummaryResponse? platformSummary = null;

            await Task.Run(() =>
            {
                try
                {
                    contentSummary = _backend.GetMetricsSummary(_projectId, "content");
                }
                catch (Exception ex)
                {
                    metricsError = ex.Message;
                }

                try
                {
                    platformSummary = _backend.GetMetricsSummary(_projectId, "platform");
                }
                catch (Exception ex)
                {
                    metricsError ??= ex.Message;
                }
            }, token);

            token.ThrowIfCancellationRequested();

            BindCharts(cards, contentSummary, platformSummary, metricsError);
            _chartsStatus.Visible = false;
        }
        catch (OperationCanceledException)
        {
            // Rời trang/đóng control: không hiện lỗi.
        }
        catch (Exception ex)
        {
            SetChartsUnavailable("Không tải được dữ liệu biểu đồ: " + ex.Message, UITheme.Danger);
        }
    }

    private void SetChartsUnavailable(string message, Color color)
    {
        _statusChart.SetEmpty("Phân bố trạng thái Content", message);
        _platformChart.SetEmpty("Content theo Platform", message);
        _timelineChart.SetEmpty("Kế hoạch publish theo tháng", message);
        _performanceChart.SetEmpty("Hiệu suất Content & Platform", message);
        _chartsStatus.Text = message;
        _chartsStatus.ForeColor = color;
        _chartsStatus.Visible = true;
    }

    private void BindCharts(
        List<ContentBoardCard> cards,
        MetricsSummaryResponse? contentSummary,
        MetricsSummaryResponse? platformSummary,
        string? metricsError)
    {
        StatusChartData status = ChartDataBuilder.BuildStatus(cards);
        _statusChart.SetBars(
            "Phân bố trạng thái Content",
            $"Tổng {status.Total:N0} Content trên Board (không gồm Archived).",
            status.Buckets.Select(bucket => new ChartBar(
                UITheme.GetStageDisplayName(bucket.Status),
                bucket.Count,
                bucket.Count.ToString("N0"),
                $"{bucket.Percent:0.#}%",
                UITheme.GetStageColor(bucket.Status))).ToList());

        PlatformChartData platforms = ChartDataBuilder.BuildPlatforms(cards);
        string platformCaption = platforms.Untagged > 0
            ? $"{platforms.Total:N0} Content; {platforms.Untagged:N0} chưa gắn Platform. Một Content có thể thuộc nhiều Platform."
            : $"{platforms.Total:N0} Content. Một Content có thể thuộc nhiều Platform.";
        _platformChart.SetBars(
            "Content theo Platform",
            platformCaption,
            platforms.Buckets.Select((bucket, index) => new ChartBar(
                bucket.Name,
                bucket.Count,
                bucket.Count.ToString("N0"),
                null,
                BarPalette[index % BarPalette.Length])).ToList());

        TimelineChartData timeline = ChartDataBuilder.BuildTimeline(cards);
        string timelineCaption = timeline.NoDate > 0
            ? $"Theo PlannedPublishAt (thiếu thì Deadline); {timeline.NoDate:N0} Content chưa có mốc thời gian."
            : "Theo PlannedPublishAt của Content (thiếu thì Deadline).";
        _timelineChart.SetBars(
            "Kế hoạch publish theo tháng",
            timelineCaption,
            timeline.Buckets.Select(bucket => new ChartBar(
                bucket.Month.ToString("MM/yyyy"),
                bucket.Count,
                bucket.Count.ToString("N0"),
                null,
                UITheme.Neutral700)).ToList());

        if (contentSummary is null || platformSummary is null)
        {
            _performanceChart.SetEmpty(
                "Hiệu suất Content & Platform",
                "Không tải được Metrics: " + (metricsError ?? "không có dữ liệu."));
            return;
        }

        PerformanceChartData performance = ChartDataBuilder.BuildPerformance(contentSummary, platformSummary);
        if (performance.TopContents.Count == 0 && performance.Platforms.Count == 0)
        {
            _performanceChart.SetEmpty("Hiệu suất Content & Platform", "Chưa có Metrics đã lưu cho Project.");
            return;
        }

        string performanceCaption = performance.ContentTotal == 0
            ? "Chưa có Metrics đã lưu cho Project."
            : $"Metrics đã lưu. Top {performance.TopContents.Count:N0}/{performance.ContentTotal:N0} Content theo Views.";
        _performanceChart.SetData(
            "Hiệu suất Content & Platform",
            performanceCaption,
            new[]
            {
                new ChartSection("Top Content theo Views",
                    performance.TopContents.Select(row => new ChartBar(
                        row.Name,
                        row.Views,
                        row.Views.ToString("N0"),
                        EngagementHint(row),
                        UITheme.Info)).ToList()),
                new ChartSection("Platform theo Views",
                    performance.Platforms.Select((row, index) => new ChartBar(
                        row.Name,
                        row.Views,
                        row.Views.ToString("N0"),
                        EngagementHint(row),
                        BarPalette[index % BarPalette.Length])).ToList()),
            });
    }

    private static string? EngagementHint(MetricsSummaryRow row) =>
        row.Views == 0 ? null : $"{row.EngagementPercent:0.##}%";

    private static readonly Color[] BarPalette =
    [
        UITheme.Info,
        UITheme.AccentPurple,
        UITheme.Success,
        UITheme.Warning,
        UITheme.StageScript,
        UITheme.Neutral700,
    ];

    private enum ReportView
    {
        Metrics,
        Statistics,
        Charts,
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

    /// <summary>SCRUM-60: gọi AI trên đúng số liệu của view đang mở. Không ghi dữ liệu, không đổi Metrics.</summary>
    private async Task AnalyzeAsync()
    {
        if (_aiBusy || _performanceAi is not { IsConfigured: true } ai)
            return;

        PerformanceAiRequest request;
        try
        {
            request = BuildAiRequest();
        }
        catch (Exception ex)
        {
            // Lỗi khi tải số liệu: báo rõ và KHÔNG gửi dữ liệu rỗng cho AI (AC3).
            _aiPanel.Visible = true;
            _aiScopeLabel.Text = string.Empty;
            _aiSourceLabel.Text = string.Empty;
            _aiText.Clear();
            _aiCopyButton.Enabled = false;
            ShowAiStatus("Không tải được số liệu: " + ex.Message, UITheme.Danger);
            return;
        }

        _aiPanel.Visible = true;
        _aiScopeLabel.Text = "Phạm vi: " + request.ScopeLabel;
        _aiSourceLabel.Text = SourceText(request.LoadedAt);
        _aiText.Clear();
        _aiCopyButton.Enabled = false;
        ShowAiStatus("Đang phân tích...", UITheme.Neutral600);
        SetAiBusy(true);

        // Chỉ hủy token cũ; không Dispose ngay để không cắt ngang lời gọi đang dở.
        _aiCts?.Cancel();
        _aiCts = new CancellationTokenSource();
        try
        {
            PerformanceAiResponse response = await ai.AnalyzeAsync(request, _aiCts.Token);
            if (!response.Succeeded)
            {
                ShowAiStatus(response.ErrorMessage ?? "Không nhận được kết quả từ AI.", UITheme.Danger);
                return;
            }

            RenderAi(response);
        }
        catch (OperationCanceledException)
        {
            // Người dùng đóng panel/hủy yêu cầu: không hiện lỗi.
        }
        catch (Exception)
        {
            ShowAiStatus("Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau.", UITheme.Danger);
        }
        finally
        {
            SetAiBusy(false);
        }
    }

    /// <summary>Chốt phạm vi + số liệu tại thời điểm gọi, để kết quả cũ không bị hiểu theo bộ lọc mới.</summary>
    private PerformanceAiRequest BuildAiRequest() =>
        _statsView.Visible ? BuildCreatorAiRequest() : BuildMetricsAiRequest();

    private PerformanceAiRequest BuildMetricsAiRequest()
    {
        MetricsSummaryResponse response = _backend.GetMetricsSummary(_projectId, _current);
        List<PerformanceMetricLine> lines = response.Rows
            .Select(row => new PerformanceMetricLine(
                row.Name, row.Views, row.Likes, row.Comments, row.Shares, row.EngagementPercent))
            .ToList();

        return new PerformanceAiRequest
        {
            ProjectId = _projectId,
            ProjectName = ProjectNameForPrompt(),
            Scope = PerformanceScope.Metrics,
            ScopeLabel = DimensionLabel(_current),
            GroupBy = _current,
            LoadedAt = DateTime.Now,
            Metrics = lines,
            IsTruncated = lines.Count > PerformanceAiPromptBuilder.MaxRows,
        };
    }

    private PerformanceAiRequest BuildCreatorAiRequest()
    {
        if (_statisticsBackend is null)
            throw new InvalidOperationException("Thống kê cần chạy ở chế độ máy chủ (đã kết nối database).");

        long? creatorId = SelectedCreatorId;
        CreatorStatisticsResponse response = _statisticsBackend.GetCreatorStatistics(_projectId, creatorId);
        List<PerformanceCreatorLine> lines = response.Creators.Select(ToPerformanceLine).ToList();

        string scopeLabel = creatorId is null
            ? "Toàn Project"
            : "Creator: " + (_creatorOptions.FirstOrDefault(o => o.CreatorId == creatorId)?.Label ?? $"#{creatorId}");

        return new PerformanceAiRequest
        {
            ProjectId = _projectId,
            ProjectName = ProjectNameForPrompt(),
            Scope = PerformanceScope.Creator,
            ScopeLabel = scopeLabel,
            LoadedAt = DateTime.Now,
            Creators = lines,
            ProjectTotals = ToPerformanceLine(response.Project),
            IsTruncated = lines.Count > PerformanceAiPromptBuilder.MaxRows,
        };
    }

    private static PerformanceCreatorLine ToPerformanceLine(CreatorStatisticsRow row) => new(
        row.Name, row.Total, row.InProgress, row.Published, row.Overdue,
        row.Views, row.Likes, row.Comments, row.Shares, row.EngagementPercent);

    private static string DimensionLabel(string key) =>
        Dimensions.FirstOrDefault(d => d.Key == key).Label ?? key;

    private static string ProjectNameForPrompt() =>
        string.IsNullOrWhiteSpace(CurrentSession.CurrentProjectName)
            ? $"Project #{CurrentSession.CurrentProjectId}"
            : CurrentSession.CurrentProjectName;

    private static string SourceText(DateTime loadedAt) =>
        $"Dựa trên Metrics đã lưu trong hệ thống (tải lúc {loadedAt:HH:mm dd/MM/yyyy}). "
        + "AI không truy cập nền tảng mạng xã hội.";

    private void RenderAi(PerformanceAiResponse response)
    {
        _aiText.Clear();
        AppendAiHeading("NHẬN XÉT");
        AppendAiBody(string.IsNullOrWhiteSpace(response.Remarks) ? "(Không có nhận xét.)" : response.Remarks);

        if (response.Suggestions.Count > 0)
        {
            AppendAiHeading("GỢI Ý CẢI THIỆN");
            foreach (string suggestion in response.Suggestions)
                AppendAiBody("• " + suggestion);
        }

        _aiCopyButton.Enabled = true;
        if (response.IsStructured)
            ShowAiStatus("Dựa trên số liệu đã tải; nội dung chỉ để tham khảo, không thay đổi dữ liệu.", UITheme.Neutral600);
        else
            ShowAiStatus("AI trả về không đúng định dạng, vui lòng kiểm tra lại nội dung.", UITheme.Warning);
    }

    private void AppendAiHeading(string text)
    {
        _aiText.SelectionStart = _aiText.TextLength;
        _aiText.SelectionLength = 0;
        _aiText.SelectionFont = UITheme.FontBodyBold;
        _aiText.SelectionColor = UITheme.Neutral900;
        _aiText.AppendText(text + Environment.NewLine);
        _aiText.SelectionFont = UITheme.FontBody;
        _aiText.SelectionColor = UITheme.Neutral800;
    }

    private void AppendAiBody(string text)
    {
        _aiText.SelectionStart = _aiText.TextLength;
        _aiText.SelectionLength = 0;
        _aiText.SelectionFont = UITheme.FontBody;
        _aiText.SelectionColor = UITheme.Neutral800;
        _aiText.AppendText(text + Environment.NewLine);
    }

    private void ShowAiStatus(string message, Color color)
    {
        _aiStatus.Text = message;
        _aiStatus.ForeColor = color;
    }

    private void SetAiBusy(bool busy)
    {
        _aiBusy = busy;
        _aiAnalyzeButton.Enabled = !busy && _performanceAi?.IsConfigured == true;
        _aiCopyButton.Enabled = !busy && _aiText.TextLength > 0;
        UseWaitCursor = busy;
    }

    private void CopyAiResult()
    {
        try
        {
            if (_aiText.TextLength == 0) return;
            Clipboard.SetText(_aiText.Text);
            ShowAiStatus("Đã sao chép nội dung phân tích.", UITheme.Success);
        }
        catch (Exception)
        {
            ShowAiStatus("Không sao chép được vào clipboard.", UITheme.Danger);
        }
    }

    private void CloseAiPanel()
    {
        _aiCts?.Cancel();
        _aiPanel.Visible = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _aiCts?.Cancel();
            _aiCts?.Dispose();
            _aiCts = null;

            _chartCts?.Cancel();
            _chartCts?.Dispose();
            _chartCts = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>Một lựa chọn trong combo Creator; <see cref="CreatorId"/> null = tất cả Creator của project.</summary>
    private sealed record CreatorFilterOption(long? CreatorId, string Label)
    {
        public override string ToString() => Label;
    }
}