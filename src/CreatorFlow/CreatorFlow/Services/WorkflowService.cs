using System;
using System.Collections.Generic;
using System.Linq;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Service chịu trách nhiệm về Workflow của Content (mục 5.2, 5.3, 5.9 trong tài liệu):
///   - Xác định trạng thái nào được chuyển sang trạng thái nào.
///   - Kiểm tra quyền Owner/Manager/Creator theo Project.
///   - Ghi ContentStatusHistory cho mọi lần đổi trạng thái.
///   - Bọc các thao tác nhiều bước trong 1 transaction (IUnitOfWork).
/// Form/UserControl KHÔNG tự đổi Contents.Status trực tiếp mà luôn gọi qua Service này.
/// </summary>
public class WorkflowService
{
    private readonly IContentRepository _contentRepo;
    private readonly IContentStatusHistoryRepository _historyRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IUnitOfWork _unitOfWork;

    // Luồng chuyển trạng thái "đi thẳng" (không cần tạo Review kèm theo).
    // Riêng Editing -> Review và Review -> Ready/Editing được xử lý bằng
    // các hàm SubmitForReview / ApproveReview / RejectReview riêng vì có
    // liên quan tới bảng Reviews.
    private static readonly Dictionary<ContentStatus, ContentStatus> SimpleNextStatus =
        new Dictionary<ContentStatus, ContentStatus>
        {
            { ContentStatus.Idea, ContentStatus.Script },
            { ContentStatus.Script, ContentStatus.Production },
            { ContentStatus.Production, ContentStatus.Editing },
            { ContentStatus.Ready, ContentStatus.Published },
        };

