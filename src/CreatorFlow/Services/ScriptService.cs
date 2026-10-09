using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services.AI;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Services;

/// <summary>
/// Nghiệp vụ Quản lý Script (SCRUM-33):
///   - Mở script: chỉ thành viên Project; trả kèm quyền sửa, số liệu và Version.
///   - Lưu script dài (tối đa <see cref="MaxScriptLength"/> ký tự): chuẩn hóa, kiểm tra quyền theo
///     <see cref="ScriptAccessPolicy"/>, phát hiện xung đột bằng Version rồi compare-and-set ở DB.
///   - Chuẩn bị dữ liệu và truyền script đã lưu sang <see cref="IAiService"/>.
/// Status của Content vẫn chỉ đổi qua WorkflowService; service này chỉ ghi cột script.
/// </summary>
public class ScriptService
{
    /// <summary>Độ dài script tối đa (ký tự, sau chuẩn hóa). ContentService dùng chung hằng số này.</summary>
    public const int MaxScriptLength = 100_000;

    public const int MaxAiInstructionLength = 1000;

    private static readonly AiRequestType[] AllowedAiRequestTypes =
    {
        AiRequestType.Script, AiRequestType.Title, AiRequestType.Outline, AiRequestType.Repurpose, AiRequestType.Other,
    };

    private readonly IContentScriptRepository _scriptRepo;
    private readonly IContentDetailsRepository _detailsRepo;
    private readonly IPlatformRepository _platformRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IMyTaskRepository _taskRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiService _aiService;

    public ScriptService(
        IContentScriptRepository scriptRepo,
        IContentDetailsRepository detailsRepo,
        IPlatformRepository platformRepo,
        IProjectMemberRepository memberRepo,
        IMyTaskRepository taskRepo,
        IUnitOfWork unitOfWork,
        IAiService aiService)
    {
        _scriptRepo = scriptRepo;
        _detailsRepo = detailsRepo;
        _platformRepo = platformRepo;
        _memberRepo = memberRepo;
        _taskRepo = taskRepo;
        _unitOfWork = unitOfWork;
        _aiService = aiService;
    }

    /// <summary>true nếu đã có AIService thật (UI dùng để chọn thông báo phù hợp).</summary>
    public bool IsAiConfigured => _aiService.IsConfigured;

    // ==========================================================
    // Đọc
    // ==========================================================

    /// <summary>Mở script để xem/sửa. Ném UnauthorizedWorkflowActionException nếu không phải thành viên Project.</summary>
    public ScriptDocument Open(long contentId, long userId)
    {
        var (script, role) = LoadForMember(contentId, userId);
        return ToDocument(script, role, userId);
    }

    // ==========================================================
    // Ghi
    // ==========================================================

    /// <summary>
    /// Lưu script. <paramref name="expectedVersion"/> là <see cref="ScriptDocument.Version"/> lúc người dùng mở/lưu lần cuối.
    /// Ném: ContentValidationException (quá dài / Content bị khóa), UnauthorizedWorkflowActionException (không đủ quyền),
    /// ScriptConflictException (script đã bị người khác sửa). Trả về bản script sau khi lưu.
    /// </summary>
    public ScriptDocument Save(long contentId, string? text, string expectedVersion, long userId)
    {
        ArgumentNullException.ThrowIfNull(expectedVersion);

        string normalized = ScriptTextAnalyzer.Normalize(text);
        if (normalized.Length > MaxScriptLength)
            throw new ContentValidationException(
                $"Kịch bản tối đa {MaxScriptLength:N0} ký tự (hiện có {normalized.Length:N0}).");

        var (script, role) = LoadForMember(contentId, userId);

        ScriptAccess access = EvaluateAccess(script, role, userId);
        if (!access.CanEdit)
        {
            string reason = access.ReadOnlyReason ?? "Không thể sửa kịch bản này.";
            throw access.IsContentLocked
                ? new ContentValidationException(reason)
                : new UnauthorizedWorkflowActionException(reason);
        }

        if (!string.Equals(ScriptTextAnalyzer.ComputeVersion(script.Script), expectedVersion, StringComparison.Ordinal))
            throw new ScriptConflictException();

        if (string.Equals(ScriptTextAnalyzer.Normalize(script.Script), normalized, StringComparison.Ordinal))
            return ToDocument(script, role, userId); // không có gì thay đổi → không ghi DB

        string? toStore = normalized.Length == 0 ? null : normalized;

        bool updated;
        _unitOfWork.Begin();
        try
        {
            // Compare-and-set ở DB đóng nốt khoảng hở giữa lúc đọc ở trên và lúc ghi.
            updated = _scriptRepo.TryUpdateScript(contentId, script.Script, toStore);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }

        if (!updated)
            throw new ScriptConflictException();

        ContentScript saved = _scriptRepo.GetByContentId(contentId)
            ?? throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");
        return ToDocument(saved, role, userId);
    }

