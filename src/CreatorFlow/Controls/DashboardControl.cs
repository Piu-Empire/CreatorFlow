using System.Drawing.Drawing2D;
using CreatorFlow.Models;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

/// <summary>
/// Màn hình Tổng quan Project (Dashboard): 6 chỉ số content của Project đang mở.
/// Dựng layout bằng code (không cần Designer). Nguồn dữ liệu do BoardForm gán qua <see cref="CardProvider"/>
/// (dùng chung _allCards với Board/Calendar) nên số liệu khớp với dữ liệu Board/DB.
/// Thẻ chỉ số vẽ trực tiếp theo .stat-card trong UI Style Guide (icon chip + nhãn + số lớn + chú thích).
/// </summary>
public sealed class DashboardControl : UserControl
{
    private const int CardGap = 16;
    private const int CardHeight = 132;   // gồm cả margin dưới; chiều cao thẻ thực tế ≈ 116
    private static readonly Font StatValueFont = new("Consolas", 20F, FontStyle.Bold);

    private readonly StatCard _total =
        new("Total", "Tất cả content của Project", UITheme.Neutral800, UITheme.Ink, UIIcons.List);
    private readonly StatCard _inProgress =
        new("In Progress", "Đang sản xuất & chỉnh sửa", UITheme.Info, UITheme.Ink, UIIcons.Chart);
    private readonly StatCard _waitingReview =
        new("Waiting Review", "Đang chờ kiểm duyệt", UITheme.Warning, UITheme.Ink, UIIcons.ReviewQueue);
    private readonly StatCard _ready =
        new("Ready", "Sẵn sàng xuất bản", UITheme.Success, UITheme.Ink, UIIcons.CheckCircle);
    private readonly StatCard _published =
        new("Published", "Đã xuất bản", UITheme.Ink, UITheme.Ink, UIIcons.Upload);
    private readonly StatCard _overdue =
        new("Overdue", "Quá hạn, chưa đăng", UITheme.Danger, UITheme.Danger, UIIcons.Warning);

    private readonly TableLayoutPanel _cards;
    private readonly Label _loading;
    private readonly Label _state;

    /// <summary>Nguồn cards dùng chung với Board (BoardForm gán khi mở màn hình; null = chưa chọn project).</summary>
    public Func<List<ContentBoardCard>?>? CardProvider { get; set; }

    /// <summary>Lý do không có nguồn dữ liệu (server chưa nối Board cho Project này).</summary>
    public string? UnavailableReason { get; set; }

    public DashboardControl()
    {
        BackColor = UITheme.Neutral50;
        Padding = new Padding(32, 24, 32, 24);

        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = UITheme.Neutral50 };
        header.Controls.Add(new Label
        {
            Text = "Số liệu content của Project đang mở theo từng giai đoạn.",
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            AutoSize = true,
            Location = new Point(1, 40),
            BackColor = Color.Transparent,
        });
        header.Controls.Add(new Label
        {
            Text = "Tổng quan Project",
            Font = UITheme.FontPageTitle,
            ForeColor = UITheme.Ink,
            AutoSize = true,
            Location = new Point(0, 0),
            BackColor = Color.Transparent,
        });

