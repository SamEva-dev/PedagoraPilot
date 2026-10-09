using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Training.Cohorts;

public sealed class DeleteCohortCommandHandler(
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    IEnrollmentRepository enrollments,
    ITrainingSessionRepository sessions,
    IAttendanceSheetRepository attendance,
    IDistanceLearningSessionRepository distanceSessions,
    IAsyncLearningModuleRepository asyncModules,
    IWorkplacePeriodRepository workplacePeriods,
    IDocumentRepository documents,
    ICertificationExamSessionRepository examSessions,
    ICurrentUser current) : IRequestHandler<DeleteCohortCommand, bool>
{
    public async Task<bool> Handle(DeleteCohortCommand request, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);

        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        ContextualScope.EnsureCanManageCohort(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value);

        if (cohort.Status is not CohortStatus.Planned and not CohortStatus.Draft)
            throw new ConflictApplicationException(ErrorKeys.CohortDeleteStatusForbidden);

        var inUse =
            await enrollments.Query(false).AnyAsync(x => x.CohortId == cohort.Id, ct)
            || await sessions.Query(false).AnyAsync(x => x.CohortId == cohort.Id, ct)
            || await attendance.Query(false).AnyAsync(x => x.CohortId == cohort.Id, ct)
            || await distanceSessions.Query(false).AnyAsync(x => x.CohortId == cohort.Id, ct)
            || await asyncModules.Query(false).AnyAsync(x => x.CohortId == cohort.Id, ct)
            || await workplacePeriods.Query(false).AnyAsync(x => x.CohortId == cohort.Id, ct)
            || await documents.Query(false).AnyAsync(x => x.CohortId == cohort.Id.Value, ct)
            || await examSessions.Query(false).AnyAsync(x => x.CohortId == cohort.Id, ct);

        if (inUse)
            throw new ConflictApplicationException(ErrorKeys.CohortDeleteInUse);

        cohort.MarkDeleted();
        cohorts.Remove(cohort);
        return true;
    }
}
