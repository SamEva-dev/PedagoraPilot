using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IProgramFamilyRepository : IRepository<ProgramFamily>
{
    Task<ProgramFamily?> GetByCodeAsync(string code, bool isTracking = false, CancellationToken ct = default);
}

public interface ITrainingProgramRepository : IRepository<TrainingProgram>
{
    Task<TrainingProgram?> GetByCodeAsync(string code, bool isTracking = false, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetCapabilitiesAsync(IEnumerable<Guid> programIds, CancellationToken ct = default);
}

public interface IProgramOfferingRepository : IRepository<ProgramOffering>
{
    Task<ProgramOffering?> GetAsync(Guid siteId, Guid programId, bool isTracking = false, CancellationToken ct = default);
}

public interface IReferentialRepository : IRepository<Referential>
{
    Task<Referential?> GetByProgramAndCodeAsync(Guid programId, string code, bool isTracking = false, CancellationToken ct = default);
}

public interface IReferentialVersionRepository : IRepository<ReferentialVersion>
{
    Task<bool> VersionExistsAsync(Guid referentialId, string versionLabel, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetCapabilitiesAsync(IEnumerable<Guid> versionIds, CancellationToken ct = default);
}
