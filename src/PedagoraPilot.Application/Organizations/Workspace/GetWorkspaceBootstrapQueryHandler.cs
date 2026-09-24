using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Catalog.Programs;
using PedagoraPilot.Application.Training.Cohorts;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Contracts.Workspace;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Application.Organizations.Workspace;

public sealed class GetWorkspaceBootstrapQueryHandler(
    IOrganizationRepository organizations,
    ITrainingSiteRepository sites,
    ITrainingProgramRepository programs,
    IProgramFamilyRepository families,
    IProgramOfferingRepository offerings,
    IReferentialRepository referentials,
    IReferentialVersionRepository versions,
    ICohortRepository cohorts,
    IEnrollmentRepository enrollments,
    IObjectMapper mapper,
    ICurrentUser currentUser)
    : IRequestHandler<GetWorkspaceBootstrapQuery, WorkspaceBootstrapDto>
{
    public async Task<WorkspaceBootstrapDto> Handle(
        GetWorkspaceBootstrapQuery _,
        CancellationToken ct)
    {
        var orgQuery = organizations.Query(false);

        // Real AuthGate users are tenant-scoped by the signed org_id claim.
        // Platform users have no organization claim and keep cross-tenant visibility.
        if (currentUser.OrganizationId is Guid organizationId)
            orgQuery = orgQuery.Where(x => x.Id == organizationId);

        var orgs = await orgQuery
            .OrderBy(x => x.LegalName)
            .ToListAsync(ct);

        var organizationIds = orgs.Select(x => x.Id).ToArray();

        var ss = organizationIds.Length == 0
            ? []
            : await sites.Query(false)
                .Where(x => x.IsActive && organizationIds.Contains(x.OrganizationId))
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

        var siteIds = ss.Select(x => x.Id).ToArray();

        var oo = siteIds.Length == 0
            ? []
            : await offerings.Query(false)
                .Where(x => x.IsActive && siteIds.Contains(x.SiteId))
                .ToListAsync(ct);

        var visibleProgramIds = oo.Select(x => x.ProgramId).Distinct().ToArray();

        var pp = visibleProgramIds.Length == 0
            ? []
            : await programs.Query(false)
                .Where(x => visibleProgramIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

        var caps = await programs.GetCapabilitiesAsync(pp.Select(x => x.Id), ct);

        var ff = pp.Count == 0
            ? new Dictionary<Guid, string>()
            : await families.Query(false)
                .Where(x => pp.Select(p => p.FamilyId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Code, ct);

        var rr = visibleProgramIds.Length == 0
            ? []
            : await referentials.Query(false)
                .Where(x => visibleProgramIds.Contains(x.ProgramId))
                .ToListAsync(ct);

        var referentialIds = rr.Select(x => x.Id).ToArray();

        var vv = referentialIds.Length == 0
            ? []
            : await versions.Query(false)
                .Where(x =>
                    referentialIds.Contains(x.ReferentialId) &&
                    x.Status == ReferentialVersionStatus.Active)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync(ct);

        var cc = organizationIds.Length == 0
            ? []
            : await cohorts.Query(false)
                .Where(x => organizationIds.Contains(x.OrganizationId))
                .OrderByDescending(x => x.StartDate)
                .ToListAsync(ct);

        var orgDtos = orgs.Select(x =>
            new WorkspaceOrganizationDto(
                x.Id,
                $"org-{x.Code.Value.ToLowerInvariant()}",
                x.Code.Value,
                x.LegalName,
                x.LegalName,
                string.Empty,
                x.Status == Domain.Organizations.OrganizationStatus.Active,
                string.Empty,
                string.Empty))
            .ToArray();

        var siteDtos = ss.Select(x =>
        {
            var o = orgs.First(y => y.Id == x.OrganizationId);
            return new WorkspaceSiteDto(
                x.Id,
                x.ExternalKey ?? x.Id.ToString(),
                x.OrganizationId,
                $"org-{o.Code.Value.ToLowerInvariant()}",
                x.Code.Value,
                x.Name,
                x.City,
                x.IsActive);
        }).ToArray();

        var programDtos = pp.Select(p =>
        {
            var siteKeys = oo
                .Where(o => o.ProgramId == p.Id)
                .Select(o =>
                    ss.FirstOrDefault(s => s.Id == o.SiteId)?.ExternalKey
                    ?? o.SiteId.ToString())
                .ToArray();

            var rf = rr.FirstOrDefault(x => x.ProgramId == p.Id);
            var rv = rf is null
                ? null
                : vv.FirstOrDefault(x => x.ReferentialId == rf.Id);

            return ProgramDtoFactory.Create(
                p,
                ff.GetValueOrDefault(p.FamilyId, "OTHER"),
                siteKeys,
                rv?.VersionLabel,
                caps.GetValueOrDefault(p.Id, Array.Empty<string>()));
        }).ToArray();

        var offeringDtos = oo.Select(o =>
        {
            var s = ss.First(x => x.Id == o.SiteId);
            var p = pp.First(x => x.Id == o.ProgramId);

            return new ProgramOfferingDto(
                o.Id,
                o.Id.ToString(),
                o.SiteId,
                s.ExternalKey ?? s.Id.ToString(),
                o.ProgramId,
                p.ExternalKey ?? p.Id.ToString(),
                o.IsActive);
        }).ToArray();

        var cohortDtos = new List<PedagoraPilot.Contracts.Training.CohortDto>(cc.Count);
        foreach (var cohort in cc)
        {
            var count = await enrollments.CountActiveByCohortAsync(cohort.Id, ct);
            cohortDtos.Add(CreateCohortCommandHandler.ToDto(cohort, count, mapper));
        }

        var firstOrg = orgs.FirstOrDefault();
        var firstSite = firstOrg is null
            ? null
            : ss.FirstOrDefault(x => x.OrganizationId == firstOrg.Id);
        var firstOffer = firstSite is null
            ? null
            : oo.FirstOrDefault(x => x.SiteId == firstSite.Id);
        var firstCohort = firstOffer is null
            ? null
            : cc.FirstOrDefault(x => x.ProgramOfferingId == firstOffer.Id);

        return new WorkspaceBootstrapDto(
            orgDtos,
            siteDtos,
            programDtos,
            offeringDtos,
            cohortDtos,
            firstOrg is null
                ? null
                : new WorkspaceSelectionDto(
                    firstOrg.Id,
                    firstSite?.Id,
                    firstOffer?.ProgramId,
                    firstCohort?.Id.Value));
    }
}
