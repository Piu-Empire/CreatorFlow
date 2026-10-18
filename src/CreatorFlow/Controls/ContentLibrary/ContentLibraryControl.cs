using System.Drawing.Drawing2D;
using CreatorFlow.Contracts.Board;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls.ContentLibrary;

/// <summary>Project-wide Content library with real status, platform, assignee and detail navigation.</summary>
public sealed partial class ContentLibraryControl : UserControl
{
    private const string AllStatuses = "Mọi trạng thái";
    private const string AllPlatforms = "Mọi nền tảng";

    private List<ContentBoardCard> contents = [];
    private IReadOnlyList<ContentBoardCard>? serverFilteredContents;
    private string? loadError;
    private bool loading = true;
    private bool updatingFilters;
    private readonly ComboBox creatorFilter = CreateFilterCombo("creatorFilter", "Lọc theo Creator được giao");
    private readonly ComboBox priorityFilter = CreateFilterCombo("priorityFilter", "Lọc theo độ ưu tiên");
    private readonly ComboBox deadlineModeFilter = CreateFilterCombo("deadlineModeFilter", "Lọc theo deadline");
    private readonly DateTimePicker deadlineFromFilter = CreateDateFilter("deadlineFromFilter", "Deadline từ ngày");
    private readonly DateTimePicker deadlineToFilter = CreateDateFilter("deadlineToFilter", "Deadline đến ngày");
    private readonly ComboBox tagFilter = CreateFilterCombo("tagFilter", "Lọc theo tag");
    private readonly ComboBox sortFilter = CreateFilterCombo("sortFilter", "Sắp xếp theo trường");
    private readonly Button sortDirection = new()
    {
        Name = "sortDirection",
        Text = "Tăng dần ↑",
        AccessibleName = "Đổi thứ tự sắp xếp",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = Color.FromArgb(42, 57, 81),
        Font = UITheme.FontLabel,
        Margin = Padding.Empty,
        Cursor = Cursors.Hand,
    };
    private readonly System.Windows.Forms.Timer queryDebounceTimer;

    public event Action<long>? OpenContentRequested;
    public event Action<ContentLibraryQuery>? QueryRequested;

    public int TotalContentCount => contents.Count;

    public ContentLibraryControl(string projectName)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        InitializeComponent();
        Dock = DockStyle.Fill;
        Font = UITheme.FontBody;
        projectNameLabel.Text = projectName;
        ConfigureFilterToolbar();
        ApplyTheme();

