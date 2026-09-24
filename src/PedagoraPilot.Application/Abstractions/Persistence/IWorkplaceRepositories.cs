using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IWorkplacePeriodRepository : IRepository<WorkplacePeriod, WorkplacePeriodId>
{
    Task<bool> OverlapsAsync(EnrollmentId enrollmentId, DateOnly startDate, DateOnly endDate, WorkplacePeriodId? exceptId = null, CancellationToken cancellationToken = default);
}

public interface IWorkplaceActivityDefinitionRepository : IRepository<WorkplaceActivityDefinition, WorkplaceActivityDefinitionId>
{
}

public interface IWorkplaceDocumentRequirementRepository : IRepository<WorkplaceDocumentRequirement, WorkplaceDocumentRequirementId>
{
}
