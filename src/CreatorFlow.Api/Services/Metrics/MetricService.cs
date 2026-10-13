using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Models.Metrics;
using CreatorFlow.Api.Models.Tasks;
using CreatorFlow.Api.Repositories.Metrics;
using CreatorFlow.Api.Repositories.Tasks;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Contracts.Metrics;

namespace CreatorFlow.Api.Services.Metrics;

/// <summary>Đọc: thành viên active của Project. Ghi: Owner/Manager, chỉ khi MetricRules.IsEligible. Người ghi lấy từ JWT.</summary>
public sealed class MetricService(IMetricRepository metrics, ITaskRepository tasks, CurrentAuthenticatedUser current)
{
    public async Task<AuthOperationResult<ContentMetricsResponse>> GetContentMetricsAsync(long contentId, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<ContentMetricsResponse>();
        ContentInfo? content = await tasks.GetContentAsync(contentId, token);
        if (content is null) return NotFound<ContentMetricsResponse>();
        ProjectRole? role = await tasks.GetRoleAsync(content.ProjectId, user.UserId, token);
        if (role is null) return NotMember<ContentMetricsResponse>();

        var platforms = await metrics.GetPlatformsAsync(contentId, token);
        var latest = (await metrics.GetLatestAsync(contentId, token)).ToDictionary(s => s.ContentPlatformId);
        return new(new ContentMetricsResponse(contentId,
            platforms.Select(p => ToResponse(p, role, latest.GetValueOrDefault(p.ContentPlatformId), null)).ToList()));
    }

    public async Task<AuthOperationResult<PlatformMetricsResponse>> GetPlatformMetricsAsync(
        long contentId, long platformId, bool history, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<PlatformMetricsResponse>();
        ContentInfo? content = await tasks.GetContentAsync(contentId, token);
        if (content is null) return NotFound<PlatformMetricsResponse>();
        ProjectRole? role = await tasks.GetRoleAsync(content.ProjectId, user.UserId, token);
        if (role is null) return NotMember<PlatformMetricsResponse>();

        PlatformTarget? target = (await metrics.GetPlatformsAsync(contentId, token)).FirstOrDefault(p => p.PlatformId == platformId);
        if (target is null) return NotFound<PlatformMetricsResponse>("Content này không đăng lên platform đó.");
        var rows = await metrics.GetHistoryAsync(target.ContentPlatformId, token);
        return new(ToResponse(target, role, rows.FirstOrDefault(), history ? rows : null));
    }

    public async Task<AuthOperationResult<PlatformMetricsResponse>> RecordAsync(
        long contentId, long platformId, RecordMetricRequest request, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<PlatformMetricsResponse>();
        if (MetricRules.Validate(request) is { } error)
            return AuthOperationResult<PlatformMetricsResponse>.Fail("validation", error, "metrics");

        ContentInfo? content = await tasks.GetContentAsync(contentId, token);
        if (content is null) return NotFound<PlatformMetricsResponse>();
        ProjectRole? role = await tasks.GetRoleAsync(content.ProjectId, user.UserId, token);
        if (role is null) return NotMember<PlatformMetricsResponse>();
        if (!MetricRules.CanRecord(role))
            return AuthOperationResult<PlatformMetricsResponse>.Fail("forbidden", "Chỉ Owner/Manager mới được nhập metrics.", status: 403);

        PlatformTarget? target = (await metrics.GetPlatformsAsync(contentId, token)).FirstOrDefault(p => p.PlatformId == platformId);
        if (target is null) return NotFound<PlatformMetricsResponse>("Content này không đăng lên platform đó.");
        if (!MetricRules.IsEligible(target))
            return AuthOperationResult<PlatformMetricsResponse>.Fail("content_locked", "Chỉ nhập metrics sau khi đã Published.", status: 409);

        MetricSnapshot saved = await metrics.AddSnapshotAsync(
            new NewMetric(target.ContentPlatformId, request.Views, request.Likes, request.Comments, request.Shares, user.UserId), token);
        return new(ToResponse(target, role, saved, null));
    }

    private static PlatformMetricsResponse ToResponse(PlatformTarget p, ProjectRole? role, MetricSnapshot? latest, IReadOnlyList<MetricSnapshot>? history) => new(
        p.ContentPlatformId, p.PlatformId, p.PlatformName, p.PublicationStatus,
        MetricRules.CanRecord(role) && MetricRules.IsEligible(p),
        latest is null ? null : ToResponse(latest), history?.Select(ToResponse).ToList());

    private static MetricSnapshotResponse ToResponse(MetricSnapshot s) => new(
        s.MetricId, s.Views, s.Likes, s.Comments, s.Shares, s.RecordedAt, s.RecordedBy, s.RecordedByName);

    private static AuthOperationResult<T> Unauthorized<T>() =>
        AuthOperationResult<T>.Fail("unauthorized", "Vui lòng đăng nhập lại.", status: 401);
    private static AuthOperationResult<T> NotMember<T>() =>
        AuthOperationResult<T>.Fail("forbidden", "Bạn không thuộc Project này.", status: 403);
    private static AuthOperationResult<T> NotFound<T>(string message = "Không tìm thấy Content.") =>
        AuthOperationResult<T>.Fail("not_found", message, status: 404);
}
