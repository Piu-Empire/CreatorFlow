using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;

namespace CreatorFlow.Forms.IdeaBank;

/// <summary>
/// Dialog thêm / sửa một Idea. Giao diện nằm trong IdeaEditDialog.Designer.cs,
/// file này chỉ nạp dữ liệu, báo lỗi nhập ngay trong dialog (dùng chung quy tắc với IdeaService) và đóng gói IdeaDraft.
/// </summary>
public partial class IdeaEditDialog : Form
{
    /// <summary>Kết quả nhập, đọc sau khi ShowDialog() trả về DialogResult.OK.</summary>
    public IdeaDraft Draft { get; private set; } = new();

    /// <summary>Constructor không tham số CHỈ để Visual Studio Designer mở được form.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IdeaEditDialog()
    {
        InitializeComponent();
    }

    /// <param name="projectTags">Các tag đã có trong Project (hiện làm gợi ý).</param>
    /// <param name="existing">Idea đang sửa; null = thêm mới.</param>
    public IdeaEditDialog(List<string> projectTags, Idea? existing = null)
    {
        InitializeComponent();

        lblHeader.UseMnemonic = false;
        lblHeader.Text = existing is null ? "Thêm ý tưởng" : $"Sửa ý tưởng — {existing.Code}";
        lblTagHint.Text = projectTags.Count == 0
            ? "Chưa có tag nào trong Project. Tag mới sẽ được tạo khi lưu."
            : "Tag đã có: " + string.Join(", ", projectTags);

        foreach (var status in IdeaRules.SelectableStatuses)
            cboStatus.Items.Add(new StatusItem(status));

        if (existing?.Status == IdeaStatus.Converted)
        {
            // Idea đã chuyển thành Content: giữ nguyên trạng thái, không cho đổi tay.
            cboStatus.Items.Add(new StatusItem(IdeaStatus.Converted));
        }

        if (existing is null)
        {
            SelectStatus(IdeaStatus.Draft);
        }
        else
        {
            txtTitle.Text = existing.Title;
            txtDescription.Text = existing.Description;
            txtNote.Text = existing.Note;
            txtTags.Text = string.Join(", ", existing.Tags);
            SelectStatus(existing.Status);
            cboStatus.Enabled = existing.Status != IdeaStatus.Converted;
        }

        AcceptButton = btnSave;
    }

    private void SelectStatus(IdeaStatus status)
    {
        for (int i = 0; i < cboStatus.Items.Count; i++)
        {
            if (((StatusItem)cboStatus.Items[i]!).Status == status)
            {
                cboStatus.SelectedIndex = i;
                return;
            }
        }
        cboStatus.SelectedIndex = 0;
    }

    private void btnSave_Click(object? sender, EventArgs e)
    {
        var draft = new IdeaDraft
        {
            Title = txtTitle.Text,
            Description = txtDescription.Text,
            Note = txtNote.Text,
            Status = (cboStatus.SelectedItem as StatusItem)?.Status ?? IdeaStatus.Draft,
            Tags = txtTags.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
        };

        // Cùng bộ quy tắc với IdeaService để báo lỗi ngay trong dialog (không đóng dialog, không mất dữ liệu đã nhập).
        IdeaService.Normalize(draft);
        string? error = IdeaService.GetValidationError(draft);
        if (error != null)
        {
            lblError.Text = error;
            if (draft.Title.Length == 0 || draft.Title.Length > IdeaService.MaxTitleLength)
                txtTitle.Focus();
            return;
        }

        lblError.Text = string.Empty;
        Draft = draft;
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>Phần tử ComboBox: hiện tên trạng thái tiếng Việt nhưng giữ giá trị enum.</summary>
    private sealed class StatusItem
    {
        public IdeaStatus Status { get; }
        public StatusItem(IdeaStatus status) => Status = status;
        public override string ToString() => IdeaRules.StatusLabel(Status);
    }
}
