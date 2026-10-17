using System.Drawing.Drawing2D;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls.ContentLibrary;

/// <summary>Project-wide Content library with real status, platform, assignee and detail navigation.</summary>
public sealed partial class ContentLibraryControl : UserControl
{
    private const string AllStatuses = "Tất cả trạng thái";
    private const string AllPlatforms = "Tất cả nền tảng";

    private List<ContentBoardCard> contents = [];
    private string? loadError;
    private bool loading = true;

    public event Action<long>? OpenContentRequested;

    public int TotalContentCount => contents.Count;

    public ContentLibraryControl(string projectName)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        InitializeComponent();
        Dock = DockStyle.Fill;
        Font = UITheme.FontBody;
        projectNameLabel.Text = projectName;
        ApplyTheme();

        statusFilter.Items.Add(AllStatuses);
        foreach (ContentStatus status in Enum.GetValues<ContentStatus>())
            statusFilter.Items.Add(StatusLabel(status));
        statusFilter.SelectedIndex = 0;
        SetLoading();
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
        countLabel.Font = UITheme.FontLabelBold;
        foreach (Label header in new[] { headerContent, headerStatus, headerPlatform, headerAssignee, headerAction })
            header.Font = UITheme.FontLabel;
        stateLabel.Font = UITheme.FontBody;
        stateLabel.ForeColor = UITheme.Neutral600;
        summaryLabel.Font = UITheme.FontLabel;
        brandLabel.Font = UITheme.FontLabel;
    }

    private void Filter_Changed(object? sender, EventArgs e) => RenderRows();

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
        RenderRows();
    }

    public void SetLoadError(string message)
    {
        loading = false;
        loadError = string.IsNullOrWhiteSpace(message) ? "Không tải được Content của Project." : message;
        contents = [];
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

        platformFilter.BeginUpdate();
        platformFilter.Items.Clear();
        platformFilter.Items.Add(AllPlatforms);
        foreach (string platform in contents.SelectMany(card => card.Platforms).Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            platformFilter.Items.Add(platform);
        platformFilter.SelectedIndex = 0;
        platformFilter.EndUpdate();
        RenderRows();
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

        string query = search.Text.Trim();
        string selectedStatus = statusFilter.SelectedItem?.ToString() ?? AllStatuses;
        string selectedPlatform = platformFilter.SelectedItem?.ToString() ?? AllPlatforms;
        IEnumerable<ContentBoardCard> filtered = contents;
        if (query.Length > 0)
            filtered = filtered.Where(card => card.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                               card.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (selectedStatus != AllStatuses)
            filtered = filtered.Where(card => StatusLabel(card.Status).Equals(selectedStatus, StringComparison.Ordinal));
        if (selectedPlatform != AllPlatforms)
            filtered = filtered.Where(card => card.Platforms.Any(platform => platform.Equals(selectedPlatform, StringComparison.OrdinalIgnoreCase)));

        List<ContentBoardCard> visible = filtered.ToList();
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
