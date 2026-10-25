using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.ContentDetail;
using CreatorFlow.Contracts.Workflow;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services.Backends;

/// <summary>Content Detail chạy qua CreatorFlow.Api (quyền, validation, workflow kiểm tra ở Backend; client chỉ đổi DTO sang model hiển thị).</summary>
public sealed class ApiContentDetailBackend(ApiClient api, UserSession session) : IContentDetailBackend
{
    public ProjectRole? GetMyRole(long projectId)
    {
        try
        {
            var response = ApiTaskCall.Run(() => api.GetMyProjectRoleAsync(projectId, ApiTaskCall.BearerOf(session)), session);
            return Enum.TryParse<ProjectRole>(response.Role, ignoreCase: true, out var role) ? role : null;
        }
        catch (Services.Exceptions.UnauthorizedWorkflowActionException)
        {
            return null; // không phải thành viên
        }
    }

    public long Create(long projectId, ContentStatus initialStatus, ContentDraft draft)
    {
        var request = new CreateContentRequest(
            draft.Title,
            draft.Description,
            draft.ContentType,
            draft.Priority.ToString(),
            initialStatus.ToString(),
            ApiTaskMapper.ToDateOnly(draft.Deadline),
            ApiTaskMapper.ToDateOnly(draft.PlannedPublishAt),
            draft.Platforms,
            draft.AssigneeUserId,
            draft.Sprint,
            draft.SourceContentId);
        return ApiTaskCall.Run(() => api.CreateContentAsync(projectId, request, ApiTaskCall.BearerOf(session)), session).ContentId;
    }

    public Content GetDetail(long contentId)
    {
        var r = ApiTaskCall.Run(() => api.GetContentDetailAsync(contentId, ApiTaskCall.BearerOf(session)), session);
        return new Content
        {
            ContentId = r.ContentId,
            ProjectId = r.ProjectId,
            Title = r.Title,
            Description = r.Description,
            ContentType = r.ContentType,
            Priority = Enum.Parse<Priority>(r.Priority, ignoreCase: true),
            Status = Enum.Parse<ContentStatus>(r.Status, ignoreCase: true),
            Deadline = ApiTaskMapper.ToDateTime(r.Deadline),
            PlannedPublishAt = ApiTaskMapper.ToDateTime(r.PlannedPublishAt),
            CreatedBy = r.CreatedBy,
            SourceContentId = r.SourceContentId,
        };
    }

    public List<string> GetAvailablePlatforms() =>
        ApiTaskCall.Run(() => api.GetPlatformOptionsAsync(ApiTaskCall.BearerOf(session)), session).Platforms.ToList();

    public void Update(long contentId, ContentDraft draft)
    {
        var request = new UpdateContentRequest(
            draft.Title,
            draft.Description,
            draft.ContentType,
            draft.Priority.ToString(),
            ApiTaskMapper.ToDateOnly(draft.Deadline),
            ApiTaskMapper.ToDateOnly(draft.PlannedPublishAt),
            draft.Platforms,
            draft.Sprint);
        ApiTaskCall.Run(() => api.UpdateContentAsync(contentId, request, ApiTaskCall.BearerOf(session)), session);
    }

    public ScriptDocument OpenScript(long contentId) =>
        ToDocument(ApiTaskCall.Run(() => api.GetScriptAsync(contentId, ApiTaskCall.BearerOf(session)), session));

    public ScriptDocument SaveScript(long contentId, string? text, string expectedVersion)
    {
        var request = new SaveScriptRequest(text, expectedVersion);
        return ToDocument(ApiTaskCall.Run(() => api.SaveScriptAsync(contentId, request, ApiTaskCall.BearerOf(session)), session));
    }

    public List<ActivityItem> GetActivity(long contentId) =>
        ApiTaskCall.Run(() => api.GetActivityAsync(contentId, ApiTaskCall.BearerOf(session)), session).Items
            .Select(i => new ActivityItem
            {
                Timestamp = i.Timestamp.ToLocalTime(),
                ActorName = i.ActorName,
                Description = i.Description,
                Feedback = i.Feedback,
                Kind = i.Kind,
                FromStatus = i.FromStatus,
                ToStatus = i.ToStatus,
                ReviewNo = i.ReviewNo,
                ReviewStatus = i.ReviewStatus,
            })
            .ToList();

    public ContentReferenceList GetReferences(long contentId)
    {
        ContentReferencesResponse response = ApiTaskCall.Run(
            () => api.GetContentReferencesAsync(contentId, ApiTaskCall.BearerOf(session)), session);
        return new ContentReferenceList(response.ContentId, response.CanManage,
            response.References.Select(ToModel).ToList());
    }

    public ContentReference AddReference(long contentId, string displayName, string resourceUrl)
    {
        var request = new CreateContentReferenceRequest(displayName, resourceUrl);
        ContentReferenceResponse response = ApiTaskCall.Run(
            () => api.AddContentReferenceAsync(contentId, request, ApiTaskCall.BearerOf(session)), session);
        return ToModel(response);
    }

    public void DeleteReference(long contentId, long referenceId) =>
        ApiTaskCall.Run(() => api.DeleteContentReferenceAsync(contentId, referenceId, ApiTaskCall.BearerOf(session)), session);

    public ContentReferenceList SaveReferenceChanges(long contentId, IReadOnlyList<ContentReferenceAddition> additions,
        IReadOnlyList<long> deletedReferenceIds)
    {
        var request = new SaveContentReferenceChangesRequest(
            additions.Select(item => new CreateContentReferenceRequest(item.DisplayName, item.ResourceUrl)).ToList(),
            deletedReferenceIds);
        ContentReferencesResponse response = ApiTaskCall.Run(
            () => api.SaveContentReferenceChangesAsync(contentId, request, ApiTaskCall.BearerOf(session)), session);
        return new ContentReferenceList(response.ContentId, response.CanManage,
            response.References.Select(ToModel).ToList());
    }

    public void ChangeStatus(long contentId, ContentStatus next) =>
        ApiTaskCall.Run(() => api.ChangeContentStatusAsync(contentId, new ChangeContentStatusRequest(next.ToString()), ApiTaskCall.BearerOf(session)), session);

    public void SubmitForReview(long contentId) =>
        ApiTaskCall.Run(() => api.SubmitReviewAsync(contentId, new SubmitReviewRequest(), ApiTaskCall.BearerOf(session)), session);

    public void DecideReview(long contentId, bool approve, string? feedback) =>
        ApiTaskCall.Run(() => api.DecideReviewAsync(contentId, new ReviewDecisionRequest(approve, feedback), ApiTaskCall.BearerOf(session)), session);

    private static ScriptDocument ToDocument(ScriptResponse r) => new()
    {
        ContentId = r.ContentId,
        ProjectId = r.ProjectId,
        Text = r.Text,
        Version = r.Version,
        Access = r.CanEdit ? ScriptAccess.Editable
            : r.IsContentLocked ? ScriptAccess.Locked(r.ReadOnlyReason ?? string.Empty)
            : ScriptAccess.Denied(r.ReadOnlyReason ?? string.Empty),
        Statistics = ScriptTextAnalyzer.Analyze(r.Text),
    };

    private static ContentReference ToModel(ContentReferenceResponse r) => new(
        r.ReferenceId, r.ContentId, r.DisplayName, r.ResourceUrl, r.AddedBy, r.CreatedAt.ToLocalTime());
}
