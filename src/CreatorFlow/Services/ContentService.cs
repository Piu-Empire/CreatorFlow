using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Nghiệp vụ tạo mới / chỉnh sửa thông tin Content (Status vẫn chỉ đổi qua WorkflowService).
/// Quy tắc:
///   - Phải là thành viên của Project.
///   - Tiêu đề bắt buộc (tối đa 200 ký tự), thời lượng tối đa 30 ký tự.
///   - Không tạo trực tiếp ở giai đoạn Review (Review phải đi qua Submit for Review).
///   - Sửa: Owner/Manager sửa mọi Content; Creator chỉ sửa Content do mình tạo; Content đã Published không được sửa.
/// </summary>
public class ContentService
{
    public static readonly string[] AvailableSprints = { "Sprint 24", "Sprint 25", "Sprint 26" };
    public static readonly string[] AvailablePlatforms = { "YouTube", "TikTok", "Instagram", "Facebook" };

    private readonly IContentRepository _contentRepo;
    private readonly IContentDetailsRepository _detailsRepo;
    private readonly IContentStatusHistoryRepository _historyRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ContentService(
        IContentRepository contentRepo,
        IContentDetailsRepository detailsRepo,
        IContentStatusHistoryRepository historyRepo,
        IProjectMemberRepository memberRepo,
        IUnitOfWork unitOfWork)
    {
        _contentRepo = contentRepo;
        _detailsRepo = detailsRepo;
        _historyRepo = historyRepo;
        _memberRepo = memberRepo;
        _unitOfWork = unitOfWork;
    }

    public List<ProjectMemberInfo> GetMembers(long projectId) => _memberRepo.GetMembers(projectId);

    public long Create(long projectId, ContentStatus initialStatus, ContentDraft draft, long userId)
    {
        Normalize(draft);
        Validate(draft);

        if (initialStatus == ContentStatus.Review)
            throw new ContentValidationException("Không thể tạo trực tiếp ở giai đoạn Review. Hãy tạo ở Editing rồi Submit Review.");

        EnsureIsProjectMember(projectId, userId);

        _unitOfWork.Begin();
        try
        {
            long id = _detailsRepo.Create(projectId, initialStatus, draft, userId);

            _historyRepo.Add(new ContentStatusHistory
            {
                ContentId = id,
                FromStatus = initialStatus,
                ToStatus = initialStatus,
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow,
                Note = "Tạo nội dung mới trên Production Board",
            });

            _unitOfWork.Commit();
            return id;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public void Update(long contentId, ContentDraft draft, long userId)
    {
        Normalize(draft);
        Validate(draft);

        var content = _contentRepo.GetById(contentId)
            ?? throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");

        var role = EnsureIsProjectMember(content.ProjectId, userId);

        if (content.Status == ContentStatus.Published)
            throw new ContentValidationException("Nội dung đã Published nên không thể chỉnh sửa.");

        if (role == ProjectRole.Creator && content.CreatedByUserId != userId)
            throw new UnauthorizedWorkflowActionException("Creator chỉ được sửa nội dung do chính mình tạo.");

        _unitOfWork.Begin();
        try
        {
            _detailsRepo.Update(contentId, draft);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    private static void Normalize(ContentDraft d)
    {
        d.Title = (d.Title ?? string.Empty).Trim();
        d.Description = (d.Description ?? string.Empty).Trim();
        d.EstimatedDuration = (d.EstimatedDuration ?? string.Empty).Trim();
        d.Sprint = string.IsNullOrWhiteSpace(d.Sprint) ? AvailableSprints[1] : d.Sprint.Trim();
        d.Platforms = (d.Platforms ?? new List<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Validate(ContentDraft d)
    {
        if (d.Title.Length == 0)
            throw new ContentValidationException("Vui lòng nhập tiêu đề.");
        if (d.Title.Length > 200)
            throw new ContentValidationException("Tiêu đề tối đa 200 ký tự.");
        if (d.EstimatedDuration.Length > 30)
            throw new ContentValidationException("Thời lượng dự kiến tối đa 30 ký tự (VD: 24 min).");
    }

    private ProjectRole EnsureIsProjectMember(long projectId, long userId)
    {
        var role = _memberRepo.GetRole(projectId, userId);
        if (role == null)
            throw new UnauthorizedWorkflowActionException("User không phải thành viên của Project này.");
        return role.Value;
    }
}
