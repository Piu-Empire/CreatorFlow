using System.Drawing.Drawing2D;
using CreatorFlow.Models;

namespace CreatorFlow.Theme;

/// <summary>
/// Hệ thống Design Tokens chính xác theo "CreatorFlow — UI Style Guide".
/// Bảng màu, typography Segoe UI / Consolas, khoảng cách 4px, bo góc 8px/12px,
/// và các tiện ích vẽ đồ hoạ phẳng hiện đại (GDI+ antialiased).
/// </summary>
public static class UITheme
{
    // ==========================================
    // 1. BẢNG MÀU (COLORS) — Cập nhật theo mẫu mới
    // ==========================================

    // Neutral
    public static readonly Color Ink = ColorTranslator.FromHtml("#151C27"); // Xanh đen chữ tiêu đề
    public static readonly Color Black = ColorTranslator.FromHtml("#000000"); // Đen tuyền: Sidebar, nút Primary
    public static readonly Color Neutral900 = ColorTranslator.FromHtml("#171717"); // Chữ đậm phụ, hover đen
    public static readonly Color Neutral800 = ColorTranslator.FromHtml("#262626"); // Chữ badge Med, avatar bg
    public static readonly Color Neutral700 = ColorTranslator.FromHtml("#404040");
    public static readonly Color Neutral600 = ColorTranslator.FromHtml("#525252"); // Chữ phụ, caption
    public static readonly Color Neutral400 = ColorTranslator.FromHtml("#A3A3A3"); // Placeholder, icon mờ
    public static readonly Color Neutral300 = ColorTranslator.FromHtml("#D4D4D4"); // Viền control mặc định
    public static readonly Color Neutral200 = ColorTranslator.FromHtml("#E5E5E5"); // Viền mặc định component, divider
    public static readonly Color Neutral100 = ColorTranslator.FromHtml("#F5F5F5"); // Nền nút Secondary, hover nhẹ
    public static readonly Color Neutral50 = ColorTranslator.FromHtml("#FAFAFA"); // Nền content area, dòng chẵn
    public static readonly Color White = ColorTranslator.FromHtml("#FFFFFF");

    // Production Board (theo mẫu AI Studio)
    public static readonly Color ColumnBody = ColorTranslator.FromHtml("#F4F5F7");   // Nền thân cột Kanban
    public static readonly Color SidebarItemActive = ColorTranslator.FromHtml("#262626");
    public static readonly Color SidebarItemHover = ColorTranslator.FromHtml("#171717");
    public static readonly Color SidebarBadge = ColorTranslator.FromHtml("#262626");

    // Sidebar text colors
    public static readonly Color SidebarText = ColorTranslator.FromHtml("#CFC4C5");
    public static readonly Color SidebarTextMuted = ColorTranslator.FromHtml("#8A8A8A");
    public static readonly Color SidebarTextFaint = ColorTranslator.FromHtml("#6B6B6B");

    // Stage colors (màu chấm theo mẫu mới)
    public static readonly Color StageIdea = ColorTranslator.FromHtml("#D4D4D4");
    public static readonly Color StageScript = ColorTranslator.FromHtml("#60A5FA");
    public static readonly Color StageProduction = ColorTranslator.FromHtml("#FBBF24");
    public static readonly Color StageEditing = ColorTranslator.FromHtml("#C084FC");
    public static readonly Color StageReview = ColorTranslator.FromHtml("#10B981");
    public static readonly Color StageReady = ColorTranslator.FromHtml("#10B981");
    public static readonly Color StagePublished = ColorTranslator.FromHtml("#000000");

    // Priority / Status
    public static readonly Color PriorityHighBg = ColorTranslator.FromHtml("#FEE2E2");
    public static readonly Color PriorityHighText = ColorTranslator.FromHtml("#B91C1C");
    public static readonly Color PriorityMedBg = ColorTranslator.FromHtml("#E5E5E5");
    public static readonly Color PriorityMedText = ColorTranslator.FromHtml("#262626");
    public static readonly Color PriorityLowBg = ColorTranslator.FromHtml("#F5F5F5");
    public static readonly Color PriorityLowText = ColorTranslator.FromHtml("#525252");

    // Action / State
    public static readonly Color Danger = ColorTranslator.FromHtml("#DC2626");
    public static readonly Color DangerBg = ColorTranslator.FromHtml("#FEE2E2");
    public static readonly Color Warning = ColorTranslator.FromHtml("#FBBF24");
    public static readonly Color Success = ColorTranslator.FromHtml("#10B981");
    public static readonly Color Info = ColorTranslator.FromHtml("#60A5FA");
    public static readonly Color AccentPurple = ColorTranslator.FromHtml("#C084FC");

    // Overdue
    public static readonly Color OverdueBg = ColorTranslator.FromHtml("#FFF1F2");
    public static readonly Color OverdueText = ColorTranslator.FromHtml("#DC2626");

    // Platform Badges
    public static readonly Color YouTubeBg = ColorTranslator.FromHtml("#FFDAD6");
    public static readonly Color YouTubeText = ColorTranslator.FromHtml("#DC2626");

    public static readonly Color TikTokBg = ColorTranslator.FromHtml("#DCE2F3");
    public static readonly Color TikTokText = ColorTranslator.FromHtml("#151C27");

    public static readonly Color InstagramBg = ColorTranslator.FromHtml("#E5E2E1");
    public static readonly Color InstagramText = ColorTranslator.FromHtml("#92400E");

    public static readonly Color FacebookBg = ColorTranslator.FromHtml("#D5E3FC");
    public static readonly Color FacebookText = ColorTranslator.FromHtml("#1D4ED8");

