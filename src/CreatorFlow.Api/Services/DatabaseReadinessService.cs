using System.Data.Common;
using CreatorFlow.Repositories;

namespace CreatorFlow.Api.Services;

public sealed class DatabaseReadinessService(
    IDatabaseReadinessRepository repository,
    ILogger<DatabaseReadinessService> logger)
{
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var probe = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probe.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await repository.ProbeAsync(probe.Token).WaitAsync(probe.Token);
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Database readiness failed. FailureType: Timeout");
            return false;
        }
        catch (Exception exception) when (exception is DbException or TimeoutException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Exception messages can include connection details or SQL; log only the type.
            logger.LogWarning("Database readiness failed. FailureType: {FailureType}", exception.GetType().Name);
            return false;
        }
    }
}
