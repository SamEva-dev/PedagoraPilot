using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Audit;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class AuditEntryRepository(PedagoraPilotDbContext db) : Repository<AuditEntry, AuditEntryId>(db), IAuditEntryRepository
{
    public IQueryable<AuditEntry> QueryByOrganization(Guid organizationId, bool isTracking = false) => Query(isTracking).Where(x => x.OrganizationId == organizationId);
}
