using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls
{
    /// <summary>
    /// Màn hình Lịch Content (khung do Designer dựng, logic + theme ở đây).
    /// Quy ước ranh giới: file này không sửa Controls trong .Designer.cs,
    /// chỉ đọc/ghi thuộc tính + gắn sự kiện; font/màu chữ ép UITheme tại ApplyTheme.
    /// </summary>
    public partial class CalendarControl : UserControl
    {
        private static readonly string[] PlatformItems =
            ["Tất cả", "TikTok", "YouTube", "Facebook", "Instagram"];

        private static readonly (string Label, ContentStatus Status)[] StatusItems =
        [
            ("Tất cả", ContentStatus.Idea),
            ("Ý tưởng", ContentStatus.Idea),
            ("Kịch bản", ContentStatus.Script),
            ("Sản xuất", ContentStatus.Production),
            ("Chỉnh sửa", ContentStatus.Editing),
            ("Kiểm duyệt", ContentStatus.Review),
            ("Sẵn sàng", ContentStatus.Ready),
            ("Đã đăng", ContentStatus.Published),
            ("Lưu trữ", ContentStatus.Archived),
        ];

        private readonly CalendarGridView _grid = new();
        private readonly SegmentedControl _segView = new();
        private CalendarView _view = CalendarView.Month;
        private DateTime _cursor = DateTime.Today;
        private string _stateAction = string.Empty;

        /// <summary>Nguồn cards dùng chung với Board (BoardForm gán khi mở màn hình; null = chưa chọn project).</summary>
        public Func<List<ContentBoardCard>?>? CardProvider { get; set; }

        /// <summary>BoardForm đăng ký để mở Drawer chi tiết khi click chip.</summary>
        public event Action<long>? ContentOpened;

        public CalendarControl()
        {
            InitializeComponent();
            ApplyTheme();
            _grid.Dock = DockStyle.Fill;
            pnlGridHost.Controls.Add(_grid);
            _grid.ChipClicked += id => ContentOpened?.Invoke(id);
            _grid.MoreClicked += date =>
            {
                _cursor = date;
                if (_segView.SelectedIndex == 2) RefreshData();
                else _segView.SelectedIndex = 2;
            };
            _segView.Items.Clear();
            _segView.Items.AddRange(["Tháng", "Tuần", "Ngày"]);
            _segView.Width = _segView.MeasureTotalWidth();
            _segView.Location = new Point(3, 0);
            pnlViewSwitch.Width = _segView.Width + 6;
            _segView.SelectedIndexChanged += (_, _) =>
            {
                _view = (CalendarView)_segView.SelectedIndex;
                RefreshData();
            };
            pnlViewSwitch.Controls.Add(_segView);
            flpToolbar.WrapContents = false;
            segPlatform.Items.Clear();
            segPlatform.Items.AddRange(PlatformItems);
            segPlatform.Width = segPlatform.MeasureTotalWidth();
            segPlatform.SelectedIndexChanged += (_, _) => RefreshData();
            cboStatus.Font = UITheme.FontBody;
            cboStatus.FlatStyle = FlatStyle.Flat;
            cboStatus.BackColor = UITheme.White;
            cboStatus.ForeColor = UITheme.Ink;
            cboStatus.Items.AddRange(StatusItems.Select(i => i.Label).ToArray());
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (_, _) => RefreshData();
            btnToday.Click += (_, _) => { _cursor = DateTime.Today; RefreshData(); };
            btnPrev.Click += (_, _) => Shift(-1);
            btnNext.Click += (_, _) => Shift(1);
            btnState.Click += (_, _) =>
            {
                if (_stateAction == "clear")
                {
                    segPlatform.SelectedIndex = 0;
                    cboStatus.SelectedIndex = 0;
                }

                RefreshData();
            };
            pnlState.Resize += (_, _) => ArrangeState();
        }

        /// <summary>Font/màu chữ ép theo UITheme (Designer chỉ giữ khung + chữ + vị trí).</summary>
        private void ApplyTheme()
        {
            BackColor = UITheme.Neutral50;
            lblTitle.Font = UITheme.FontH2;
            lblTitle.ForeColor = UITheme.Ink;
            badgeKind.Location = new Point(
                20 + TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font).Width + 12, 16);
            lblDesc.Font = UITheme.FontBody;
            lblDesc.ForeColor = UITheme.Neutral600;
            lblSummary.Font = UITheme.FontBody;
            lblSummary.ForeColor = UITheme.Neutral600;
            lblPage.Font = UITheme.FontBodyBold;
            lblPage.ForeColor = UITheme.Ink;
            lblStateTitle.Font = UITheme.FontH3;
            lblStateTitle.ForeColor = UITheme.Ink;
            lblStateSub.Font = UITheme.FontBody;
            lblStateSub.ForeColor = UITheme.Neutral600;
            pnlGridHost.BackColor = UITheme.White;
            pnlState.BackColor = UITheme.White;
        }

        /// <summary>Tải lại từ provider (BoardForm gọi khi mở màn hình và khi Drawer đóng).</summary>
        public void RefreshData()
        {
            List<ContentBoardCard>? cards;
            try
            {
                cards = CardProvider?.Invoke();
            }
            catch
            {
                ShowState("error", "Không tải được dữ liệu", "Kiểm tra kết nối và thử lại.", "Thử lại", "retry");
                return;
            }

            if (cards is null)
            {
                ShowState("empty", "Chưa chọn project", "Chọn một project để xem lịch content.", null, string.Empty);
                return;
            }

            List<ContentBoardCard> filtered = Filter(cards);
            List<CalendarChip> chips = CalendarDateLogic.BuildChips(filtered, DateTime.Today);
            _grid.View = _view;
            _grid.CursorDate = _cursor;
            _grid.ChipsByDate = CalendarDateLogic.GroupByDate(chips);
            _grid.Unscheduled = CalendarDateLogic.Unscheduled(filtered);
            _grid.Invalidate();
            RenderPeriod();
            lblSummary.Text = $"{chips.Count} mốc trên lịch · {_grid.Unscheduled.Count} chưa xếp lịch";
            ShowState(null, string.Empty, string.Empty, null, string.Empty);
            if (chips.Count == 0 && _grid.Unscheduled.Count == 0)
                ShowState("empty", "Chưa có content nào", "Thử đổi bộ lọc hoặc thêm content có deadline.", "Xóa bộ lọc", "clear");
        }

        private void ShowState(string? mode, string title, string sub, string? buttonText, string action)
        {
            _stateAction = action;
            lblStateTitle.Text = title;
            lblStateSub.Text = sub;
            btnState.Visible = buttonText is not null;
            if (buttonText is not null) btnState.Text = buttonText;
            pnlState.Visible = mode is not null;
            pnlGridHost.Visible = mode is null;
            if (pnlState.Visible) ArrangeState();
        }

        private void ArrangeState()
        {
            int centerX = pnlState.Width / 2;
            lblStateTitle.Location = new Point(centerX - lblStateTitle.Width / 2, pnlState.Height / 2 - 52);
            lblStateSub.Location = new Point(centerX - lblStateSub.Width / 2, pnlState.Height / 2 - 20);
            btnState.Location = new Point(centerX - btnState.Width / 2, pnlState.Height / 2 + 12);
        }

        private List<ContentBoardCard> Filter(List<ContentBoardCard> cards)
        {
            string? platform = segPlatform.SelectedIndex <= 0 ? null : segPlatform.SelectedValue;
            ContentStatus? status = cboStatus.SelectedIndex <= 0 ? null : StatusItems[cboStatus.SelectedIndex].Status;
            return cards.Where(c =>
                    (platform is null || c.Platforms.Any(p => string.Equals(p, platform, StringComparison.OrdinalIgnoreCase))) &&
                    (status is null || c.Status == status))
                .ToList();
        }

        private void Shift(int step)
        {
            _cursor = _view switch
            {
                CalendarView.Month => _cursor.AddMonths(step),
                CalendarView.Week => _cursor.AddDays(7 * step),
                _ => _cursor.AddDays(step),
            };
            RefreshData();
        }

        private void RenderPeriod()
        {
            lblPage.Text = _view switch
            {
                CalendarView.Month => $"Tháng {_cursor.Month}/{_cursor.Year}",
                CalendarView.Week => $"Tuần {CalendarDateLogic.StartOfWeek(_cursor):dd/MM}–{CalendarDateLogic.StartOfWeek(_cursor).AddDays(6):dd/MM}",
                _ => _cursor.ToString("dd/MM/yyyy"),
            };
        }
    }
}
