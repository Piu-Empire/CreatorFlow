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

    public async Task<AuthOperationResult<MetricsSummaryResponse>> GetSummaryAsync(
        long projectId, string? groupBy, CancellationToken token = default)
    {
        if (current.User is not { } user) return Unauthorized<MetricsSummaryResponse>();
        if (!TryParseGroupBy(groupBy, out MetricGroupBy dimension))
            return AuthOperationResult<MetricsSummaryResponse>.Fail("validation",
                "groupBy phải là content, creator, platform hoặc project.", "groupBy");

        ProjectRole? role = await tasks.GetRoleAsync(projectId, user.UserId, token);
        if (role is null) return NotMember<MetricsSummaryResponse>();

        var summaryRows = await metrics.GetProjectSummaryAsync(projectId, dimension, token);
        var response = new MetricsSummaryResponse(projectId, DimensionText(dimension),
            summaryRows.Select(r => new MetricsSummaryRow(r.Key, r.Name, r.Views, r.Likes, r.Comments, r.Shares,
                MetricEngagement.Percent(r.Views, r.Likes, r.Comments, r.Shares))).ToList());
        return new(response);
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

        // AC3b: nếu giá trị sau khi gộp trùng y nguyên snapshot mới nhất thì không tạo dòng trùng.
        // (Kiểm tra ở service cho dễ test; khe hở đọc–ghi nhỏ, chấp nhận cho POC.)
        MetricSnapshot? latest = (await metrics.GetHistoryAsync(target.ContentPlatformId, token)).FirstOrDefault();
        if (latest is not null && IsUnchanged(latest, request))
            return new(ToResponse(target, role, latest, null));

        MetricSnapshot saved = await metrics.AddSnapshotAsync(
            new NewMetric(target.ContentPlatformId, request.Views, request.Likes, request.Comments, request.Shares, user.UserId), token);
        return new(ToResponse(target, role, saved, null));
    }

    /// <summary>true nếu request (field null = giữ nguyên) không đổi so với snapshot <paramref name="latest"/>.</summary>
    private static bool IsUnchanged(MetricSnapshot latest, RecordMetricRequest request) =>
        (request.Views ?? latest.Views) == latest.Views &&
        (request.Likes ?? latest.Likes) == latest.Likes &&
        (request.Comments ?? latest.Comments) == latest.Comments &&
        (request.Shares ?? latest.Shares) == latest.Shares;

    private static bool TryParseGroupBy(string? value, out MetricGroupBy groupBy)
    {
        groupBy = MetricGroupBy.Content;
        if (string.IsNullOrWhiteSpace(value)) return true;
        switch (value.Trim().ToLowerInvariant())
        {
            case "project": groupBy = MetricGroupBy.Project; return true;
            case "content": groupBy = MetricGroupBy.Content; return true;
            case "creator": groupBy = MetricGroupBy.Creator; return true;
            case "platform": groupBy = MetricGroupBy.Platform; return true;
            default: return false;
        }
    }

    private static string DimensionText(MetricGroupBy groupBy) => groupBy switch
    {
        MetricGroupBy.Project => "project",
        MetricGroupBy.Creator => "creator",
        MetricGroupBy.Platform => "platform",
        _ => "content",
    };

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