    // ==========================================
    // 2. TYPOGRAPHY (Segoe UI & Consolas)
    // ==========================================
    public static readonly Font FontH1 = new("Segoe UI", 16F, FontStyle.Bold); // ~22-24px
    public static readonly Font FontH2 = new("Segoe UI", 13.5F, FontStyle.Bold); // ~18-20px
    public static readonly Font FontH3 = new("Segoe UI", 11F, FontStyle.Bold); // ~14-16px
    public static readonly Font FontBody = new("Segoe UI", 9.5F, FontStyle.Regular); // ~12.5px
    public static readonly Font FontBodyBold = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font FontLabel = new("Segoe UI", 8.25F, FontStyle.Regular); // ~11px
    public static readonly Font FontLabelBold = new("Segoe UI", 8.25F, FontStyle.Bold);
    public static readonly Font FontMicro = new("Segoe UI", 7.5F, FontStyle.Bold); // ~10px
    public static readonly Font FontMono = new("Consolas", 8.5F, FontStyle.Bold); // ID / Code
    public static readonly Font FontMonoLarge = new("Consolas", 10F, FontStyle.Bold);
    public static readonly Font FontPageTitle = new("Segoe UI", 20F, FontStyle.Bold);  // "Production Board"
    public static readonly Font FontCardTitle = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font FontNav = new("Segoe UI Semibold", 10.5F, FontStyle.Regular);

    // ==========================================
    // 3. PIPELINE STAGES (Giai đoạn quy trình)
    // ==========================================
    public static Color GetStageColor(ContentStatus status) => status switch
    {
        ContentStatus.Idea => StageIdea,
        ContentStatus.Script => StageScript,
        ContentStatus.Production => StageProduction,
        ContentStatus.Editing => StageEditing,
        ContentStatus.Review => StageReview,
        ContentStatus.Ready => StageReady,
        ContentStatus.Published => StagePublished,
        _ => StageIdea,
    };

    public static string GetStageDisplayName(ContentStatus status) => status switch
    {
        ContentStatus.Idea => "Ideas & Discovery",
        ContentStatus.Script => "Scripting & Outline",
        ContentStatus.Production => "Filming / Studio",
        ContentStatus.Editing => "Post-Production",
        ContentStatus.Review => "Review & QA",
        ContentStatus.Ready => "Ready",
        ContentStatus.Published => "Scheduled & Live",
        _ => status.ToString(),
    };

    public static string GetStageSubtitle(ContentStatus status) => status switch
    {
        ContentStatus.Idea => "Concept refinement",
        ContentStatus.Script => "Hook & beats",
        ContentStatus.Production => "A-roll & B-roll capture",
        ContentStatus.Editing => "Rough cut & color/audio",
        ContentStatus.Review => "Producer sign-off",
        ContentStatus.Ready => "Queued for publish",
        ContentStatus.Published => "Delivered to platform",
        _ => ""
    };

    // Platform colors mapping
    public static (Color Bg, Color Text, string IconPrefix) GetPlatformStyle(string platform) =>
        platform.Trim().ToLowerInvariant() switch
        {
            "youtube" => (YouTubeBg, YouTubeText, "▶ YouTube"),
            "tiktok" => (TikTokBg, TikTokText, "♪ TikTok"),
            "instagram" => (InstagramBg, InstagramText, "📷 Instagram"),
            "facebook" => (FacebookBg, FacebookText, "🌐 Facebook"),
            _ => (Neutral100, Neutral700, platform)
        };

    public static (Color Bg, Color Text, string Label) GetPriorityStyle(Priority priority) => priority switch
    {
        Priority.High => (PriorityHighBg, PriorityHighText, "HIGH"),
        Priority.Medium => (PriorityMedBg, PriorityMedText, "MED"),
        _ => (PriorityLowBg, PriorityLowText, "LOW"),
    };

    // ==========================================
    // 4. GRAPHICS DRAWING HELPERS
    // ==========================================

    /// <summary>Tạo GraphicsPath bo góc chuẩn.</summary>
    public static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        int d = radius * 2;
        var arc = new Rectangle(bounds.X, bounds.Y, d, d);

        // Top left
        path.AddArc(arc, 180, 90);

        // Top right
        arc.X = bounds.Right - d;
        path.AddArc(arc, 270, 90);

        // Bottom right
        arc.Y = bounds.Bottom - d;
        path.AddArc(arc, 0, 90);

        // Bottom left
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    /// <summary>Vẽ Badge hình pill bo tròn tuyệt đối.</summary>
    public static void DrawBadge(Graphics g, string text, Color bg, Color textColor, Rectangle rect, Font? font = null)
    {
        font ??= FontMicro;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int radius = rect.Height / 2;
        using var path = CreateRoundedRectanglePath(rect, radius);
        using var brush = new SolidBrush(bg);
        g.FillPath(brush, path);

        using var textBrush = new SolidBrush(textColor);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(text, font, textBrush, rect, sf);
    }

    /// <summary>Vẽ Avatar hình tròn với 2 chữ cái viết tắt (initials).</summary>
    public static void DrawAvatar(Graphics g, string initials, Rectangle rect, Color? bg = null, Color? textColor = null, Font? font = null)
    {
        bg ??= Neutral800;
        textColor ??= White;
        font ??= FontMicro;

        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var brush = new SolidBrush(bg.Value);
        g.FillEllipse(brush, rect);

        // Viền rất mảnh để avatar không chìm vào nền
        using var pen = new Pen(Color.FromArgb(20, 0, 0, 0), 1f);
        g.DrawEllipse(pen, rect);

        using var textBrush = new SolidBrush(textColor.Value);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(initials.ToUpperInvariant(), font, textBrush, rect, sf);
    }
}
