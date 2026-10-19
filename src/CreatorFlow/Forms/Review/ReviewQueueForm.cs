using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Review;

/// <summary>
/// Danh sách Content đang chờ kiểm duyệt (mục 2 UX spec - Review Queue, dành cho Owner/Manager).
/// Không tự kiểm tra Role ở đây - Backend (API) hoặc WorkflowService sẽ chặn nếu User
/// không phải Owner/Manager của Project tương ứng (nguyên tắc #11 UX spec).
/// </summary>
public partial class ReviewQueueForm : Form
{
    private readonly IReviewQueueRepository? _reviewQueueRepo;
    private readonly WorkflowService? _workflowService;
    private readonly IReviewQueueBackend? _backend;
    private List<ReviewQueueItem> _items = new();

    /// <summary>Chế độ in-memory / PostgreSQL cục bộ (chưa có API).</summary>
    public ReviewQueueForm(IReviewQueueRepository reviewQueueRepo, WorkflowService workflowService)
        : this()
    {
        _reviewQueueRepo = reviewQueueRepo;
        _workflowService = workflowService;
    }

    /// <summary>Chế độ API: danh sách và quyết định duyệt đều qua CreatorFlow.Api.</summary>
    public ReviewQueueForm(IReviewQueueBackend backend)
        : this()
    {
        _backend = backend;
    }

    private ReviewQueueForm()
    {
        InitializeComponent();
        Load += (_, _) => ReloadQueue();
    }

    // Handler đặt tên (Designer không đọc được lambda trong InitializeComponent)
    private void panelTop_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.Neutral200, 1f);
        e.Graphics.DrawLine(pen, 0, panelTop.Height - 1, panelTop.Width, panelTop.Height - 1);
    }

    private void ReloadQueue()
    {
        try
        {
            _items = _backend is not null
                ? _backend.GetPending(CurrentSession.CurrentProjectId)
                : _reviewQueueRepo!.GetPendingReviews(CurrentSession.CurrentProjectId);
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        catch (Exception ex) when (ex is ContentValidationException or InvalidOperationException)
        {
            MessageBox.Show(this, ex.Message, "Không tải được Review Queue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        listViewQueue.BeginUpdate();
        listViewQueue.Items.Clear();

        foreach (var item in _items)
        {
            var listItem = new ListViewItem(item.ContentCode) { Tag = item };
            listItem.SubItems.Add(item.ContentTitle);
            listItem.SubItems.Add(item.SubmittedByName);
            listItem.SubItems.Add(item.SubmittedAt.ToString("dd/MM HH:mm"));
            listItem.SubItems.Add($"#{item.ReviewNo}");
            listViewQueue.Items.Add(listItem);
        }

        listViewQueue.EndUpdate();
        btnOpenReview.Enabled = false;
    }

    private void listViewQueue_SelectedIndexChanged(object? sender, EventArgs e)
    {
        btnOpenReview.Enabled = listViewQueue.SelectedItems.Count > 0;
    }

    private void btnRefresh_Click(object? sender, EventArgs e) => ReloadQueue();

    private void btnOpenReview_Click(object? sender, EventArgs e)
    {
        if (listViewQueue.SelectedItems.Count == 0)
            return;

        var item = (ReviewQueueItem)listViewQueue.SelectedItems[0].Tag!;

        using var dialog = new Board.ReviewDecisionDialog(
            $"{item.ContentCode} — {item.ContentTitle}",
            $"Review #{item.ReviewNo} — Submitted by {item.SubmittedByName} at {item.SubmittedAt:dd/MM HH:mm}");

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            if (_backend is not null)
                _backend.Decide(item.ContentId, dialog.Approved, dialog.Feedback);
            else if (dialog.Approved)
                _workflowService!.ApproveReview(item.ContentId, CurrentSession.CurrentUserId, dialog.Feedback);
            else
                _workflowService!.RejectReview(item.ContentId, CurrentSession.CurrentUserId, dialog.Feedback);

            ReloadQueue();
        }
        catch (UnauthorizedWorkflowActionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (InvalidWorkflowTransitionException ex)
        {
            MessageBox.Show(this, ex.Message, "Không thể xử lý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex) when (ex is ContentValidationException or InvalidOperationException)
        {
            MessageBox.Show(this, ex.Message, "Không thể xử lý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
