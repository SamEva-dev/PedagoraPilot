using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;

namespace PedagoraPilot.Application.Abstractions.Security;

public static class TenantCatalogAccess
{
    public static async Task EnsureReferentialVersionAsync(
        ICurrentUser user, Guid versionId,
        IReferentialVersionRepository versions, IReferentialRepository referentials,
        IProgramOfferingRepository offerings, ITrainingSiteRepository sites,
        ITrainingProgramRepository programs, CancellationToken ct,
        bool manage = false)
    {
        var organizationId = TenantScope.Organization(user);
        if (!organizationId.HasValue) return;

        // Tenant-owned programs are already visible in GetReferentialsQuery, including before
        // any offering exists. A tenant-wide user may therefore read/manage their referential
        // (subject to the endpoint permission), even without an active site offering.
        // A site/program/cohort/exam-restricted user must still follow an authorized offering.
        if (!user.HasContextualScopeRestrictions)
        {
            var ownsProgram = await (from version in versions.Query(false)
                                     join referential in referentials.Query(false) on version.ReferentialId equals referential.Id
                                     join program in programs.Query(false) on referential.ProgramId equals program.Id
                                     where version.Id == versionId && program.OrganizationId == organizationId.Value
                                     select program.Id).AnyAsync(ct);
            if (ownsProgram)
                return;
        }

        var candidates = await (from version in versions.Query(false)
                                join referential in referentials.Query(false) on version.ReferentialId equals referential.Id
                                join offering in offerings.Query(false) on referential.ProgramId equals offering.ProgramId
                                join program in programs.Query(false) on offering.ProgramId equals program.Id
                                join site in sites.Query(false) on offering.SiteId equals site.Id
                                where version.Id == versionId
                                      && offering.IsActive
                                      && site.OrganizationId == organizationId.Value
                                      && (!program.OrganizationId.HasValue || program.OrganizationId == organizationId.Value)
                                select new { SiteId = site.Id, ProgramId = offering.ProgramId }).ToListAsync(ct);
        var allowed = manage
            ? candidates.Any(x => user.CanManageProgram(x.SiteId, x.ProgramId))
            : candidates.Any(x => user.CanViewProgram(x.SiteId, x.ProgramId));
        if (!allowed)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}