    public WorkflowService(
        IContentRepository contentRepo,
        IContentStatusHistoryRepository historyRepo,
        IReviewRepository reviewRepo,
        IProjectMemberRepository memberRepo,
        IUnitOfWork unitOfWork)
    {
        _contentRepo = contentRepo;
        _historyRepo = historyRepo;
        _reviewRepo = reviewRepo;
        _memberRepo = memberRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Trả về Status kế tiếp cho các bước "đi thẳng" (không liên quan Reviews),
    /// hoặc null nếu Status hiện tại không có bước đi thẳng (VD: EDITING, REVIEW, PUBLISHED).
    /// UI dùng hàm này để quyết định hiện nút "Chuyển sang [X]" nào, thay vì tự đoán luồng.
    /// </summary>
    public ContentStatus? GetNextSimpleStatus(ContentStatus current)
    {
        return SimpleNextStatus.TryGetValue(current, out var next) ? next : (ContentStatus?)null;
    }

    /// <summary>Kiểm tra 1 cặp (from, to) có hợp lệ theo luồng chuẩn hay không.</summary>
    public bool CanTransition(ContentStatus from, ContentStatus to)
    {
        if (SimpleNextStatus.TryGetValue(from, out var next) && next == to)
            return true;

        if (from == ContentStatus.Editing && to == ContentStatus.Review)
            return true; // qua SubmitForReview

        if (from == ContentStatus.Review && (to == ContentStatus.Ready || to == ContentStatus.Editing))
            return true; // qua ApproveReview / RejectReview

        return false;
    }

    /// <summary>
    /// Dùng cho các bước chuyển đơn giản, KHÔNG liên quan Reviews:
    /// IDEA→SCRIPT, SCRIPT→PRODUCTION, PRODUCTION→EDITING, READY→PUBLISHED.
    /// Muốn gửi duyệt (EDITING→REVIEW) dùng SubmitForReview.
    /// Muốn duyệt/từ chối (REVIEW→...) dùng ApproveReview / RejectReview.
    /// </summary>
    public void ChangeStatus(long contentId, ContentStatus newStatus, long changedByUserId, string? note = null)
    {
        var content = GetContentOrThrow(contentId);

        if (newStatus == ContentStatus.Review || content.Status == ContentStatus.Review)
            throw new InvalidOperationException(
                "Dùng SubmitForReview / ApproveReview / RejectReview cho các bước liên quan tới REVIEW.");

        if (!CanTransition(content.Status, newStatus))
            throw new InvalidWorkflowTransitionException(content.Status, newStatus);

        EnsureIsProjectMember(content.ProjectId, changedByUserId);

        ApplyStatusChange(content, newStatus, changedByUserId, note);
    }

    /// <summary>
    /// EDITING → REVIEW. Tạo 1 dòng Review mới với ReviewNo = lớn nhất hiện có + 1
    /// (Review không ghi đè lần duyệt cũ - mục 5.3).
    /// </summary>
    public void SubmitForReview(long contentId, long submittedByUserId, string? note = null)
    {
        var content = GetContentOrThrow(contentId);

        if (content.Status != ContentStatus.Editing)
            throw new InvalidWorkflowTransitionException(content.Status, ContentStatus.Review);

        EnsureIsProjectMember(content.ProjectId, submittedByUserId);

        _unitOfWork.Begin();
        try
        {
            var review = new Review
            {
                ContentId = contentId,
                ReviewNo = _reviewRepo.GetLatestReviewNo(contentId) + 1,
                SubmittedByUserId = submittedByUserId,
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
            };
            _reviewRepo.Add(review);

            ApplyStatusChangeNoTransaction(content, ContentStatus.Review, submittedByUserId, note);

            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    /// <summary>
    /// REVIEW → READY. Chỉ Owner/Manager của Project được Approve (mục 5.9).
    /// </summary>
    public void ApproveReview(long contentId, long reviewerUserId, string? feedback = null)
    {
        DecideReview(contentId, reviewerUserId, feedback, approve: true);
    }

    /// <summary>
    /// REVIEW → EDITING (bị từ chối, quay lại chỉnh sửa - mục 5.3).
    /// Chỉ Owner/Manager của Project được Reject.
    /// </summary>
    public void RejectReview(long contentId, long reviewerUserId, string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
            throw new ArgumentException("Reject Review bắt buộc phải có Feedback.", nameof(feedback));

        DecideReview(contentId, reviewerUserId, feedback, approve: false);
    }

    private void DecideReview(long contentId, long reviewerUserId, string feedback, bool approve)
    {
        var content = GetContentOrThrow(contentId);

        if (content.Status != ContentStatus.Review)
            throw new InvalidWorkflowTransitionException(
                content.Status, approve ? ContentStatus.Ready : ContentStatus.Editing);

        var role = EnsureIsProjectMember(content.ProjectId, reviewerUserId);
        if (role != ProjectRole.Owner && role != ProjectRole.Manager)
            throw new UnauthorizedWorkflowActionException("Chỉ Owner hoặc Manager mới được Duyệt/Từ chối Review.");

        var pendingReview = _reviewRepo.GetPendingReview(contentId);
        if (pendingReview == null)
            throw new InvalidOperationException("Không tìm thấy Review đang PENDING cho Content này.");

        _unitOfWork.Begin();
        try
        {
            pendingReview.Status = approve ? ReviewStatus.Approved : ReviewStatus.Rejected;
            pendingReview.ReviewerUserId = reviewerUserId;
            pendingReview.Feedback = feedback;
            pendingReview.DecidedAt = DateTime.UtcNow;
            _reviewRepo.Update(pendingReview);

            var newStatus = approve ? ContentStatus.Ready : ContentStatus.Editing;
            ApplyStatusChangeNoTransaction(content, newStatus, reviewerUserId, feedback);

            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ---- Helpers ----

    private Content GetContentOrThrow(long contentId)
    {
        var content = _contentRepo.GetById(contentId);
        if (content == null)
            throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");
        return content;
    }

    private ProjectRole EnsureIsProjectMember(long projectId, long userId)
    {
        var role = _memberRepo.GetRole(projectId, userId);
        if (role == null)
            throw new UnauthorizedWorkflowActionException("User không phải thành viên của Project này.");
        return role.Value;
    }

    /// <summary>Đổi trạng thái + ghi lịch sử, tự mở/đóng transaction riêng (dùng cho ChangeStatus).</summary>
    private void ApplyStatusChange(Content content, ContentStatus newStatus, long changedByUserId, string note)
    {
        _unitOfWork.Begin();
        try
        {
            ApplyStatusChangeNoTransaction(content, newStatus, changedByUserId, note);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    /// <summary>Đổi trạng thái + ghi lịch sử, KHÔNG tự mở transaction (dùng khi đã có transaction bên ngoài).</summary>
    private void ApplyStatusChangeNoTransaction(Content content, ContentStatus newStatus, long changedByUserId, string note)
    {
        var oldStatus = content.Status;

        _contentRepo.UpdateStatus(content.Id, newStatus);

        _historyRepo.Add(new ContentStatusHistory
        {
            ContentId = content.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Note = note,
        });

        content.Status = newStatus;
        content.UpdatedAt = DateTime.UtcNow;
    }
}