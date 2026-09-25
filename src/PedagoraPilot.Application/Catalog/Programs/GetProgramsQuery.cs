using PedagoraPilot.Application.Abstractions.Security;
using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Contracts.Catalog;

namespace PedagoraPilot.Application.Catalog.Programs;
public sealed record GetProgramsQuery(Guid? OrganizationId = null) : IRequest<IReadOnlyCollection<TrainingProgramDto>>;
public sealed class GetProgramsQueryHandler(ITrainingProgramRepository programs, IProgramFamilyRepository families, IProgramOfferingRepository offerings, ITrainingSiteRepository sites, IReferentialVersionRepository versions, IReferentialRepository refs, ICurrentUser current) : IRequestHandler<GetProgramsQuery, IReadOnlyCollection<TrainingProgramDto>>
{
    public async Task<IReadOnlyCollection<TrainingProgramDto>> Handle(GetProgramsQuery r, CancellationToken ct)
    {
        var organizationId = TenantScope.Organization(current);
        if (r.OrganizationId.HasValue)
            TenantScope.Ensure(current, r.OrganizationId.Value);
        var effectiveOrganization = organizationId ?? r.OrganizationId;
        var ps = await programs.Query(false).OrderBy(x => x.Name).ToListAsync(ct);
        var caps = await programs.GetCapabilitiesAsync(ps.Select(x => x.Id), ct);
        var fs = await families.Query(false).ToDictionaryAsync(x => x.Id, x => x.Code, ct);
        var ss = await sites.Query(false)
            .Where(x => !effectiveOrganization.HasValue || x.OrganizationId == effectiveOrganization.Value)
            .ToDictionaryAsync(x => x.Id, ct);
        var siteIds = ss.Keys.ToArray();
        var os = await offerings.Query(false)
            .Where(x => x.IsActive && siteIds.Contains(x.SiteId)).ToListAsync(ct);
        var rs = await refs.Query(false).ToListAsync(ct);
        var vs = await versions.Query(false).Where(x => x.Status == Domain.Catalog.ReferentialVersionStatus.Active).OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        var result = new List<TrainingProgramDto>();
        foreach (var p in ps)
        {
            var siteKeys = os.Where(x => x.ProgramId == p.Id).Select(x => ss.TryGetValue(x.SiteId, out var s) ? s.ExternalKey ?? s.Id.ToString() : x.SiteId.ToString()).ToArray();
            if (effectiveOrganization.HasValue && !os.Any(x => x.ProgramId == p.Id))
                continue;
            var rid = rs.FirstOrDefault(x => x.ProgramId == p.Id)?.Id;
            var rv = rid.HasValue ? vs.FirstOrDefault(x => x.ReferentialId == rid.Value) : null;
            result.Add(ProgramDtoFactory.Create(p, fs.GetValueOrDefault(p.FamilyId, "OTHER"), siteKeys, rv?.VersionLabel, caps.GetValueOrDefault(p.Id, Array.Empty<string>())));
        }

        return result;
    }
}