        components ??= new System.ComponentModel.Container();
        queryDebounceTimer = new System.Windows.Forms.Timer(components) { Interval = 300 };
        queryDebounceTimer.Tick += (_, _) =>
        {
            queryDebounceTimer.Stop();
            QueryRequested?.Invoke(BuildQuery());
        };
        foreach (ComboBox filter in new[] { creatorFilter, priorityFilter, deadlineModeFilter, tagFilter, sortFilter })
        {
            filter.SelectedIndexChanged += Filter_Changed;
            filter.DrawItem += Filter_DrawItem;
        }
        deadlineFromFilter.ValueChanged += Filter_Changed;
        deadlineToFilter.ValueChanged += Filter_Changed;
        foreach (DateTimePicker picker in new[] { deadlineFromFilter, deadlineToFilter })
        {
            picker.CloseUp += Filter_Changed;
            picker.MouseUp += Filter_Changed;
            picker.KeyUp += Filter_Changed;
        }
        sortDirection.Click += (_, _) =>
        {
            sortDirection.Text = sortDirection.Text.StartsWith("Tăng", StringComparison.Ordinal) ? "Giảm dần ↓" : "Tăng dần ↑";
            Filter_Changed(this, EventArgs.Empty);
        };
        SetFilterDefaults();
        SetLoading();
    }

    private void ConfigureFilterToolbar()
    {
        filters.SuspendLayout();
        filters.Controls.Clear();
        filters.ColumnStyles.Clear();
        filters.RowStyles.Clear();
        filters.ColumnCount = 1;
        filters.RowCount = 2;
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        filters.Padding = Padding.Empty;

        var primary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.White };
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146F));
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154F));
        primary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68F));
        primary.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        searchSurface.Margin = new Padding(1, 1, 8, 1);
        statusSurface.Margin = new Padding(2, 1, 2, 1);
        platformSurface.Margin = new Padding(2, 1, 2, 1);
        countSurface.Margin = new Padding(2, 1, 0, 1);
        primary.Controls.Add(searchSurface, 0, 0);
        primary.Controls.Add(statusSurface, 1, 0);
        primary.Controls.Add(platformSurface, 2, 0);
        primary.Controls.Add(countSurface, 3, 0);

        var secondary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 8, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.White };
        float[] widths = [16F, 12F, 14F, 12F, 12F, 12F, 15F, 7F];
        foreach (float width in widths) secondary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
        secondary.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        AddFilterSurface(secondary, creatorFilter, "Creator", 0);
        AddFilterSurface(secondary, priorityFilter, "Ưu tiên", 1);
        AddFilterSurface(secondary, deadlineModeFilter, "Deadline", 2);
        AddFilterSurface(secondary, deadlineFromFilter, "Từ ngày", 3);
        AddFilterSurface(secondary, deadlineToFilter, "Đến ngày", 4);
        AddFilterSurface(secondary, tagFilter, "Tag", 5);
        AddFilterSurface(secondary, sortFilter, "Sắp xếp", 6);
        AddFilterSurface(secondary, sortDirection, "Thứ tự", 7);

        filters.Controls.Add(primary, 0, 0);
        filters.Controls.Add(secondary, 0, 1);
        if (layout.RowStyles.Count > 1) layout.RowStyles[1].Height = 96F;
        toolbar.Padding = new Padding(10, 7, 10, 7);
        toolbar.Margin = new Padding(0, 1, 0, 6);
        search.PlaceholderText = "Tìm theo tiêu đề hoặc mã Content";
        filters.ResumeLayout(true);
    }

    private static void AddFilterSurface(TableLayoutPanel parent, Control child, string accessibleName, int column)
    {
        var surface = new RoundedSurfacePanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderColor = Color.FromArgb(231, 236, 245),
            Radius = 9,
            Padding = new Padding(child is DateTimePicker ? 1 : 4, 2, child is DateTimePicker ? 1 : 3, 2),
            Margin = new Padding(2, 1, 2, 1),
            AccessibleName = accessibleName,
        };
        child.Dock = DockStyle.Fill;
        child.Margin = Padding.Empty;
        surface.Controls.Add(child);
        parent.Controls.Add(surface, column, 0);
    }

    private static ComboBox CreateFilterCombo(string name, string accessibleName) => new()
    {
        Name = name,
        AccessibleName = accessibleName,
        BackColor = Color.White,
        ForeColor = Color.FromArgb(42, 57, 81),
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        IntegralHeight = false,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 24,
        Font = UITheme.FontLabel,
        Margin = Padding.Empty,
    };

    private static DateTimePicker CreateDateFilter(string name, string accessibleName) => new()
    {
        Name = name,
        AccessibleName = accessibleName,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        ShowCheckBox = true,
        Checked = false,
        Value = DateTime.Today,
        Dock = DockStyle.Fill,
        Font = UITheme.FontLabel,
        Margin = Padding.Empty,
    };

    private void SetFilterDefaults()
    {
        updatingFilters = true;
        try
        {
            Populate(deadlineModeFilter, new object[] { new DeadlineFilterOption(null, "Mọi deadline"), new DeadlineFilterOption(true, "Có deadline"), new DeadlineFilterOption(false, "Chưa có") });
            Populate(sortFilter, new object[]
            {
                new TextFilterOption(null, "Mới thêm"),
                new TextFilterOption("title", "Tiêu đề"),
                new TextFilterOption("platform", "Nền tảng"),
                new TextFilterOption("status", "Trạng thái"),
                new TextFilterOption("creator", "Creator"),
                new TextFilterOption("priority", "Ưu tiên"),
                new TextFilterOption("deadline", "Deadline"),
                new TextFilterOption("tag", "Tag"),
            });
        }
        finally { updatingFilters = false; }
    }

    private void ApplyTheme()
    {
        titleLabel.Font = UITheme.FontPageTitle;
        titleLabel.ForeColor = UITheme.Ink;
        workspaceBadge.Font = UITheme.FontMicro;
        projectNameLabel.Font = dotLabel.Font = descriptionLabel.Font = UITheme.FontBody;
        search.Font = UITheme.FontBody;
        search.ForeColor = UITheme.Ink;
        statusFilter.Font = platformFilter.Font = UITheme.FontLabel;
        foreach (ComboBox filter in new[] { creatorFilter, priorityFilter, deadlineModeFilter, tagFilter, sortFilter })
            filter.Font = UITheme.FontLabel;
        countLabel.Font = UITheme.FontLabelBold;
        foreach (Label header in new[] { headerContent, headerStatus, headerPlatform, headerAssignee, headerAction })
            header.Font = UITheme.FontLabel;
        stateLabel.Font = UITheme.FontBody;
        stateLabel.ForeColor = UITheme.Neutral600;
        summaryLabel.Font = UITheme.FontLabel;
        brandLabel.Font = UITheme.FontLabel;
    }

    private void Filter_Changed(object? sender, EventArgs e)
    {
        if (updatingFilters) return;
        serverFilteredContents = null;
        RenderRows();
        queryDebounceTimer.Stop();
        queryDebounceTimer.Start();
    }

    private void Filter_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox filter || e.Index < 0 || e.Index >= filter.Items.Count) return;
        e.DrawBackground();
        TextRenderer.DrawText(e.Graphics, filter.Items[e.Index]?.ToString(), filter.Font,
            Rectangle.Inflate(e.Bounds, -7, 0), filter.ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        e.DrawFocusRectangle();
    }

    public void SetLoading()
    {
        loading = true;
        loadError = null;
        contents = [];
        serverFilteredContents = null;
        RenderRows();
    }

    public void SetLoadError(string message)
    {
        loading = false;
        loadError = string.IsNullOrWhiteSpace(message) ? "Không tải được Content của Project." : message;
        contents = [];
        serverFilteredContents = null;
        RenderRows();
    }

    public void SetContents(IEnumerable<ContentBoardCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        loading = false;
        loadError = null;
        contents = cards
            .Where(card => card is not null)
            .OrderByDescending(card => card.ContentId)
            .ToList();
        serverFilteredContents = null;
        PopulateFilterOptions();
        RenderRows();
    }

    /// <summary>Updates the server-filtered rows and keeps options from the full Project response.</summary>
    public void SetFilteredContents(IEnumerable<ContentBoardCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        serverFilteredContents = cards.ToList();
        loading = false;
        loadError = null;
        RenderRows();
    }

    private void PopulateFilterOptions()
    {
        updatingFilters = true;
        try
        {
            Populate(statusFilter, new object[] { new TextFilterOption(null, AllStatuses) }
                .Concat(contents.Select(card => card.Status).Distinct()
                    .OrderBy(StatusLabel, StringComparer.CurrentCultureIgnoreCase)
                    .Select(status => (object)new TextFilterOption(status.ToString(), StatusLabel(status)))));
            Populate(platformFilter, new object[] { new TextFilterOption(null, AllPlatforms) }
                .Concat(contents.SelectMany(card => card.Platforms)
                    .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .Select(value => (object)new TextFilterOption(value, value))));
            Populate(priorityFilter, new object[] { new TextFilterOption(null, "Mọi mức ưu tiên") }
                .Concat(contents.Select(card => card.Priority).Distinct().OrderBy(value => value)
                    .Select(value => (object)new TextFilterOption(value.ToString(), PriorityLabel(value)))));
            Populate(creatorFilter, new object[] { new CreatorFilterOption(null, "Mọi Creator") }
                .Concat(contents.SelectMany(card => card.Assignees)
                    .Where(creator => creator.UserId > 0 && !string.IsNullOrWhiteSpace(creator.Name))
                    .GroupBy(creator => creator.UserId).Select(group => group.First())
                    .OrderBy(creator => creator.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(creator => (object)new CreatorFilterOption(creator.UserId, creator.Name))));
            Populate(tagFilter, new object[] { new TextFilterOption(null, "Mọi tag") }
                .Concat(contents.SelectMany(card => card.Tags)
                    .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .Select(value => (object)new TextFilterOption(value, value))));
            SetSelectedIndexZero(statusFilter, platformFilter, creatorFilter, priorityFilter, deadlineModeFilter, tagFilter, sortFilter);
        }
        finally { updatingFilters = false; }
    }

    private static void Populate(ComboBox combo, IEnumerable<object> values)
    {
        combo.BeginUpdate();
        combo.Items.Clear();
        foreach (object value in values) combo.Items.Add(value);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        combo.EndUpdate();
    }

    private static void SetSelectedIndexZero(params ComboBox[] combos)
    {
        foreach (ComboBox combo in combos)
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private void RenderRows()
    {
        // Build the replacement list while hidden so rows appear together instead of streaming in.
        rows.Visible = false;
        rows.SuspendLayout();
        foreach (Control child in rows.Controls.Cast<Control>().ToArray())
        {
            rows.Controls.Remove(child);
            child.Dispose();
        }
        rows.RowStyles.Clear();
        rows.RowCount = 0;

        if (loading || loadError is not null)
        {
            ShowState(loading ? "Đang tải Content của Project…" : loadError!);
            countLabel.Text = "—";
            summaryLabel.Text = string.Empty;
            rows.ResumeLayout(true);
            return;
        }

        ContentLibraryQuery query = BuildQuery();
        IEnumerable<ContentBoardCard> filtered = serverFilteredContents ?? contents;
        if (!string.IsNullOrWhiteSpace(query.Title))
            filtered = filtered.Where(card => card.Code.Contains(query.Title, StringComparison.OrdinalIgnoreCase) ||
                                               card.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Status))
            filtered = filtered.Where(card => card.Status.ToString().Equals(query.Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Platform))
            filtered = filtered.Where(card => card.Platforms.Any(value => value.Equals(query.Platform, StringComparison.OrdinalIgnoreCase)));
        if (query.CreatorId.HasValue)
            filtered = filtered.Where(card => card.Assignees.Any(value => value.UserId == query.CreatorId.Value));
        if (!string.IsNullOrWhiteSpace(query.Priority))
            filtered = filtered.Where(card => card.Priority.ToString().Equals(query.Priority, StringComparison.OrdinalIgnoreCase));
        if (query.HasDeadline.HasValue)
            filtered = filtered.Where(card => card.Deadline.HasValue == query.HasDeadline.Value);
        if (query.DeadlineFrom.HasValue)
            filtered = filtered.Where(card => card.Deadline.HasValue && DateOnly.FromDateTime(card.Deadline.Value.Date) >= query.DeadlineFrom.Value);
        if (query.DeadlineTo.HasValue)
            filtered = filtered.Where(card => card.Deadline.HasValue && DateOnly.FromDateTime(card.Deadline.Value.Date) <= query.DeadlineTo.Value);
        if (!string.IsNullOrWhiteSpace(query.Tag))
            filtered = filtered.Where(card => card.Tags.Any(value => value.Equals(query.Tag, StringComparison.OrdinalIgnoreCase)));

        List<ContentBoardCard> visible = SortCards(filtered, query).ToList();
        countLabel.Text = $"{visible.Count} / {contents.Count}";
        summaryLabel.Text = visible.Count == contents.Count
            ? $"Hiển thị {visible.Count} mục dữ liệu"
            : $"Hiển thị {visible.Count} / {contents.Count} mục dữ liệu";
        if (visible.Count == 0)
        {
            ShowState(contents.Count == 0 ? "Project này chưa có Content." : "Không tìm thấy Content phù hợp.");
            rows.ResumeLayout(true);
            return;
        }

        stateLabel.Visible = false;
        rows.Visible = true;
        foreach (ContentBoardCard card in visible)
        {
            var row = new ContentLibraryRowControl(card);
            row.OpenRequested += contentId => OpenContentRequested?.Invoke(contentId);
            int rowIndex = rows.RowCount++;
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            rows.Controls.Add(row, 0, rowIndex);
        }
        rows.ResumeLayout(true);
        rows.Visible = true;
    }

    private ContentLibraryQuery BuildQuery()
    {
        var status = statusFilter.SelectedItem as TextFilterOption;
        var platform = platformFilter.SelectedItem as TextFilterOption;
        var creator = creatorFilter.SelectedItem as CreatorFilterOption;
        var priority = priorityFilter.SelectedItem as TextFilterOption;
        var deadlineMode = deadlineModeFilter.SelectedItem as DeadlineFilterOption;
        var tag = tagFilter.SelectedItem as TextFilterOption;
        var sort = sortFilter.SelectedItem as TextFilterOption;
        bool? hasDeadline = deadlineMode?.Value;
        return new ContentLibraryQuery
        {
            Title = string.IsNullOrWhiteSpace(search.Text) ? null : search.Text.Trim(),
            Platform = platform?.Value,
            Status = status?.Value,
            CreatorId = creator?.UserId,
            Priority = priority?.Value,
            HasDeadline = hasDeadline,
            DeadlineFrom = hasDeadline == false || !deadlineFromFilter.Checked ? null : DateOnly.FromDateTime(deadlineFromFilter.Value.Date),
            DeadlineTo = hasDeadline == false || !deadlineToFilter.Checked ? null : DateOnly.FromDateTime(deadlineToFilter.Value.Date),
            Tag = tag?.Value,
            SortBy = sort?.Value,
            SortDescending = sortDirection.Text.StartsWith("Giảm", StringComparison.Ordinal),
        };
    }

    private static IEnumerable<ContentBoardCard> SortCards(IEnumerable<ContentBoardCard> cards, ContentLibraryQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.SortBy)) return cards.OrderByDescending(card => card.ContentId);
        string sort = query.SortBy.Trim().ToLowerInvariant();
        if (sort == "deadline")
        {
            IOrderedEnumerable<ContentBoardCard> dated = cards.OrderBy(card => !card.Deadline.HasValue);
            dated = query.SortDescending == true ? dated.ThenByDescending(card => card.Deadline) : dated.ThenBy(card => card.Deadline);
            return dated.ThenBy(card => card.ContentId);
        }
        if (sort == "priority")
        {
            Func<ContentBoardCard, int> rank = card => card.Priority switch
            {
                Priority.Low => 0,
                Priority.Medium => 1,
                Priority.High => 2,
                Priority.Urgent => 3,
                _ => -1,
            };
            IOrderedEnumerable<ContentBoardCard> byPriority = query.SortDescending == true
                ? cards.OrderByDescending(rank)
                : cards.OrderBy(rank);
            return byPriority.ThenBy(card => card.ContentId);
        }

        Func<ContentBoardCard, string> key = sort switch
        {
            "title" => card => card.Title,
            "platform" => card => FirstSorted(card.Platforms),
            "status" => card => StatusLabel(card.Status),
            "creator" => card => FirstSorted(card.Assignees.Select(value => value.Name)),
            "tag" => card => FirstSorted(card.Tags),
            _ => card => card.Title,
        };
        IOrderedEnumerable<ContentBoardCard> ordered = query.SortDescending == true
            ? cards.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
            : cards.OrderBy(key, StringComparer.OrdinalIgnoreCase);
        return ordered.ThenBy(card => card.ContentId);
    }

    private static string FirstSorted(IEnumerable<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault() ?? string.Empty;

    private static string PriorityLabel(Priority priority) => priority switch
    {
        Priority.Urgent => "Khẩn cấp",
        Priority.High => "Cao",
        Priority.Medium => "Trung bình",
        Priority.Low => "Thấp",
        _ => priority.ToString(),
    };

    private sealed record TextFilterOption(string? Value, string Display)
    {
        public override string ToString() => Display;
    }

    private sealed record CreatorFilterOption(long? UserId, string Display)
    {
        public override string ToString() => Display;
    }

    private sealed record DeadlineFilterOption(bool? Value, string Display)
    {
        public override string ToString() => Display;
    }

    private void ShowState(string text)
    {
        rows.Visible = false;
        stateLabel.Text = text;
        stateLabel.ForeColor = loadError is null ? UITheme.Neutral600 : UITheme.Danger;
        stateLabel.Visible = true;
    }

    internal static void AddLibraryColumns(TableLayoutPanel table)
    {
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9));
    }

    private static string StatusLabel(ContentStatus status) => status switch
    {
        ContentStatus.Archived => "Đã lưu trữ",
        ContentStatus.Idea => "Ý tưởng",
        ContentStatus.Script => "Kịch bản",
        ContentStatus.Production => "Đang sản xuất",
        ContentStatus.Editing => "Đang chỉnh sửa",
        ContentStatus.Review => "Đang duyệt",
        ContentStatus.Ready => "Sẵn sàng",
        ContentStatus.Published => "Đã xuất bản",
        _ => status.ToString(),
    };

}

