using CreatorFlow.Controls;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Forms.IdeaBank;

/// <summary>
/// Màn Idea Bank (kho ý tưởng) của Project hiện hành:
///   - Danh sách Idea với trạng thái, tag, người tạo; chọn một dòng để xem mô tả + ghi chú ở khung dưới.
///   - Tìm kiếm theo từ khóa (có trễ ngắn để không truy vấn mỗi phím), lọc theo trạng thái và tag.
///   - Chuyển Idea thành Content (SCRUM-31): Content mới nằm ở cột Idea của Production Board, Idea gốc được giữ lại.
///   - Thêm / sửa / xóa theo quyền: nút bị tắt khi người dùng không có quyền (IdeaService vẫn kiểm tra lại khi ghi).
/// Layout tĩnh nằm trong IdeaBankForm.Designer.cs, file này chỉ chứa dữ liệu, sự kiện và logic.
/// </summary>
public partial class IdeaBankForm : Form
{
    private const string AllStatusLabel = "Tất cả trạng thái";
    private const string AllTagLabel = "Tất cả tag";

    private readonly IdeaService _ideaService;
    private readonly ToastNotification? _toast;
    private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 300 };
    private List<Idea> _ideas = new();
    private bool _loading;

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được form. Không dùng khi chạy thật.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IdeaBankForm()
    {
        InitializeComponent();
        _ideaService = null!;
    }

    public IdeaBankForm(IdeaService ideaService)
    {
        InitializeComponent();
        _ideaService = ideaService;

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
        FormClosed += (_, _) => _searchTimer.Dispose();
    }

    private Idea? SelectedIdea =>
        listViewIdeas.SelectedItems.Count > 0 ? listViewIdeas.SelectedItems[0].Tag as Idea : null;

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
        try { return _ideaService.GetProjectTags(ProjectId, UserId); }
        catch (UnauthorizedWorkflowActionException) { return new List<string>(); }
    }

    private void ReloadIdeas()
    {
        if (_loading || _ideaService is null) return;

        long? selectedId = SelectedIdea?.IdeaId;
        try
        {
            _ideas = _ideaService.GetIdeas(ProjectId, UserId, BuildFilter());
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            _ideas = new List<Idea>();
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                item.SubItems.Add(string.Join(", ", idea.Tags));
                item.SubItems.Add(idea.CreatedByName);
                item.SubItems.Add(idea.UpdatedAt.ToString("dd/MM/yyyy"));
                listViewIdeas.Items.Add(item);
                if (idea.IdeaId == selectedId) item.Selected = true;
            }
        }
        finally
        {
            listViewIdeas.EndUpdate();
        }

        lblCount.Text = _ideas.Count == 0
            ? "Không có ý tưởng phù hợp."
            : $"{_ideas.Count} ý tưởng";
        UpdateDetailAndButtons();
    }

    private void UpdateDetailAndButtons()
    {
        var idea = SelectedIdea;
        btnNew.Enabled = _ideaService is not null && _ideaService.CanCreate(ProjectId, UserId);
        btnEdit.Enabled = idea is not null && _ideaService.CanEdit(idea, UserId);
        btnDelete.Enabled = idea is not null && _ideaService.CanDelete(idea, UserId);
        btnConvert.Enabled = idea is not null && _ideaService.CanConvert(idea, UserId);

        if (idea is null)
        {
            txtDetail.Text = string.Empty;
            return;
        }

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
        using var dialog = new IdeaEditDialog(SafeGetProjectTags());
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Execute(() => _ideaService.Create(ProjectId, dialog.Draft, UserId), "Da them y tuong moi");
    }

    private void btnEdit_Click(object? sender, EventArgs e)
    {
        var idea = SelectedIdea;
        if (idea is null) return;

        using var dialog = new IdeaEditDialog(SafeGetProjectTags(), idea);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Execute(() => _ideaService.Update(idea.IdeaId, dialog.Draft, UserId), "Da cap nhat y tuong");
    }

    private void btnConvert_Click(object? sender, EventArgs e)
    {
        var idea = SelectedIdea;
        if (idea is null) return;

        var confirm = MessageBox.Show(this,
            $"Chuyển ý tưởng \"{idea.Title}\" thành Content?{Environment.NewLine}{Environment.NewLine}" +
            "Content mới nằm ở cột Idea của Production Board. Ý tưởng gốc được giữ lại và đánh dấu \"Đã chuyển Content\".",
            "Chuyển thành Content", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        Execute(() => _ideaService.ConvertToContent(idea.IdeaId, UserId), "Da chuyen thanh Content");
    }

    private void btnDelete_Click(object? sender, EventArgs e)
    {
        var idea = SelectedIdea;
        if (idea is null) return;

        var confirm = MessageBox.Show(this, $"Xóa ý tưởng \"{idea.Title}\"? Thao tác này không hoàn tác được.",
            "Xóa ý tưởng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        Execute(() => _ideaService.Delete(idea.IdeaId, UserId), "Da xoa y tuong");
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