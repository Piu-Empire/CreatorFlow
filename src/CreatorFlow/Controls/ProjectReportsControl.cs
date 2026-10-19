using CreatorFlow.Contracts.Metrics;
using CreatorFlow.Services.Backends;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Màn Báo cáo Metrics của Project (SCRUM-52): tổng hợp Views/Likes/Comments/Shares và Engagement
/// theo Content / Creator / Platform / toàn Project. Số liệu lấy từ Backend (SQL); control chỉ hiển thị.
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
    private readonly long _projectId;
    private readonly DataGridView _grid;
    private readonly Label _status;
    private readonly Dictionary<string, RoundedButton> _buttons = new();

    private string _current = "content";

    public ProjectReportsControl(IMetricBackend backend, long projectId)
    {
        _backend = backend;
        _projectId = projectId;
        BackColor = UITheme.Neutral50;
        Padding = new Padding(32, 24, 32, 24);

        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = UITheme.Neutral50 };
        header.Controls.Add(new Label
        {
            Text = "Tổng hợp hiệu suất theo Content, Creator, Platform và toàn Project. Engagement = (Likes + Comments + Shares) / Views × 100.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(1, 40),
            BackColor = Color.Transparent,
        });
        header.Controls.Add(new Label
        {
            Text = "Báo cáo Metrics",
            Font = UITheme.FontPageTitle,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(0, 0),
            BackColor = Color.Transparent,
        });

        var toolbar = new FlowLayoutPanel
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
            toolbar.Controls.Add(button);
        }

        _grid = new DataGridView
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
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Neutral100;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.Neutral700;
        _grid.ColumnHeadersDefaultCellStyle.Font = UITheme.FontLabelBold;
        _grid.DefaultCellStyle.Font = UITheme.FontBody;
        _grid.DefaultCellStyle.SelectionBackColor = UITheme.Neutral100;
        _grid.DefaultCellStyle.SelectionForeColor = UITheme.Neutral800;
        AddTextColumn("name", "Tên", 40, DataGridViewContentAlignment.MiddleLeft);
        AddTextColumn("views", "Views", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn("likes", "Likes", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn("comments", "Comments", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn("shares", "Shares", 12, DataGridViewContentAlignment.MiddleRight);
        AddTextColumn("engagement", "Engagement", 14, DataGridViewContentAlignment.MiddleRight);

        _status = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            BackColor = UITheme.Neutral50,
            Visible = false,
        };

        Controls.Add(_grid);
        Controls.Add(_status);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private void AddTextColumn(string name, string header, int weight, DataGridViewContentAlignment alignment)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
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
            ShowStatus("Không tải được số liệu: " + ex.Message);
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
            ShowStatus("Chưa có số liệu nào cho Project này.");
            return;
        }

        _status.Visible = false;
        _grid.Visible = true;
    }

    private void ShowStatus(string message)
    {
        _grid.Visible = false;
        _status.Text = message;
        _status.Visible = true;
    }

    private void UpdateButtons()
    {
        foreach (var (key, button) in _buttons)
            button.Style = key == _current ? RoundButtonStyle.Primary : RoundButtonStyle.Secondary;
    }
}
