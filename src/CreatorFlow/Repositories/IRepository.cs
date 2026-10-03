namespace CreatorFlow.Repositories;

public interface IRepository<TEntity, in TId>
{
    Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
