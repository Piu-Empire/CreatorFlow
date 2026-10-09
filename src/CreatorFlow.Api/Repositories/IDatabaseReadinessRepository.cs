namespace CreatorFlow.Repositories;

public interface IDatabaseReadinessRepository
{
    Task ProbeAsync(CancellationToken cancellationToken);
}
