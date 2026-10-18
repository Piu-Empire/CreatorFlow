using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;
using CreatorFlow.Forms.IdeaBank;
using System.Drawing.Drawing2D;

namespace CreatorFlow.Controls.IdeaBank;

/// <summary>
/// Trang Idea Bank (kho ý tưởng) của Project hiện hành:
///   - Danh sách Idea với trạng thái, tag, người tạo; chọn một dòng để xem mô tả + ghi chú ở khung dưới.
///   - Tìm kiếm theo từ khóa (có trễ ngắn để không truy vấn mỗi phím), lọc theo trạng thái và tag.
///   - Chuyển Idea thành Content (SCRUM-31): Content mới nằm ở cột Idea của Production Board, Idea gốc được giữ lại.
///   - Thêm / sửa / xóa theo quyền: nút bị tắt khi người dùng không có quyền (IdeaService vẫn kiểm tra lại khi ghi).
/// Layout tĩnh nằm trong IdeaBankControl.Designer.cs, file này chỉ chứa dữ liệu, sự kiện và logic.
/// </summary>
public partial class IdeaBankControl : UserControl
{
    private const string AllStatusLabel = "Tất cả trạng thái";
    private const string AllTagLabel = "Tất cả tag";
    private const string NoIdeasMessage = "Chưa có ý tưởng phù hợp\r\nThử thay đổi bộ lọc hoặc thêm ý tưởng mới.";

