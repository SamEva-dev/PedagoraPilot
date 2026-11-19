using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IDeaReferentialStructureRepository : IRepository<DeaReferentialStructure, DeaReferentialStructureId>
{
    Task<DeaReferentialStructure?> ByVersionAsync(Guid versionId, bool isTracking = false, CancellationToken ct = default);
}
