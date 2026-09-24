using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Training;

namespace PedagoraPilot.Application.Training.Cohorts;
public sealed record GetCohortsQuery(Guid? OrganizationId = null, Guid? SiteId = null, Guid? ProgramId = null) : IQuery<IReadOnlyCollection<CohortDto>>;
