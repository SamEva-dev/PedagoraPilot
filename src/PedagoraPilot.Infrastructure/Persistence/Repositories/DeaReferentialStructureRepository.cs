using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class DeaReferentialStructureRepository(PedagoraPilotDbContext db)
    : Repository<DeaReferentialStructure, DeaReferentialStructureId>(db), IDeaReferentialStructureRepository
{
    public Task<DeaReferentialStructure?> ByVersionAsync(Guid versionId, bool isTracking = false, CancellationToken ct = default)
        => Query(isTracking).SingleOrDefaultAsync(x => x.ReferentialVersionId == versionId, ct);
}
