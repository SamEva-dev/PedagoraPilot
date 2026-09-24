using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class ProgramFamilyRepository(PedagoraPilotDbContext db) : Repository<ProgramFamily>(db), IProgramFamilyRepository
{
    public Task<ProgramFamily?> GetByCodeAsync(string code, bool isTracking = false, CancellationToken ct = default) => Query(isTracking).SingleOrDefaultAsync(x => x.Code == code.Trim().ToUpperInvariant(), ct);
}

public sealed class TrainingProgramRepository(PedagoraPilotDbContext db) : Repository<TrainingProgram>(db), ITrainingProgramRepository
{
    public Task<TrainingProgram?> GetByCodeAsync(string code, bool isTracking = false, CancellationToken ct = default) => Query(isTracking).SingleOrDefaultAsync(x => x.Code.Value == code.Trim().ToUpperInvariant(), ct);
    public Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default) => Query(false).AnyAsync(x => x.Code.Value == code.Trim().ToUpperInvariant() && (!exceptId.HasValue || x.Id != exceptId.Value), ct);
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetCapabilitiesAsync(IEnumerable<Guid> programIds, CancellationToken ct = default)
    {
        var ids = programIds.Distinct().ToArray();
        var rows = await DbContext.ProgramCapabilities.AsNoTracking().Where(x => ids.Contains(x.ProgramId)).ToListAsync(ct);
        return rows.GroupBy(x => x.ProgramId).ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(x => x.CapabilityCode).ToArray());
    }
}

public sealed class ProgramOfferingRepository(PedagoraPilotDbContext db) : Repository<ProgramOffering>(db), IProgramOfferingRepository
{
    public Task<ProgramOffering?> GetAsync(Guid siteId, Guid programId, bool isTracking = false, CancellationToken ct = default) => Query(isTracking).SingleOrDefaultAsync(x => x.SiteId == siteId && x.ProgramId == programId, ct);
}

public sealed class ReferentialRepository(PedagoraPilotDbContext db) : Repository<Referential>(db), IReferentialRepository
{
    public Task<Referential?> GetByProgramAndCodeAsync(Guid programId, string code, bool isTracking = false, CancellationToken ct = default) => Query(isTracking).SingleOrDefaultAsync(x => x.ProgramId == programId && x.Code == code.Trim().ToUpperInvariant(), ct);
}

public sealed class ReferentialVersionRepository(PedagoraPilotDbContext db) : Repository<ReferentialVersion>(db), IReferentialVersionRepository
{
    public Task<bool> VersionExistsAsync(Guid referentialId, string versionLabel, CancellationToken ct = default) => Query(false).AnyAsync(x => x.ReferentialId == referentialId && x.VersionLabel == versionLabel, ct);
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetCapabilitiesAsync(IEnumerable<Guid> versionIds, CancellationToken ct = default)
    {
        var ids = versionIds.Distinct().ToArray();
        var rows = await DbContext.ReferentialVersionCapabilities.AsNoTracking().Where(x => ids.Contains(x.ReferentialVersionId)).ToListAsync(ct);
        return rows.GroupBy(x => x.ReferentialVersionId).ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(x => x.CapabilityCode).ToArray());
    }
}
