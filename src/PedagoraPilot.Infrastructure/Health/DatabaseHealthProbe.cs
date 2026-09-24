using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Infrastructure.Persistence;

namespace PedagoraPilot.Infrastructure.Health;
public sealed class DatabaseHealthProbe
{
    private readonly IDbContextFactory<PedagoraPilotDbContext> _factory;
    public DatabaseHealthProbe(IDbContextFactory<PedagoraPilotDbContext> factory) => _factory = factory;
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Database.CanConnectAsync(cancellationToken);
    }
}