/// <summary>One Content row in the Project library.</summary>
public sealed class ContentLibraryRowControl : UserControl
{
    public ContentBoardCard Content { get; }
    public Button OpenButton { get; }
    public event Action<long>? OpenRequested;

    public ContentLibraryRowControl(ContentBoardCard content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Dock = DockStyle.Top;
        Height = 62;
        BackColor = UITheme.White;
        Margin = Padding.Empty;
        Padding = new Padding(19, 2, 19, 2);
        AccessibleName = $"{content.Code}: {content.Title}";

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            BackColor = UITheme.White,
            Margin = Padding.Empty,
        };
        ContentLibraryControl.AddLibraryColumns(layout);

        var identity = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.White,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(0, 4, 0, 4),
        };
        identity.Controls.Add(new Label
        {
            Name = "contentTitle",
            Text = content.Title,
            Dock = DockStyle.Top,
            Height = 26,
            Font = UITheme.FontBody,
            ForeColor = Color.FromArgb(25, 39, 62),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
        });
        identity.Controls.Add(new Label
        {
            Name = "contentCode",
            Text = content.Code,
            Dock = DockStyle.Bottom,
            Height = 18,
            Font = UITheme.FontLabel,
            ForeColor = Color.FromArgb(132, 150, 177),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
        });

        var status = new ContentStatusPillLabel(content.Status)
        {
            Name = "contentStatus",
            Text = DisplayStatus(content.Status),
            Dock = DockStyle.Fill,
            Font = UITheme.FontLabel,
            ForeColor = StatusTextColor(content.Status),
            BackColor = UITheme.White,
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = "Trạng thái: " + DisplayStatus(content.Status),
            Margin = Padding.Empty,
        };

        string[] platformNames = content.Platforms
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var platformCell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UITheme.White,
            Margin = new Padding(0, 0, 8, 0),
        };
        platformCell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, platformNames.Length == 0 ? 0 : 22));
        platformCell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        platformCell.Controls.Add(new PlatformMarkControl(platformNames.FirstOrDefault())
        {
            Dock = DockStyle.Fill,
            Visible = platformNames.Length > 0,
            Margin = Padding.Empty,
        }, 0, 0);
        platformCell.Controls.Add(new Label
        {
            Name = "contentPlatforms",
            Text = platformNames.Length == 0 ? "Chưa chọn" : string.Join(" · ", platformNames),
            Dock = DockStyle.Fill,
            Font = UITheme.FontBody,
            ForeColor = platformNames.Length == 0 ? Color.FromArgb(151, 165, 187) : Color.FromArgb(51, 69, 98),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(platformNames.Length == 0 ? 0 : 2, 0, 4, 0),
            AccessibleName = "Nền tảng: " + (platformNames.Length == 0 ? "Chưa chọn" : string.Join(", ", platformNames)),
        }, 1, 0);

        string[] assigneeNames = content.Assignees.Select(assignee => assignee.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (assigneeNames.Length == 0 && !string.IsNullOrWhiteSpace(content.AssigneeName))
            assigneeNames = [content.AssigneeName];
        var assigneeCell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UITheme.White,
            Margin = Padding.Empty,
        };
        assigneeCell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, assigneeNames.Length == 0 ? 0 : 27));
        assigneeCell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (assigneeNames.Length > 0)
            assigneeCell.Controls.Add(new AssigneeAvatarControl(assigneeNames[0])
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 7, 0),
            }, 0, 0);
        assigneeCell.Controls.Add(new Label
        {
            Name = "contentAssignees",
            Text = assigneeNames.Length == 0 ? "Chưa giao" : string.Join(", ", assigneeNames),
            Dock = DockStyle.Fill,
            Font = UITheme.FontBody,
            ForeColor = assigneeNames.Length == 0 ? Color.FromArgb(151, 165, 187) : Color.FromArgb(66, 84, 113),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
            AccessibleName = "Người được giao: " + (assigneeNames.Length == 0 ? "Chưa giao" : string.Join(", ", assigneeNames)),
        }, 1, 0);

        OpenButton = new ProjectActionButton
        {
            Name = "openContentDetail",
            Text = "Mở chi tiết",
            Style = RoundButtonStyle.Ghost,
            BorderRadius = 8,
            Size = new Size(88, 30),
            Anchor = AnchorStyles.Right,
            Font = UITheme.FontLabel,
            Margin = Padding.Empty,
            AccessibleName = $"Mở chi tiết {content.Code}: {content.Title}",
        };
        OpenButton.Click += (_, _) => OpenRequested?.Invoke(content.ContentId);

        layout.Controls.Add(identity, 0, 0);
        layout.Controls.Add(status, 1, 0);
        layout.Controls.Add(platformCell, 2, 0);
        layout.Controls.Add(assigneeCell, 3, 0);
        layout.Controls.Add(OpenButton, 4, 0);
        Controls.Add(layout);
        Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(239, 243, 248));
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        };
    }

    private static string DisplayStatus(ContentStatus status) => status switch
    {
        ContentStatus.Archived => "Đã lưu trữ",
        ContentStatus.Idea => "Ý tưởng",
        ContentStatus.Script => "Kịch bản",
        ContentStatus.Production => "Đang sản xuất",
        ContentStatus.Editing => "Đang chỉnh sửa",
        ContentStatus.Review => "Đang duyệt",
        ContentStatus.Ready => "Sẵn sàng",
        ContentStatus.Published => "Đã xuất bản",
        _ => status.ToString(),
    };

    private static Color StatusTextColor(ContentStatus status) => status switch
    {
        ContentStatus.Archived => Color.FromArgb(91, 111, 143),
        ContentStatus.Idea => Color.FromArgb(91, 111, 143),
        ContentStatus.Script => Color.FromArgb(39, 97, 177),
        ContentStatus.Production => Color.FromArgb(162, 105, 22),
        ContentStatus.Editing => Color.FromArgb(109, 40, 217),
        ContentStatus.Review or ContentStatus.Ready => Color.FromArgb(5, 132, 95),
        ContentStatus.Published => Color.FromArgb(5, 132, 95),
        _ => Color.FromArgb(91, 111, 143),
    };

    private static Color StatusBackColor(ContentStatus status) => status switch
    {
        ContentStatus.Archived or ContentStatus.Idea => Color.FromArgb(241, 245, 249),
        ContentStatus.Script => Color.FromArgb(235, 244, 255),
        ContentStatus.Production => Color.FromArgb(255, 247, 224),
        ContentStatus.Editing => Color.FromArgb(246, 239, 255),
        ContentStatus.Review or ContentStatus.Ready or ContentStatus.Published => Color.FromArgb(232, 251, 245),
        _ => Color.FromArgb(241, 245, 249),
    };

    private static Color StatusDotColor(ContentStatus status) => status switch
    {
        ContentStatus.Archived or ContentStatus.Idea => Color.FromArgb(148, 163, 184),
        ContentStatus.Script => Color.FromArgb(59, 130, 246),
        ContentStatus.Production => Color.FromArgb(245, 158, 11),
        ContentStatus.Editing => Color.FromArgb(139, 92, 246),
        ContentStatus.Review or ContentStatus.Ready or ContentStatus.Published => Color.FromArgb(16, 185, 129),
        _ => Color.FromArgb(148, 163, 184),
    };

    private sealed class ContentStatusPillLabel : Label
    {
        private readonly ContentStatus status;

        public ContentStatusPillLabel(ContentStatus status)
        {
            this.status = status;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? UITheme.White);

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int width = Math.Min(Width - 4, TextRenderer.MeasureText(Text, Font).Width + 30);
            if (width <= 0 || Height <= 0) return;
            var bounds = new Rectangle(1, Math.Max(2, (Height - 24) / 2), width, Math.Min(24, Height - 4));
            using var path = UITheme.CreateRoundedRectanglePath(bounds, 12);
            using var background = new SolidBrush(StatusBackColor(status));
            e.Graphics.FillPath(background, path);
            using var dot = new SolidBrush(StatusDotColor(status));
            e.Graphics.FillEllipse(dot, bounds.X + 8, bounds.Y + (bounds.Height - 6) / 2, 6, 6);
            var textBounds = new Rectangle(bounds.X + 19, bounds.Y, Math.Max(0, bounds.Width - 21), bounds.Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, StatusTextColor(status),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}

internal sealed class RoundedSurfacePanel : Panel
{
    public int Radius { get; set; } = 10;
    public Color BorderColor { get; set; } = Color.FromArgb(229, 235, 244);

    public RoundedSurfacePanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 3 || Height < 3) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UITheme.CreateRoundedRectanglePath(bounds, Radius);
        using var pen = new Pen(BorderColor, 1f);
        e.Graphics.DrawPath(pen, path);
    }
}

