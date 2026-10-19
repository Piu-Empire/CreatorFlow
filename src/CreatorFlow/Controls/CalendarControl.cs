using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
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

        // Panel bộ lọc mở từ nút "Bộ lọc"; dựng 1 lần rồi host trong ToolStripDropDown.
        private readonly CalendarFilterPanel _filterPanel = new();
        private ToolStripDropDown? _filterMenu;
        private ToolStripControlHost? _filterHost;

        /// <summary>Nguồn cards dùng chung với Board (BoardForm gán khi mở màn hình; null = chưa chọn project).</summary>
        public Func<List<ContentBoardCard>?>? CardProvider { get; set; }
        public string? UnavailableReason { get; set; }

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
            searchBox.PlaceholderText = "Tìm theo tên hoặc mã content...";
            searchBox.TextChanged += (_, _) => RefreshData();
            btnFilter.Click += (_, _) => ToggleFilterMenu();
            _filterPanel.SetPlatforms(PlatformItems);
            _filterPanel.SetStatuses(StatusItems);
            _filterPanel.FiltersChanged += (_, _) => RefreshData();
            btnToday.Click += (_, _) => { _cursor = DateTime.Today; RefreshData(); };
            btnPrev.Click += (_, _) => Shift(-1);
            btnNext.Click += (_, _) => Shift(1);
            btnState.Click += (_, _) =>
            {
                if (_stateAction == "clear")
                {
                    _filterPanel.Reset();
                    searchBox.Clear();
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
            if (UnavailableReason is { } unavailable)
            {
                ShowState("empty", "Nguồn dữ liệu chưa khả dụng", unavailable, null, string.Empty);
                return;
            }
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

            RebuildFilterOptions(cards);
            List<ContentBoardCard> filtered = Filter(cards);
            List<CalendarChip> chips = CalendarDateLogic.BuildChips(filtered, DateTime.Today);
            _grid.View = _view;
            _grid.CursorDate = _cursor;
            _grid.ChipsByDate = CalendarDateLogic.GroupByDate(chips);
            _grid.Unscheduled = CalendarDateLogic.Unscheduled(filtered);
            _grid.Invalidate();
            RenderPeriod();
            lblSummary.Text = $"{chips.Count} mốc trên lịch · {_grid.Unscheduled.Count} chưa xếp lịch";
            UpdateFilterBadge();
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

        private List<ContentBoardCard> Filter(List<ContentBoardCard> cards) =>
            CalendarDateLogic.ApplyFilters(
                cards,
                _filterPanel.Platform,
                _filterPanel.Status,
                _filterPanel.CreatorId,
                _filterPanel.ProjectId,
                searchBox.QueryText);

        /// <summary>Cập nhật nhãn nút "Bộ lọc" kèm số tiêu chí đang bật (gồm cả từ khóa tìm kiếm).</summary>
        private void UpdateFilterBadge()
        {
            int active = _filterPanel.ActiveCount + (searchBox.QueryText.Length > 0 ? 1 : 0);
            btnFilter.Text = active > 0 ? $"Bộ lọc ({active})" : "Bộ lọc";
        }

        /// <summary>Mở/đóng panel "Bộ lọc" ngay dưới nút.</summary>
        private void ToggleFilterMenu()
        {
            if (_filterMenu is { Visible: true } open)
            {
                open.Close();
                return;
            }

            EnsureFilterMenu();
            _filterMenu!.Show(btnFilter, new Point(0, btnFilter.Height + 4));
        }

        private void EnsureFilterMenu()
        {
            if (_filterMenu is not null) return;
            _filterHost = new ToolStripControlHost(_filterPanel)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = _filterPanel.Size,
            };
            var menu = new ToolStripDropDown { Padding = new(4), BackColor = UITheme.White };
            menu.SizeChanged += (_, _) =>
            {
                using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, menu.Width, menu.Height), 8);
                var old = menu.Region;
                menu.Region = new Region(path);
                old?.Dispose();
            };
            menu.Paint += (_, e) =>
            {
                using var path = UITheme.CreateRoundedRectanglePath(new Rectangle(0, 0, menu.Width - 1, menu.Height - 1), 8);
                using var pen = new Pen(UITheme.Neutral200);
                e.Graphics.DrawPath(pen, path);
            };
            menu.Items.Add(_filterHost);
            _filterMenu = menu;
            Disposed += (_, _) => menu.Dispose();
        }

        /// <summary>Dựng option Creator/Project từ chính cards (nguồn chưa lọc), giữ lựa chọn hiện tại nếu còn.</summary>
        private void RebuildFilterOptions(List<ContentBoardCard> cards)
        {
            List<(long Id, string Name)> creators = cards.SelectMany(c => c.Assignees)
                .GroupBy(a => a.UserId)
                .Select(g => (Id: g.Key, Name: g.First().Name))
                .OrderBy(x => x.Name)
                .ToList();
            _filterPanel.SetCreators(creators);

            List<(long Id, string Name)> projects = cards.Select(c => c.ProjectId)
                .Distinct()
                .OrderBy(id => id)
                .Select(id => (Id: id, Name: ProjectDisplayName(id)))
                .ToList();
            _filterPanel.SetProjects(projects);
        }

        private static string ProjectDisplayName(long projectId) =>
            projectId == CurrentSession.CurrentProjectId && !string.IsNullOrWhiteSpace(CurrentSession.CurrentProjectName)
                ? CurrentSession.CurrentProjectName
                : $"Project {projectId}";

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
