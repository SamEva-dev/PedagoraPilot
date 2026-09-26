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
        CancellationToken ct,
        bool manage = false)
    {
        var organizationId = TenantScope.Organization(user);
        if (!organizationId.HasValue) return;

        var candidates = await (from version in versions.Query(false)
                                  join referential in referentials.Query(false) on version.ReferentialId equals referential.Id
                                  join offering in offerings.Query(false) on referential.ProgramId equals offering.ProgramId
                                  join site in sites.Query(false) on offering.SiteId equals site.Id
                                  where version.Id == versionId && offering.IsActive && site.OrganizationId == organizationId.Value
                                  select new { SiteId = site.Id, ProgramId = offering.ProgramId }).ToListAsync(ct);
        var allowed = manage
            ? candidates.Any(x => user.CanManageProgram(x.SiteId, x.ProgramId))
            : candidates.Any(x => user.CanViewProgram(x.SiteId, x.ProgramId));
        if (!allowed)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}
