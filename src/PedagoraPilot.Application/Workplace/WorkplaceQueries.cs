using DomainRelay.Abstractions;
using PedagoraPilot.Contracts.Workplace;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Workplace;
public sealed record GetWorkplacePeriodsQuery(CohortId? CohortId, EnrollmentId? EnrollmentId) : IRequest<IReadOnlyCollection<WorkplacePeriodDto>>;
public sealed record GetWorkplacePeriodQuery(WorkplacePeriodId Id) : IRequest<WorkplacePeriodDto>;
public sealed record GetWorkplaceRequirementsQuery(Guid ReferentialVersionId, string PeriodTypeCode) : IRequest<IReadOnlyCollection<WorkplaceRequirementDto>>;