    // Null only in the parameterless constructor used by the Visual Studio Designer.
    private readonly IIdeaBackend? _ideaService;
    private readonly ToastNotification? _toast;
    private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 300 };
    private List<Idea> _ideas = new();
    private bool _loading;
    private long? _pendingFocusIdeaId;

    public event EventHandler? IdeasChanged;
    public event EventHandler? CloseRequested;

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được control. Không dùng khi chạy thật.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IdeaBankControl()
    {
        InitializeComponent();
        _ideaService = null!;
    }

    public IdeaBankControl(IIdeaBackend ideaService)
    {
        InitializeComponent();
        _ideaService = ideaService;
        lblSubtitle.Text = "Kho ý tưởng của Project: thêm, sửa, gắn tag, ghi chú, tìm kiếm và lọc theo trạng thái.";

        _toast = new ToastNotification();
        Controls.Add(_toast);

        cboStatus.Items.Add(AllStatusLabel);
        foreach (IdeaStatus status in Enum.GetValues<IdeaStatus>())
            cboStatus.Items.Add(new StatusOption(status));
        cboStatus.SelectedIndex = 0;

        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); ReloadIdeas(); };
        txtSearch.TextChanged += (_, _) => { _searchTimer.Stop(); _searchTimer.Start(); };
        cboStatus.SelectedIndexChanged += (_, _) => ReloadIdeas();
        cboTag.SelectedIndexChanged += (_, _) => ReloadIdeas();

        Load += (_, _) =>
        {
            ReloadTagFilter();
            ReloadIdeas();
        };
        Disposed += (_, _) => _searchTimer.Dispose();
    }

    private Idea? SelectedIdea =>
        listViewIdeas.SelectedItems.Count > 0 ? listViewIdeas.SelectedItems[0].Tag as Idea : null;

    /// <summary>Chọn một Idea cụ thể, dùng khi điều hướng từ kết quả tìm kiếm toàn cục.</summary>
    public bool FocusIdea(long ideaId)
    {
        if (ideaId <= 0 || _ideaService is null) return false;
        _pendingFocusIdeaId = ideaId;
        if (!IsHandleCreated) return true;

        if (_ideas.All(idea => idea.IdeaId != ideaId))
        {
            _loading = true;
            try
            {
                txtSearch.Clear();
                cboStatus.SelectedIndex = 0;
                cboTag.SelectedIndex = 0;
            }
            finally { _loading = false; }
            ReloadIdeas();
        }

        FocusPendingIdea();
        return _ideas.Any(idea => idea.IdeaId == ideaId);
    }

    private void FocusPendingIdea()
    {
        if (_pendingFocusIdeaId is not long ideaId) return;
        ListViewItem? item = listViewIdeas.Items.Cast<ListViewItem>()
            .FirstOrDefault(row => (row.Tag as Idea)?.IdeaId == ideaId);
        if (item is null) return;

        item.Selected = true;
        item.Focused = true;
        item.EnsureVisible();
        listViewIdeas.Focus();
        _pendingFocusIdeaId = null;
        UpdateDetailAndButtons();
    }

    private static long ProjectId => CurrentSession.CurrentProjectId;
    private static long UserId => CurrentSession.CurrentUserId;

    // ==========================================================
    // Tải & lọc dữ liệu
    // ==========================================================
    private IdeaFilter BuildFilter()
    {
        string? tag = cboTag.SelectedIndex > 0 ? cboTag.SelectedItem as string : null;
        return new IdeaFilter
        {
            SearchText = txtSearch.Text.Trim(),
            Status = (cboStatus.SelectedItem as StatusOption)?.Status,
            Tag = tag,
        };
    }

    private void ReloadTagFilter()
    {
        string? previous = cboTag.SelectedIndex > 0 ? cboTag.SelectedItem as string : null;

        _loading = true;
        try
        {
            cboTag.Items.Clear();
            cboTag.Items.Add(AllTagLabel);
            foreach (string tag in SafeGetProjectTags())
                cboTag.Items.Add(tag);

            int index = previous is null ? 0 : cboTag.Items.IndexOf(previous);
            cboTag.SelectedIndex = index < 0 ? 0 : index;
        }
        finally
        {
            _loading = false;
        }
    }

    private List<string> SafeGetProjectTags()
    {
        if (_ideaService is null) return new List<string>();
        try { return _ideaService.GetProjectTags(ProjectId, UserId); }
        catch (Exception ex) when (ex is UnauthorizedWorkflowActionException or IdeaValidationException) { return new List<string>(); }
    }

    private void ReloadIdeas()
    {
        if (_loading || _ideaService is null) return;

        long? selectedId = SelectedIdea?.IdeaId;
        string? loadError = null;
        try
        {
            _ideas = _ideaService.GetIdeas(ProjectId, UserId, BuildFilter());
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            _ideas = new List<Idea>();
            loadError = ex.Message;
        }
        catch (IdeaValidationException ex)
        {
            // Lỗi tải dữ liệu được hiển thị ngay trong trang. MessageBox modal ở đây làm
            // trang nền bị đổi kích thước khi form cha đang chuyển/đo lại layout.
            _ideas = new List<Idea>();
            loadError = ex.Message;
        }

        listViewIdeas.BeginUpdate();
        try
        {
            listViewIdeas.Items.Clear();
            foreach (var idea in _ideas)
            {
                var item = new ListViewItem(idea.Code) { Tag = idea };
                item.SubItems.Add(idea.Title);
                item.SubItems.Add(IdeaRules.StatusLabel(idea.Status));
                item.SubItems.Add(idea.Tags.Count == 0 ? "—" : string.Join(", ", idea.Tags));
                item.SubItems.Add(idea.CreatedByName);
                item.SubItems.Add(idea.UpdatedAt.ToString("dd/MM/yyyy"));
                listViewIdeas.Items.Add(item);
                if (idea.IdeaId == selectedId || idea.IdeaId == _pendingFocusIdeaId) item.Selected = true;
            }
        }
        finally
        {
            listViewIdeas.EndUpdate();
        }

        lblCount.Text = loadError is not null
            ? "Không tải được ý tưởng."
            : _ideas.Count == 0 ? "Không có ý tưởng phù hợp." : $"{_ideas.Count} ý tưởng";
        lblEmptyState.Text = loadError ?? NoIdeasMessage;
        lblEmptyState.ForeColor = loadError is null ? UITheme.Neutral600 : UITheme.Danger;
        lblEmptyState.Font = loadError is null ? UITheme.FontH2 : UITheme.FontBody;
        lblEmptyState.Visible = _ideas.Count == 0;
        listViewIdeas.Visible = _ideas.Count > 0;
        UpdateDetailAndButtons();
        ResizeIdeaColumns();
        FocusPendingIdea();
    }

    private void UpdateDetailAndButtons()
    {
        var idea = SelectedIdea;
        var service = _ideaService;
        btnNew.Enabled = service?.CanCreate(ProjectId, UserId) == true;
        btnEdit.Enabled = service is not null && idea is not null && service.CanEdit(idea, UserId);
        btnDelete.Enabled = service is not null && idea is not null && service.CanDelete(idea, UserId);
        btnConvert.Enabled = service is not null && idea is not null && service.CanConvert(idea, UserId);

        if (idea is null)
        {
            txtDetail.Text = string.Empty;
            txtDetail.Visible = false;
            detailPlaceholder.Visible = true;
            return;
        }

        txtDetail.Visible = true;
        detailPlaceholder.Visible = false;
        txtDetail.Text =
            $"{idea.Code} — {idea.Title}" + Environment.NewLine +
            "Mô tả: " + (idea.Description.Length == 0 ? "(chưa có)" : idea.Description) + Environment.NewLine +
            "Ghi chú: " + (idea.Note.Length == 0 ? "(chưa có)" : idea.Note) +
            (idea.ConvertedContentId is long contentId
                ? Environment.NewLine + $"Đã chuyển thành Content #{contentId}"
                : string.Empty);
    }

    // ==========================================================
    // Sự kiện giao diện
    // ==========================================================
    private void listViewIdeas_SelectedIndexChanged(object? sender, EventArgs e) => UpdateDetailAndButtons();

    private void listViewIdeas_DoubleClick(object? sender, EventArgs e)
    {
        if (btnEdit.Enabled) btnEdit_Click(sender, e);
    }

    private void listViewIdeas_SizeChanged(object? sender, EventArgs e) => ResizeIdeaColumns();

    private void lblSearchIcon_Paint(object? sender, PaintEventArgs e) =>
        UIIcons.Search(e.Graphics,
            new Rectangle((lblSearchIcon.Width - 17) / 2, (lblSearchIcon.Height - 17) / 2, 17, 17),
            Color.FromArgb(148, 163, 184));

    private void footerPanel_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(238, 240, 243));
        e.Graphics.DrawLine(pen, 0, 0, footerPanel.Width, 0);
    }

    private void listViewIdeas_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using var background = new SolidBrush(Color.FromArgb(248, 250, 253));
        e.Graphics.FillRectangle(background, e.Bounds);
        TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? string.Empty, UITheme.FontLabelBold,
            Rectangle.Inflate(e.Bounds, -12, 0), Color.FromArgb(91, 111, 143),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        using var divider = new Pen(Color.FromArgb(232, 237, 245));
        e.Graphics.DrawLine(divider, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void listViewIdeas_DrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        // The item background is painted with each subitem so the selected row fills the full width.
    }

    private void listViewIdeas_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        bool selected = e.Item?.Selected == true;
        Color rowColor = selected ? Color.FromArgb(245, 243, 255) : Color.White;
        using (var background = new SolidBrush(rowColor))
            e.Graphics.FillRectangle(background, e.Bounds);

        if (e.ColumnIndex == 2 && e.Item?.Tag is Idea idea)
        {
            DrawStatusPill(e.Graphics, e.Bounds, idea.Status);
        }
        else if (e.ColumnIndex == 3 && e.Item?.Tag is Idea taggedIdea)
        {
            DrawTagPills(e.Graphics, e.Bounds, taggedIdea.Tags);
        }
        else
        {
            Font font = e.ColumnIndex == 0 ? UITheme.FontMono : e.ColumnIndex == 1 ? UITheme.FontBodyBold : UITheme.FontBody;
            Color color = e.ColumnIndex == 0 ? Color.FromArgb(132, 150, 177) : Color.FromArgb(48, 63, 88);
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? string.Empty, font,
                Rectangle.Inflate(e.Bounds, -12, 0), color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        using var divider = new Pen(Color.FromArgb(239, 243, 248));
        e.Graphics.DrawLine(divider, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private static void DrawTagPills(Graphics graphics, Rectangle cellBounds, IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            TextRenderer.DrawText(graphics, "—", UITheme.FontBody, Rectangle.Inflate(cellBounds, -12, 0),
                Color.FromArgb(151, 165, 187), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        int x = cellBounds.Left + 10;
        int y = cellBounds.Top + (cellBounds.Height - 24) / 2;
        foreach (string tag in tags)
        {
            int width = Math.Min(cellBounds.Right - x - 8, TextRenderer.MeasureText(tag, UITheme.FontMicro).Width + 18);
            if (width < 20) break;
            var badge = new Rectangle(x, y, width, 24);
            using var path = UITheme.CreateRoundedRectanglePath(badge, 6);
            using var fill = new SolidBrush(Color.FromArgb(23, 32, 51));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillPath(fill, path);
            TextRenderer.DrawText(graphics, tag, UITheme.FontMicro,
                new Rectangle(badge.X + 8, badge.Y, badge.Width - 16, badge.Height), Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            x += width + 6;
        }
    }

    private static void DrawStatusPill(Graphics graphics, Rectangle cellBounds, IdeaStatus status)
    {
        (Color background, Color foreground) = status switch
        {
            IdeaStatus.Backlog => (Color.FromArgb(243, 238, 255), Color.FromArgb(109, 40, 217)),
            IdeaStatus.Converted => (Color.FromArgb(245, 245, 247), Color.FromArgb(71, 85, 105)),
            IdeaStatus.Archived => (Color.FromArgb(245, 245, 245), Color.FromArgb(107, 114, 128)),
            _ => (Color.FromArgb(241, 245, 249), Color.FromArgb(91, 111, 143)),
        };

        string text = IdeaRules.StatusLabel(status);
        int width = Math.Min(cellBounds.Width - 16, TextRenderer.MeasureText(text, UITheme.FontLabel).Width + 24);
        int height = 24;
        if (width < 8 || cellBounds.Height < height) return;
        var pill = new Rectangle(cellBounds.Left + 10, cellBounds.Top + (cellBounds.Height - height) / 2, width, height);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UITheme.CreateRoundedRectanglePath(pill, height / 2);
        using var fill = new SolidBrush(background);
        graphics.FillPath(fill, path);
        using var dot = new SolidBrush(foreground);
        graphics.FillEllipse(dot, pill.X + 8, pill.Y + 9, 6, 6);
        TextRenderer.DrawText(graphics, text, UITheme.FontLabel,
            new Rectangle(pill.X + 18, pill.Y, pill.Width - 20, pill.Height), foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void ResizeIdeaColumns()
    {
        if (listViewIdeas.Columns.Count != 6 || listViewIdeas.ClientSize.Width <= 0) return;
        int width = Math.Max(0, listViewIdeas.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
        listViewIdeas.Columns[0].Width = Math.Max(82, (int)(width * 0.10));
        listViewIdeas.Columns[1].Width = Math.Max(220, (int)(width * 0.38));
        listViewIdeas.Columns[2].Width = Math.Max(150, (int)(width * 0.15));
        listViewIdeas.Columns[3].Width = Math.Max(100, (int)(width * 0.10));
        listViewIdeas.Columns[4].Width = Math.Max(140, (int)(width * 0.15));
        listViewIdeas.Columns[5].Width = Math.Max(105, width - listViewIdeas.Columns.Cast<ColumnHeader>().Take(5).Sum(column => column.Width));
    }

    private void btnClose_Click(object? sender, EventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void btnClearFilter_Click(object? sender, EventArgs e)
    {
        _loading = true;
        txtSearch.Text = string.Empty;
        cboStatus.SelectedIndex = 0;
        if (cboTag.Items.Count > 0) cboTag.SelectedIndex = 0;
        _loading = false;
        ReloadIdeas();
    }

    private void btnNew_Click(object? sender, EventArgs e)
    {
        var service = _ideaService;
        if (service is null) return;
        using var dialog = new IdeaEditDialog(SafeGetProjectTags());
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Execute(() => service.Create(ProjectId, dialog.Draft, UserId), "Da them y tuong moi");
    }

    private void btnEdit_Click(object? sender, EventArgs e)
    {
        var service = _ideaService;
        var idea = SelectedIdea;
        if (service is null || idea is null) return;

        using var dialog = new IdeaEditDialog(SafeGetProjectTags(), idea);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Execute(() => service.Update(idea.IdeaId, dialog.Draft, UserId), "Da cap nhat y tuong");
    }

    private void btnConvert_Click(object? sender, EventArgs e)
    {
        var service = _ideaService;
        var idea = SelectedIdea;
        if (service is null || idea is null) return;

        var confirm = MessageBox.Show(this,
            $"Chuyển ý tưởng \"{idea.Title}\" thành Content?{Environment.NewLine}{Environment.NewLine}" +
            "Content mới nằm ở cột Idea của Production Board. Ý tưởng gốc được giữ lại và đánh dấu \"Đã chuyển Content\".",
            "Chuyển thành Content", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        Execute(() => service.ConvertToContent(idea.IdeaId, UserId), "Da chuyen thanh Content");
    }

    private void btnDelete_Click(object? sender, EventArgs e)
    {
        var service = _ideaService;
        var idea = SelectedIdea;
        if (service is null || idea is null) return;

        var confirm = MessageBox.Show(this, $"Xóa ý tưởng \"{idea.Title}\"? Thao tác này không hoàn tác được.",
            "Xóa ý tưởng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        Execute(() => service.Delete(idea.IdeaId, UserId), "Da xoa y tuong");
    }

    /// <summary>Chạy một thao tác ghi, hiện lỗi nghiệp vụ bằng hộp thoại, thành công thì tải lại danh sách và báo toast.</summary>
    private void Execute(Action action, string successToast)
    {
        try
        {
            action();
        }
        catch (IdeaValidationException ex)
        {
            MessageBox.Show(this, ex.Message, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ReloadTagFilter();
        ReloadIdeas();
        IdeasChanged?.Invoke(this, EventArgs.Empty);
        _toast?.ShowToast(this, successToast);
    }

    /// <summary>Phần tử ComboBox lọc trạng thái: hiện tên tiếng Việt, giữ giá trị enum.</summary>
    private sealed class StatusOption
    {
        public IdeaStatus Status { get; }
        public StatusOption(IdeaStatus status) => Status = status;
        public override string ToString() => IdeaRules.StatusLabel(Status);
    }
}