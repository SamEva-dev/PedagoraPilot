using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Application.Organizations.Access;

public sealed record ValidateAccessScopeAssignmentQuery(
    string Scope,
    Guid? SiteId,
    Guid? ProgramId,
    Guid? CohortId,
    Guid? ExamSessionId) : IQuery<bool>;

public sealed class ValidateAccessScopeAssignmentQueryHandler(
    ITrainingSiteRepository sites,
    IProgramOfferingRepository offerings,
    ICohortRepository cohorts,
    ICertificationExamSessionRepository exams,
    ICurrentUser current) : IRequestHandler<ValidateAccessScopeAssignmentQuery, bool>
{
    public async Task<bool> Handle(ValidateAccessScopeAssignmentQuery request, CancellationToken ct)
    {
        _ = TenantScope.Organization(current); // fail closed when org_id is missing
        if (string.IsNullOrWhiteSpace(request.Scope))
            return false;
        var scope = request.Scope.Trim().ToLowerInvariant();

        if (scope == "organization")
            return request.SiteId is null && request.ProgramId is null && request.CohortId is null && request.ExamSessionId is null;

        if (!request.SiteId.HasValue)
            return false;

        var site = await sites.GetByIdAsync(request.SiteId.Value, false, ct);
        if (site is null)
            return false;
        TenantScope.Ensure(current, site.OrganizationId);

        if (scope == "site")
            return request.ProgramId is null && request.CohortId is null && request.ExamSessionId is null;

        if (!request.ProgramId.HasValue)
            return false;

        // A cohort is the strongest source of truth for a cohort-scoped assignment.
        // Resolve its actual ProgramOffering first, then compare the caller-supplied
        // site/program IDs with that canonical relationship. This prevents false
        // invalid_scope_assignment_target errors caused by stale UI combinations.
        if (scope is "cohort" or "exam")
        {
            if (!request.CohortId.HasValue)
                return false;

            var cohortId = new PedagoraPilot.Domain.Identifiers.CohortId(request.CohortId.Value);
            var cohort = await cohorts.GetByIdAsync(cohortId, false, ct);
            if (cohort is null)
                return false;
            TenantScope.Ensure(current, cohort.OrganizationId);

            var cohortOffering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct);
            if (cohortOffering is null || !cohortOffering.IsActive)
                return false;

            if (cohort.SiteId != site.Id || cohortOffering.SiteId != site.Id || cohortOffering.ProgramId != request.ProgramId.Value)
                return false;

            if (scope == "cohort")
                return request.ExamSessionId is null;

            if (!request.ExamSessionId.HasValue)
                return false;

            var examId = new PedagoraPilot.Domain.Identifiers.CertificationExamSessionId(request.ExamSessionId.Value);
            var exam = await exams.GetByIdAsync(examId, false, ct);
            if (exam is null)
                return false;
            TenantScope.Ensure(current, exam.OrganizationId);

            return exam.SiteId == site.Id && exam.CohortId == cohort.Id;
        }

        var offering = await offerings.Query(false).FirstOrDefaultAsync(x =>
            x.SiteId == site.Id && x.ProgramId == request.ProgramId.Value && x.IsActive, ct);
        if (offering is null)
            return false;

        if (scope == "program")
            return request.CohortId is null && request.ExamSessionId is null;

        return false;
    }
}