    // ==========================================================
    // AI
    // ==========================================================

    /// <summary>
    /// Chuẩn bị dữ liệu gửi AI từ script ĐÃ LƯU của Content (không gửi nội dung đang gõ dở).
    /// Mọi thành viên Project đều chuẩn bị/gửi được; việc này không sửa dữ liệu.
    /// </summary>
    public AiScriptRequest PrepareAiRequest(long contentId, long userId,
        AiRequestType requestType = AiRequestType.Script, string? instruction = null)
    {
        if (!AllowedAiRequestTypes.Contains(requestType))
            throw new ContentValidationException("Loại yêu cầu AI không áp dụng cho kịch bản.");

        string note = (instruction ?? string.Empty).Trim();
        if (note.Length > MaxAiInstructionLength)
            throw new ContentValidationException($"Yêu cầu thêm cho AI tối đa {MaxAiInstructionLength} ký tự.");

        var (script, _) = LoadForMember(contentId, userId);

        if (ScriptTextAnalyzer.Normalize(script.Script).Length == 0)
            throw new ContentValidationException("Kịch bản đang trống nên chưa thể gửi sang AI.");

        Content detail = _detailsRepo.GetDetail(contentId)
            ?? throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");
        List<string> platforms = _platformRepo.GetByContentId(contentId).Select(p => p.PlatformName).ToList();

        return ScriptAiRequestBuilder.Build(detail, platforms, script.Script, requestType, note, userId);
    }

    /// <summary>
    /// Chuẩn bị dữ liệu rồi gửi sang IAiService. Lỗi AI (chưa cấu hình, mất kết nối) trả về Failure thay vì ném exception;
    /// lỗi nghiệp vụ (không phải thành viên, script trống...) vẫn ném như PrepareAiRequest.
    /// </summary>
    public async Task<AiScriptResponse> SendToAiAsync(long contentId, long userId,
        AiRequestType requestType = AiRequestType.Script, string? instruction = null,
        CancellationToken cancellationToken = default)
    {
        if (!_aiService.IsConfigured)
            return AiScriptResponse.Failure(NotConfiguredAiService.Message);

        AiScriptRequest request = PrepareAiRequest(contentId, userId, requestType, instruction);

        try
        {
            return await _aiService.RequestScriptAssistAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Không lộ chi tiết lỗi của dịch vụ ngoài (có thể chứa URL/khóa) ra giao diện.
            return AiScriptResponse.Failure("Không thể kết nối dịch vụ AI lúc này. Vui lòng thử lại sau.");
        }
    }

    // ==========================================================
    // Helpers
    // ==========================================================

    private (ContentScript Script, ProjectRole Role) LoadForMember(long contentId, long userId)
    {
        ContentScript script = _scriptRepo.GetByContentId(contentId)
            ?? throw new InvalidOperationException($"Không tìm thấy Content #{contentId}.");

        ProjectRole role = _memberRepo.GetRole(script.ProjectId, userId)
            ?? throw new UnauthorizedWorkflowActionException("User không phải thành viên của Project này.");

        return (script, role);
    }

    private ScriptAccess EvaluateAccess(ContentScript script, ProjectRole role, long userId)
    {
        bool isCreator = script.CreatedByUserId == userId;
        bool isAssignee = role == ProjectRole.Creator
            && !isCreator
            && _taskRepo.GetAssignment(script.ContentId, userId) is not null;

        return ScriptAccessPolicy.Evaluate(role, script.Status, isCreator, isAssignee);
    }

    private ScriptDocument ToDocument(ContentScript script, ProjectRole role, long userId)
    {
        string normalized = ScriptTextAnalyzer.Normalize(script.Script);
        return new ScriptDocument
        {
            ContentId = script.ContentId,
            ProjectId = script.ProjectId,
            Text = normalized,
            Version = ScriptTextAnalyzer.ComputeVersion(script.Script),
            Access = EvaluateAccess(script, role, userId),
            Statistics = ScriptTextAnalyzer.Analyze(normalized),
        };
    }
}