internal sealed class LibraryFramePanel : Panel
{
    public LibraryFramePanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 3 || Height < 3) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        using (var path = UITheme.CreateRoundedRectanglePath(bounds, 13))
        using (var outline = new Pen(Color.FromArgb(224, 232, 244), 1f))
            e.Graphics.DrawPath(outline, path);
        using var accent = new Pen(Color.FromArgb(105, 91, 255), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLine(accent, 2, 18, 2, Height - 18);
        e.Graphics.DrawLine(accent, 18, Height - 2, Width - 18, Height - 2);
    }
}

internal sealed class WorkspaceBadgeLabel : Label
{
    public WorkspaceBadgeLabel() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? UITheme.White);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 3 || Height < 3) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UITheme.CreateRoundedRectanglePath(bounds, Height / 2);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

internal sealed class SearchGlyphControl : Control
{
    public SearchGlyphControl() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(147, 163, 187), 1.4f);
        e.Graphics.DrawEllipse(pen, 4, Math.Max(5, (Height - 13) / 2), 9, 9);
        e.Graphics.DrawLine(pen, 12, Math.Max(13, (Height + 2) / 2), 16, Math.Max(17, (Height + 6) / 2));
    }
}

internal sealed class PlatformMarkControl : Control
{
    private static readonly Font FacebookFont = new("Segoe UI", 9f, FontStyle.Bold);
    private static readonly Font TikTokFont = new("Segoe UI", 8f, FontStyle.Bold);
    private readonly string? platform;