        _cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 2,
            Height = CardHeight * 2,
            BackColor = UITheme.Neutral50,
        };
        for (int c = 0; c < 3; c++) _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        for (int r = 0; r < 2; r++) _cards.RowStyles.Add(new RowStyle(SizeType.Absolute, CardHeight));

        StatCard[] all = [_total, _inProgress, _waitingReview, _ready, _published, _overdue];
        for (int i = 0; i < all.Length; i++)
        {
            int column = i % 3, row = i / 3;
            StatCard card = all[i];
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, column < 2 ? CardGap : 0, CardGap);
            _cards.Controls.Add(card, column, row);
        }

        _state = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            BackColor = UITheme.Neutral50,
            Visible = false,
        };
        _loading = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đang tải…",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.FontBody,
            ForeColor = UITheme.Neutral600,
            BackColor = UITheme.Neutral50,
            Visible = false,
        };

        Controls.Add(_cards);
        Controls.Add(_state);
        Controls.Add(_loading);
        Controls.Add(header);
    }

    /// <summary>Hiện trạng thái "đang tải" trong lúc chờ BoardForm nạp lại card cho Project mới.</summary>
    public void BeginLoading()
    {
        _cards.Visible = false;
        _state.Visible = false;
        _loading.Visible = true;
    }

    /// <summary>Hiện một thông báo trạng thái (vd. lỗi tải dữ liệu) thay cho lưới chỉ số.</summary>
    public void ShowMessage(string message) => ShowState(message);

    /// <summary>Tải lại từ provider (BoardForm gọi khi mở màn hình và khi đổi/reload Project).</summary>
    public void RefreshData()
    {
        if (UnavailableReason is { } unavailable)
        {
            ShowState(unavailable);
            return;
        }

        List<ContentBoardCard>? cards;
        try
        {
            cards = CardProvider?.Invoke();
        }
        catch
        {
            ShowState("Không tải được dữ liệu. Kiểm tra kết nối và thử lại.");
            return;
        }

        if (cards is null)
        {
            ShowState("Chưa chọn project. Chọn một project để xem tổng quan.");
            return;
        }

        DashboardCounts counts = DashboardMetrics.From(cards, DateTime.Today);
        _total.SetValue(counts.Total);
        _inProgress.SetValue(counts.InProgress);
        _waitingReview.SetValue(counts.WaitingReview);
        _ready.SetValue(counts.Ready);
        _published.SetValue(counts.Published);
        _overdue.SetValue(counts.Overdue);

        _loading.Visible = false;
        _state.Visible = false;
        _cards.Visible = true;
    }

    private void ShowState(string message)
    {
        _loading.Visible = false;
        _cards.Visible = false;
        _state.Text = message;
        _state.Visible = true;
    }

    /// <summary>
    /// Thẻ chỉ số: icon chip (nền màu nhạt + icon theo màu chỉ số) góc phải trên, nhãn, con số lớn, chú thích ngữ cảnh.
    /// Vẽ trực tiếp để kiểm soát vị trí, đồng bộ với .stat-card trong UI Style Guide.
    /// </summary>
    private sealed class StatCard : RoundedPanel
    {
        private readonly string _label;
        private readonly string _caption;
        private readonly Color _accent;
        private readonly Color _valueColor;
        private readonly Action<Graphics, Rectangle, Color> _icon;
        private string _value = "0";

        public StatCard(
            string label, string caption, Color accent, Color valueColor, Action<Graphics, Rectangle, Color> icon)
        {
            _label = label;
            _caption = caption;
            _accent = accent;
            _valueColor = valueColor;
            _icon = icon;
            Radius = 12;
            FillColor = UITheme.White;
            BorderColor = UITheme.Neutral200;
        }

        public void SetValue(int value)
        {
            _value = value.ToString();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var chip = new Rectangle(Width - 56, 16, 40, 40);
            using (var path = UITheme.CreateRoundedRectanglePath(chip, 10))
            using (var fill = new SolidBrush(Color.FromArgb(30, _accent)))
                g.FillPath(fill, path);
            _icon(g, new Rectangle(chip.X + 10, chip.Y + 10, 20, 20), _accent);

            TextRenderer.DrawText(g, _label, UITheme.FontLabelBold,
                new Rectangle(18, 18, Width - 78, 16), UITheme.Neutral600,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, _value, StatValueFont,
                new Rectangle(18, 36, Width - 36, 38), _valueColor,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, _caption, UITheme.FontLabel,
                new Rectangle(18, Height - 30, Width - 36, 16), UITheme.Neutral400,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
    }
}
