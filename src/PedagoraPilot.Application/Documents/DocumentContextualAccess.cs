using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Documents;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Documents;

public sealed class DocumentContextualAccess(
    IProgramOfferingRepository offerings,
    ICohortRepository cohorts,
    ICurrentUser current)
{
    public Task EnsureCanReadAsync(ManagedDocument document, CancellationToken ct) =>
        EnsureAsync(document.SiteId, document.ProgramId, document.CohortId, manage: false, ct);

    public async Task<bool> CanReadAsync(ManagedDocument document, CancellationToken ct)
    {
        try
        {
            await EnsureCanReadAsync(document, ct);
            return true;
        }
        catch (ForbiddenApplicationException)
        {
            return false;
        }
    }

    public Task EnsureCanManageAsync(ManagedDocument document, CancellationToken ct) =>
        EnsureAsync(document.SiteId, document.ProgramId, document.CohortId, manage: true, ct);

    public Task EnsureCanManageTargetAsync(Guid? siteId, Guid? programId, Guid? cohortId, CancellationToken ct) =>
        EnsureAsync(siteId, programId, cohortId, manage: true, ct);

    private async Task EnsureAsync(Guid? siteId, Guid? programId, Guid? cohortId, bool manage, CancellationToken ct)
    {
        if (!current.HasContextualScopeRestrictions)
            return;

        if (cohortId.HasValue)
        {
            var cohort = await cohorts.GetByIdAsync(new CohortId(cohortId.Value), false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
            var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
            if (manage)
                ContextualScope.EnsureCanManageCohort(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value);
            else
                ContextualScope.EnsureCanViewCohort(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value);
            return;
        }

        if (siteId.HasValue && programId.HasValue)
        {
            if (manage)
                ContextualScope.EnsureCanManageProgram(current, siteId.Value, programId.Value);
            else
                ContextualScope.EnsureCanViewProgram(current, siteId.Value, programId.Value);
            return;
        }

        if (programId.HasValue)
        {
            var candidateSites = await offerings.Query(false)
                .Where(x => x.ProgramId == programId.Value && x.IsActive)
                .Select(x => x.SiteId)
                .Distinct()
                .ToArrayAsync(ct);
            var allowed = candidateSites.Any(id => manage
                ? current.CanManageProgram(id, programId.Value)
                : current.CanViewProgram(id, programId.Value));
            if (!allowed)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            return;
        }

        if (siteId.HasValue)
        {
            if (manage)
                ContextualScope.EnsureCanManageSite(current, siteId.Value);
            else
                ContextualScope.EnsureCanViewSite(current, siteId.Value);
            return;
        }

        // Organization-level documents may be read by every scoped member, but
        // only organization-wide users may create/change/delete them.
        if (manage)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}
