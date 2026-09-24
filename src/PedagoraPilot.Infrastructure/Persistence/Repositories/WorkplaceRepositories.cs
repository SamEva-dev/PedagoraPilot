using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class WorkplacePeriodRepository(PedagoraPilotDbContext db) : Repository<WorkplacePeriod, WorkplacePeriodId>(db), IWorkplacePeriodRepository
{
    public Task<bool> OverlapsAsync(EnrollmentId enrollmentId, DateOnly startDate, DateOnly endDate, WorkplacePeriodId? exceptId = null, CancellationToken cancellationToken = default) => Query(false).AnyAsync(x => x.EnrollmentId == enrollmentId && x.Status != WorkplacePeriodStatus.Cancelled && (!exceptId.HasValue || x.Id != exceptId.Value) && x.StartDate <= endDate && x.EndDate >= startDate, cancellationToken);
}

public sealed class WorkplaceActivityDefinitionRepository(PedagoraPilotDbContext db) : Repository<WorkplaceActivityDefinition, WorkplaceActivityDefinitionId>(db), IWorkplaceActivityDefinitionRepository;
public sealed class WorkplaceDocumentRequirementRepository(PedagoraPilotDbContext db) : Repository<WorkplaceDocumentRequirement, WorkplaceDocumentRequirementId>(db), IWorkplaceDocumentRequirementRepository;
