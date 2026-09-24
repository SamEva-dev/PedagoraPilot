using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IRepository<TEntity>
    where TEntity : AggregateRoot
{
    IQueryable<TEntity> Query(bool isTracking = false);
    Task<TEntity?> GetByIdAsync(Guid id, bool isTracking = false, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}

public interface IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId> where TId : struct, IIdentifier
{
    IQueryable<TEntity> Query(bool isTracking = false);
    Task<TEntity?> GetByIdAsync(TId id, bool isTracking = false, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
