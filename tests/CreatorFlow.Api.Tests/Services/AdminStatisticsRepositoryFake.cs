using CreatorFlow.Api.Models.Admin;
using CreatorFlow.Api.Repositories.Admin;

namespace CreatorFlow.Api.Tests.Services;

internal sealed class AdminStatisticsRepositoryFake : IAdminStatisticsRepository
{
    public AdminStatisticsRecord Result { get; set; } = new();
    public Exception? Failure { get; set; }

    public Task<AdminStatisticsRecord> GetAsync(CancellationToken cancellationToken = default)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Result);
    }
}
