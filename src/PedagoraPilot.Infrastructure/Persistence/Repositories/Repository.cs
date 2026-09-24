using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public class Repository<TEntity> : IRepository<TEntity> where TEntity : AggregateRoot
{
    protected readonly PedagoraPilotDbContext DbContext;
    protected readonly DbSet<TEntity> Set;
    public Repository(PedagoraPilotDbContext dbContext)
    {
        DbContext = dbContext;
        Set = dbContext.Set<TEntity>();
    }

    public virtual IQueryable<TEntity> Query(bool isTracking = false) => isTracking ? Set : Set.AsNoTracking();
    public virtual Task<TEntity?> GetByIdAsync(Guid id, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) => Set.AddAsync(entity, cancellationToken).AsTask();
    public void Update(TEntity entity) => Set.Update(entity);
    public void Remove(TEntity entity) => Set.Remove(entity);
}

public class Repository<TEntity, TId> : IRepository<TEntity, TId> where TEntity : AggregateRoot<TId> where TId : struct, IIdentifier
{
    protected readonly PedagoraPilotDbContext DbContext;
    protected readonly DbSet<TEntity> Set;
    public Repository(PedagoraPilotDbContext dbContext)
    {
        DbContext = dbContext;
        Set = dbContext.Set<TEntity>();
    }

    public virtual IQueryable<TEntity> Query(bool isTracking = false) => isTracking ? Set : Set.AsNoTracking();
    public virtual Task<TEntity?> GetByIdAsync(TId id, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).SingleOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) => Set.AddAsync(entity, cancellationToken).AsTask();
    public void Update(TEntity entity) => Set.Update(entity);
    public void Remove(TEntity entity) => Set.Remove(entity);
}
