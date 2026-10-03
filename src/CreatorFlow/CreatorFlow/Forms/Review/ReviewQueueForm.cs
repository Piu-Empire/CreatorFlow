using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Forms.Review;

/// <summary>
/// Danh sách Content đang chờ kiểm duyệt (mục 2 UX spec - Review Queue, dành cho Owner/Manager).
/// Không tự kiểm tra Role ở đây - WorkflowService.ApproveReview/RejectReview sẽ chặn nếu User
/// không phải Owner/Manager của Project tương ứng (nguyên tắc #11 UX spec).
/// </summary>
public partial class ReviewQueueForm : Form
{
    private readonly IReviewQueueRepository _reviewQueueRepo;
    private readonly WorkflowService _workflowService;
    private List<ReviewQueueItem> _items = new();

    public ReviewQueueForm(IReviewQueueRepository reviewQueueRepo, WorkflowService workflowService)
    {
        InitializeComponent();
        _reviewQueueRepo = reviewQueueRepo;
        _workflowService = workflowService;

        Load += (_, _) => ReloadQueue();
    }

    private void ReloadQueue()
    {
        _items = _reviewQueueRepo.GetPendingReviews(CurrentSession.CurrentProjectId);

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
            if (dialog.Approved)
                _workflowService.ApproveReview(item.ContentId, CurrentSession.CurrentUserId, dialog.Feedback);
            else
                _workflowService.RejectReview(item.ContentId, CurrentSession.CurrentUserId, dialog.Feedback);

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
    }
}