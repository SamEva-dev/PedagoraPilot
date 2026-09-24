using Microsoft.EntityFrameworkCore.Storage;
using PedagoraPilot.Application.Abstractions.Persistence;

namespace PedagoraPilot.Infrastructure.Persistence;
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PedagoraPilotDbContext _dbContext;
    public UnitOfWork(PedagoraPilotDbContext dbContext) => _dbContext = dbContext;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
            return await operation(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
