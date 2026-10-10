using System.Drawing.Drawing2D;

namespace CreatorFlow.Theme;

public enum ChevronDir { Left, Right, Up, Down }

/// <summary>
/// Bộ icon nét mảnh (stroke icon) vẽ bằng GDI+ để thay cho các ký tự Unicode tạm
/// ("O", "o", "v", "▦"...). Mọi hàm nhận Rectangle vùng vẽ (thường 16–24px).
/// </summary>
public static class UIIcons
{
    private static Pen NewPen(Color c, float w) => new(c, w)
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
        LineJoin = LineJoin.Round,
    };

    private static PointF Pt(Rectangle r, float fx, float fy) => new(r.X + r.Width * fx, r.Y + r.Height * fy);

    public static void Search(Graphics g, Rectangle r, Color c, float w = 1.8f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, w);
        float d = r.Width * 0.64f;
        g.DrawEllipse(pen, r.X + 1, r.Y + 1, d, d);
        var a = new PointF(r.X + 1 + d * 0.86f, r.Y + 1 + d * 0.86f);
        g.DrawLine(pen, a, new PointF(r.Right - 1.5f, r.Bottom - 1.5f));
    }

    public static void Plus(Graphics g, Rectangle r, Color c, float w = 2f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, w);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float s = Math.Min(r.Width, r.Height) * 0.38f;
        g.DrawLine(pen, cx - s, cy, cx + s, cy);
        g.DrawLine(pen, cx, cy - s, cx, cy + s);
    }

    public static void Close(Graphics g, Rectangle r, Color c, float w = 1.8f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, w);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float s = Math.Min(r.Width, r.Height) * 0.30f;
        g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
        g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
    }

    public static void Chevron(Graphics g, Rectangle r, ChevronDir dir, Color c, float w = 1.8f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, w);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float s = Math.Min(r.Width, r.Height) * 0.22f;
        PointF[] pts = dir switch
        {
            ChevronDir.Right => new[] { new PointF(cx - s * 0.6f, cy - s), new PointF(cx + s * 0.6f, cy), new PointF(cx - s * 0.6f, cy + s) },
            ChevronDir.Left => new[] { new PointF(cx + s * 0.6f, cy - s), new PointF(cx - s * 0.6f, cy), new PointF(cx + s * 0.6f, cy + s) },
            ChevronDir.Up => new[] { new PointF(cx - s, cy + s * 0.6f), new PointF(cx, cy - s * 0.6f), new PointF(cx + s, cy + s * 0.6f) },
            _ => new[] { new PointF(cx - s, cy - s * 0.6f), new PointF(cx, cy + s * 0.6f), new PointF(cx + s, cy - s * 0.6f) },
        };
        g.DrawLines(pen, pts);
    }

    public static void Bell(Graphics g, Rectangle r, Color c, float w = 1.8f)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, w);
        using var path = new GraphicsPath();
        path.AddArc(r.X + r.Width * 0.22f, r.Y + r.Height * 0.10f, r.Width * 0.56f, r.Height * 0.56f, 180, 180);
        path.AddLines(new[]
        {
            Pt(r, 0.78f, 0.62f), Pt(r, 0.90f, 0.76f), Pt(r, 0.10f, 0.76f), Pt(r, 0.22f, 0.62f), Pt(r, 0.22f, 0.38f),
        });
        g.DrawPath(pen, path);
        g.DrawArc(pen, r.X + r.Width * 0.5f - 3f, r.Y + r.Height * 0.80f, 6f, 5f, 0, 180);
    }

    // ---------- Sidebar icons ----------

    private static Rectangle Inset(Rectangle r, int m) => new(r.X + m, r.Y + m, r.Width - 2 * m, r.Height - 2 * m);

    /// <summary>Summary: khung cửa sổ chia ô.</summary>
    public static void Summary(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        var b = Inset(r, 2);
        using (var path = UITheme.CreateRoundedRectanglePath(b, 3)) g.DrawPath(pen, path);
        g.DrawLine(pen, Pt(b, 0.42f, 0f), Pt(b, 0.42f, 1f));
        g.DrawLine(pen, Pt(b, 0.42f, 0.5f), Pt(b, 1f, 0.5f));
    }

    /// <summary>Board: khung + 2 cột kanban.</summary>
    public static void Board(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        var b = Inset(r, 2);
        using (var path = UITheme.CreateRoundedRectanglePath(b, 3)) g.DrawPath(pen, path);
        using var thick = NewPen(c, 2.6f);
        g.DrawLine(thick, Pt(b, 0.32f, 0.24f), Pt(b, 0.32f, 0.72f));
        g.DrawLine(thick, Pt(b, 0.68f, 0.24f), Pt(b, 0.68f, 0.50f));
    }

    /// <summary>Backlog: danh sách 3 dòng có chấm.</summary>
    public static void List(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        using var brush = new SolidBrush(c);
        for (int i = 0; i < 3; i++)
        {
            float y = r.Y + r.Height * (0.22f + i * 0.28f);
            g.FillEllipse(brush, r.X + r.Width * 0.06f, y - 1.6f, 3.2f, 3.2f);
            g.DrawLine(pen, r.X + r.Width * 0.30f, y, r.Right - r.Width * 0.06f, y);
        }
    }

    public static void Calendar(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        using var brush = new SolidBrush(c);
        var b = new Rectangle(r.X + 2, r.Y + 4, r.Width - 4, r.Height - 6);
        using (var path = UITheme.CreateRoundedRectanglePath(b, 3)) g.DrawPath(pen, path);
        g.DrawLine(pen, Pt(b, 0f, 0.28f), Pt(b, 1f, 0.28f));
        g.DrawLine(pen, r.X + r.Width * 0.32f, r.Y + 1.5f, r.X + r.Width * 0.32f, r.Y + 6);
        g.DrawLine(pen, r.X + r.Width * 0.68f, r.Y + 1.5f, r.X + r.Width * 0.68f, r.Y + 6);
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++)
                g.FillEllipse(brush, b.X + b.Width * (0.22f + col * 0.28f) - 1.3f, b.Y + b.Height * (0.52f + row * 0.26f) - 1.3f, 2.6f, 2.6f);
    }

    /// <summary>Reports: 3 cột biểu đồ.</summary>
    public static void Chart(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(c);
        float bw = r.Width * 0.22f;
        float baseY = r.Bottom - 2;
        float[] hs = { 0.45f, 0.95f, 0.65f };
        for (int i = 0; i < 3; i++)
        {
            float x = r.X + r.Width * (0.08f + i * 0.31f);
            float h = (r.Height - 4) * hs[i];
            g.FillRectangle(brush, x, baseY - h, bw, h);
        }
    }

    /// <summary>My Work: vòng tròn có dấu tick.</summary>
    public static void CheckCircle(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        var b = Inset(r, 2);
        g.DrawEllipse(pen, b);
        g.DrawLines(pen, new[] { Pt(b, 0.28f, 0.52f), Pt(b, 0.45f, 0.68f), Pt(b, 0.74f, 0.34f) });
    }

    /// <summary>Review Queue: khung tin nhắn có nét bút.</summary>
    public static void ReviewQueue(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        var b = new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 7);
        using (var path = UITheme.CreateRoundedRectanglePath(b, 3)) g.DrawPath(pen, path);
        g.DrawLines(pen, new[] { Pt(r, 0.22f, 0.72f), Pt(r, 0.18f, 0.92f), Pt(r, 0.40f, 0.76f) });
        g.DrawLine(pen, Pt(b, 0.30f, 0.66f), Pt(b, 0.66f, 0.28f));
    }

    /// <summary>AI Assistant: ngôi sao 4 cánh.</summary>
    public static void Sparkle(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(c);
        float cx = r.X + r.Width * 0.45f, cy = r.Y + r.Height * 0.55f;
        float R = r.Width * 0.44f;
        var pts = new PointF[8];
        for (int i = 0; i < 8; i++)
        {
            double a = (-90 + i * 45) * Math.PI / 180.0;
            float rad = (i % 2 == 0) ? R : R * 0.30f;
            pts[i] = new PointF(cx + (float)(Math.Cos(a) * rad), cy + (float)(Math.Sin(a) * rad));
        }
        g.FillPolygon(brush, pts);
        // Ngôi sao nhỏ phụ góc trên phải
        float sx = r.X + r.Width * 0.82f, sy = r.Y + r.Height * 0.18f, sr = r.Width * 0.16f;
        var small = new PointF[8];
        for (int i = 0; i < 8; i++)
        {
            double a = (-90 + i * 45) * Math.PI / 180.0;
            float rad = (i % 2 == 0) ? sr : sr * 0.30f;
            small[i] = new PointF(sx + (float)(Math.Cos(a) * rad), sy + (float)(Math.Sin(a) * rad));
        }
        g.FillPolygon(brush, small);
    }

    public static void Gear(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        using var thick = NewPen(c, 3f);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float m = Math.Min(r.Width, r.Height);
        float ring = m * 0.30f, inner = m * 0.13f;
        g.DrawEllipse(pen, cx - ring, cy - ring, ring * 2, ring * 2);
        g.DrawEllipse(pen, cx - inner, cy - inner, inner * 2, inner * 2);
        for (int k = 0; k < 8; k++)
        {
            double a = k * Math.PI / 4;
            float x1 = cx + (float)Math.Cos(a) * (ring + 0.5f), y1 = cy + (float)Math.Sin(a) * (ring + 0.5f);
            float x2 = cx + (float)Math.Cos(a) * (m * 0.44f), y2 = cy + (float)Math.Sin(a) * (m * 0.44f);
            g.DrawLine(thick, x1, y1, x2, y2);
        }
    }

    /// <summary>Shield: khiên bảo vệ cho mục quản trị hệ thống.</summary>
    public static void Shield(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.8f);
        var b = Inset(r, 2);
        using var path = new GraphicsPath();
        path.AddLines(new[]
        {
            Pt(b, 0.50f, 0.02f), Pt(b, 0.88f, 0.20f), Pt(b, 0.88f, 0.52f),
            Pt(b, 0.50f, 0.98f), Pt(b, 0.12f, 0.52f), Pt(b, 0.12f, 0.20f),
        });
        path.CloseFigure();
        g.DrawPath(pen, path);
    }

    /// <summary>Logo: khung bo góc có tam giác play.</summary>
    public static void LogoMark(Graphics g, Rectangle r, Color bg, Color fg)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var brush = new SolidBrush(bg))
        using (var path = UITheme.CreateRoundedRectanglePath(r, 5))
            g.FillPath(brush, path);
        using var fgBrush = new SolidBrush(fg);
        g.FillPolygon(fgBrush, new[] { Pt(r, 0.36f, 0.26f), Pt(r, 0.74f, 0.5f), Pt(r, 0.36f, 0.74f) });
    }

    public static void Trash(Graphics g, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.7f);
        g.DrawLine(pen, Pt(r, 0.15f, 0.25f), Pt(r, 0.85f, 0.25f));
        g.DrawLine(pen, Pt(r, 0.38f, 0.25f), Pt(r, 0.38f, 0.12f));
        g.DrawLine(pen, Pt(r, 0.38f, 0.12f), Pt(r, 0.62f, 0.12f));
        g.DrawLine(pen, Pt(r, 0.62f, 0.12f), Pt(r, 0.62f, 0.25f));
        g.DrawLines(pen, new[] { Pt(r, 0.22f, 0.25f), Pt(r, 0.27f, 0.88f), Pt(r, 0.73f, 0.88f), Pt(r, 0.78f, 0.25f) });
    }

    // ---------- Platform glyphs ----------

    public static void PlatformGlyph(Graphics g, string platform, Rectangle r, Color c)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = NewPen(c, 1.7f);
        using var brush = new SolidBrush(c);
        switch (platform.Trim().ToLowerInvariant())
        {
            case "youtube":
                g.DrawEllipse(pen, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                g.FillPolygon(brush, new[] { Pt(r, 0.40f, 0.30f), Pt(r, 0.72f, 0.50f), Pt(r, 0.40f, 0.70f) });
                break;

            case "tiktok":
                g.FillEllipse(brush, r.X + r.Width * 0.10f, r.Y + r.Height * 0.60f, r.Width * 0.38f, r.Height * 0.30f);
                g.DrawLine(pen, Pt(r, 0.46f, 0.74f), Pt(r, 0.46f, 0.10f));
                g.DrawBezier(pen, Pt(r, 0.46f, 0.10f), Pt(r, 0.62f, 0.34f), Pt(r, 0.90f, 0.24f), Pt(r, 0.84f, 0.50f));
                break;

            case "instagram":
                var box = Inset(r, 1);
                using (var path = UITheme.CreateRoundedRectanglePath(box, 5)) g.DrawPath(pen, path);
                g.DrawEllipse(pen, r.X + r.Width * 0.30f, r.Y + r.Height * 0.30f, r.Width * 0.40f, r.Height * 0.40f);
                g.FillEllipse(brush, r.X + r.Width * 0.70f, r.Y + r.Height * 0.20f, 2.4f, 2.4f);
                break;

            case "facebook":
                g.DrawEllipse(pen, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                using (var f = new Font("Segoe UI", r.Height * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("f", f, brush, new RectangleF(r.X + 1, r.Y + 2, r.Width, r.Height), sf);
                break;

            default:
                g.DrawEllipse(pen, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                break;
        }
    }

    /// <summary>Vẽ pill nền tảng: [glyph] Tên. Trả về rộng của pill để căn phải/trái.</summary>
    public static int MeasurePlatformPill(Graphics g, string platform, int height)
    {
        var size = TextRenderer.MeasureText(g, platform, UITheme.FontLabelBold, new Size(int.MaxValue, height), TextFormatFlags.NoPadding);
        return 10 + 16 + 6 + size.Width + 14;
    }

    public static void DrawPlatformPill(Graphics g, string platform, Rectangle rect)
    {
        var (bg, fg, _) = UITheme.GetPlatformStyle(platform);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var brush = new SolidBrush(bg))
        using (var path = UITheme.CreateRoundedRectanglePath(rect, rect.Height / 2))
            g.FillPath(brush, path);

        var glyph = new Rectangle(rect.X + 10, rect.Y + (rect.Height - 16) / 2, 16, 16);
        PlatformGlyph(g, platform, glyph, fg);

        TextRenderer.DrawText(g, platform, UITheme.FontLabelBold,
            new Rectangle(glyph.Right + 6, rect.Y, rect.Width - (glyph.Right - rect.X) - 6, rect.Height), fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}
