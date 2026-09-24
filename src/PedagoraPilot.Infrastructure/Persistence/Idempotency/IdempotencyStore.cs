using Microsoft.EntityFrameworkCore;

namespace PedagoraPilot.Infrastructure.Persistence.Idempotency;
public sealed class IdempotencyStore
{
    private readonly PedagoraPilotDbContext _dbContext;
    public IdempotencyStore(PedagoraPilotDbContext dbContext) => _dbContext = dbContext;
    public Task<IdempotencyRequest?> FindAsync(string key, string scope, bool isTracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<IdempotencyRequest> query = _dbContext.IdempotencyRequests;
        if (!isTracking)
            query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Key == key && x.Scope == scope, cancellationToken);
    }

    public async Task AddProcessingAsync(IdempotencyRequest request, CancellationToken cancellationToken = default)
    {
        await _dbContext.IdempotencyRequests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(Guid id, int statusCode, string? contentType, string body, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.IdempotencyRequests.SingleAsync(x => x.Id == id, cancellationToken);
        request.Status = "Completed";
        request.ResponseStatusCode = statusCode;
        request.ResponseContentType = contentType;
        request.ResponseBody = body;
        request.CompletedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.IdempotencyRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null)
            return;
        _dbContext.IdempotencyRequests.Remove(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
