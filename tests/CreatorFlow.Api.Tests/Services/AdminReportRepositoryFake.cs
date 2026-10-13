using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Repositories.Admin;

namespace CreatorFlow.Api.Tests.Services;

internal sealed class AdminReportRepositoryFake : IAdminReportRepository
{
    public List<AdminReportRecord> Rows { get; } = new();
    public int TotalCount { get; set; }
    public AdminReportCreateResult? CreateResult { get; set; } = new(false, 7);
    public AdminReportHandleResult? HandleResult { get; set; } =
        new(ReportStatus.Open, "a@example.test", "b@example.test");
    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<AdminReportRecord>> SearchAsync(string? search, string? status, int limit, int offset,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult<IReadOnlyList<AdminReportRecord>>(Rows);
    }

    public Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(TotalCount);
    }

    public Task<AdminReportCreateResult?> CreateAsync(long reporterUserId, long reportedUserId,
        string reason, string? details, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(CreateResult);
    }

    public Task<AdminReportHandleResult?> HandleAsync(long reportId, ReportStatus status, string? note, long actorUserId,
        string traceId, CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(HandleResult);
    }
}