    public PlatformMarkControl(string? platform)
    {
        this.platform = platform;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(platform)) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int centerY = Height / 2;
        if (platform.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
        {
            var badge = new Rectangle(2, centerY - 6, 15, 12);
            using var path = UITheme.CreateRoundedRectanglePath(badge, 4);
            using var red = new SolidBrush(Color.FromArgb(239, 68, 68));
            e.Graphics.FillPath(red, path);
            using var triangle = new SolidBrush(Color.White);
            e.Graphics.FillPolygon(triangle, [new Point(8, centerY - 3), new Point(8, centerY + 3), new Point(12, centerY)]);
        }
        else if (platform.Contains("Facebook", StringComparison.OrdinalIgnoreCase))
        {
            using var blue = new SolidBrush(Color.FromArgb(59, 130, 246));
            e.Graphics.FillEllipse(blue, 2, centerY - 7, 14, 14);
            TextRenderer.DrawText(e.Graphics, "f", FacebookFont, new Rectangle(2, centerY - 8, 14, 16), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        else if (platform.Contains("TikTok", StringComparison.OrdinalIgnoreCase))
        {
            var badge = new Rectangle(2, centerY - 7, 14, 14);
            using var path = UITheme.CreateRoundedRectanglePath(badge, 4);
            using var dark = new SolidBrush(Color.FromArgb(23, 32, 51));
            e.Graphics.FillPath(dark, path);
            TextRenderer.DrawText(e.Graphics, "♪", TikTokFont, badge, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        else
        {
            using var dot = new SolidBrush(Color.FromArgb(122, 139, 167));
            e.Graphics.FillEllipse(dot, 6, centerY - 4, 8, 8);
        }
    }
}

internal sealed class AssigneeAvatarControl : Control
{
    private readonly string initials;
    private static readonly Font InitialsFont = new("Segoe UI", 6.5f, FontStyle.Bold);

    public AssigneeAvatarControl(string name)
    {
        string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        initials = words.Length switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
            _ => string.Concat(words[0][0], words[^1][0]).ToUpperInvariant(),
        };
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int diameter = Math.Max(1, Math.Min(Width, Height) - 1);
        var bounds = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
        using var fill = new SolidBrush(Color.FromArgb(239, 233, 255));
        e.Graphics.FillEllipse(fill, bounds);
        TextRenderer.DrawText(e.Graphics, initials, InitialsFont, bounds,
            Color.FromArgb(109, 40, 217), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
