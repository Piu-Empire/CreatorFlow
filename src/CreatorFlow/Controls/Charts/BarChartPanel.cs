using System.Drawing.Drawing2D;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls.Charts;

/// <summary>Một thanh trong biểu đồ: giá trị dùng để tính độ dài, ValueText/Hint dùng để hiển thị.</summary>
public sealed record ChartBar(string Label, long Value, string ValueText, string? Hint, Color Color);

/// <summary>Một nhóm thanh có tiêu đề phụ (rỗng = không vẽ tiêu đề).</summary>
public sealed record ChartSection(string Subtitle, IReadOnlyList<ChartBar> Bars);

/// <summary>
/// SCRUM-54: control vẽ biểu đồ thanh ngang bằng GDI+, không phụ thuộc thư viện ngoài.
/// Thang đo mỗi nhóm thanh lấy từ giá trị lớn nhất thực tế của nhóm (qua ChartDataBuilder.NiceCeiling).
/// </summary>
public sealed class BarChartPanel : UserControl
{
    private const int Edge = 16;

    private string _title = string.Empty;
    private string _caption = string.Empty;
    private IReadOnlyList<ChartSection> _sections = Array.Empty<ChartSection>();
    private string? _emptyMessage;

    public BarChartPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = UITheme.White;
        DoubleBuffered = true;
        Margin = Padding.Empty;
    }

    public void SetData(string title, string caption, IReadOnlyList<ChartSection> sections)
    {
        _title = title ?? string.Empty;
        _caption = caption ?? string.Empty;
        _sections = sections ?? Array.Empty<ChartSection>();
        _emptyMessage = null;
        Invalidate();
    }

    public void SetBars(string title, string caption, IReadOnlyList<ChartBar> bars) =>
        SetData(title, caption, new[] { new ChartSection(string.Empty, bars) });

    public void SetEmpty(string title, string message)
    {
        _title = title ?? string.Empty;
        _caption = string.Empty;
        _sections = Array.Empty<ChartSection>();
        _emptyMessage = message;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int width = ClientSize.Width;
        int height = ClientSize.Height;
        if (width <= 4 || height <= 4) return;

        using (var border = new Pen(UITheme.Neutral200))
            g.DrawRectangle(border, 0, 0, width - 1, height - 1);

        int left = Edge;
        int textWidth = Math.Max(20, width - Edge * 2);
        int y = Edge;

        TextRenderer.DrawText(g, _title, UITheme.FontBodyBold,
            new Rectangle(left, y, textWidth, 22), UITheme.Neutral900,
            TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        y += 24;

        if (!string.IsNullOrEmpty(_caption))
        {
            Size captionSize = TextRenderer.MeasureText(g, _caption, UITheme.FontLabel,
                new Size(textWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, _caption, UITheme.FontLabel,
                new Rectangle(left, y, textWidth, captionSize.Height), UITheme.Neutral600,
                TextFormatFlags.Left | TextFormatFlags.WordBreak);
            y += captionSize.Height + 6;
        }

        int bodyHeight = Math.Max(0, height - y - Edge);

        if (_emptyMessage is not null)
        {
            DrawCenteredMessage(g, _emptyMessage, left, y, textWidth, bodyHeight);
            return;
        }

        int barRows = _sections.Sum(section => Math.Max(1, section.Bars.Count));
        int subtitleRows = _sections.Count(section => !string.IsNullOrWhiteSpace(section.Subtitle));
        if (barRows == 0 || bodyHeight <= 0)
        {
            DrawCenteredMessage(g, "Chưa có dữ liệu", left, y, textWidth, bodyHeight);
            return;
        }

        const int subtitleHeight = 18;
        int rowHeight = Math.Clamp((bodyHeight - subtitleRows * subtitleHeight) / barRows, 12, 32);

        var brushes = new Dictionary<Color, SolidBrush>();
        try
        {
            foreach (ChartSection section in _sections)
            {
                if (!string.IsNullOrWhiteSpace(section.Subtitle))
                {
                    TextRenderer.DrawText(g, section.Subtitle, UITheme.FontLabelBold,
                        new Rectangle(left, y, textWidth, subtitleHeight), UITheme.Neutral700,
                        TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    y += subtitleHeight;
                }

                long max = section.Bars.Count == 0 ? 0 : section.Bars.Max(bar => bar.Value);
                long ceiling = ChartDataBuilder.NiceCeiling(max);

                foreach (ChartBar bar in section.Bars)
                {
                    DrawBar(g, brushes, bar, left, y, textWidth, rowHeight, ceiling);
                    y += rowHeight;
                }
            }
        }
        finally
        {
            foreach (SolidBrush brush in brushes.Values) brush.Dispose();
        }
    }

    private static void DrawCenteredMessage(Graphics g, string message,
        int left, int top, int width, int height)
    {
        if (height < 10) return;

        TextRenderer.DrawText(g, message, UITheme.FontBody,
            new Rectangle(left, top, width, height), UITheme.Neutral600,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    private static void DrawBar(Graphics g, Dictionary<Color, SolidBrush> brushes, ChartBar bar,
        int left, int top, int width, int height, long ceiling)
    {
        if (height < 8) return;

        int labelWidth = Math.Clamp((int)(width * 0.30), 70, 240);
        labelWidth = Math.Min(labelWidth, Math.Max(0, width - 120));
        int valueWidth = Math.Min(120, Math.Max(40, width / 3));
        int barLeft = left + labelWidth + 8;
        int barWidth = width - labelWidth - 8 - valueWidth - 4;
        if (barWidth < 12) return;

        int textHeight = Math.Min(height, 18);
        int textTop = top + (height - textHeight) / 2;

        TextRenderer.DrawText(g, bar.Label, UITheme.FontLabel,
            new Rectangle(left, textTop, labelWidth, textHeight), UITheme.Neutral700,
            TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

        int trackHeight = Math.Max(6, (int)(height * 0.55));
        int trackTop = top + (height - trackHeight) / 2;
        using (var track = new SolidBrush(UITheme.Neutral100))
            g.FillRectangle(track, barLeft, trackTop, barWidth, trackHeight);

        long value = Math.Max(0, bar.Value);
        double ratio = ceiling <= 0 ? 0 : Math.Min(1d, value / (double)ceiling);
        int filled = (int)Math.Round(barWidth * ratio);
        if (value > 0 && filled < 2) filled = 2;
        if (filled > 0)
        {
            if (!brushes.TryGetValue(bar.Color, out SolidBrush? brush))
            {
                brush = new SolidBrush(bar.Color);
                brushes[bar.Color] = brush;
            }

            g.FillRectangle(brush, barLeft, trackTop, filled, trackHeight);
        }

        string valueText = string.IsNullOrEmpty(bar.Hint)
            ? bar.ValueText
            : $"{bar.ValueText} · {bar.Hint}";
        TextRenderer.DrawText(g, valueText, UITheme.FontLabel,
            new Rectangle(barLeft + barWidth + 6, textTop, valueWidth, textHeight), UITheme.Neutral800,
            TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
    }
}
