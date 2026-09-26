using PedagoraPilot.Application.Abstractions.Security;
using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Certification;

namespace PedagoraPilot.Application.Catalog.Referentials;
public sealed record GetReferentialsQuery(Guid? ProgramId = null) : IRequest<IReadOnlyCollection<ReferentialVersionDto>>;
public sealed class GetReferentialsQueryHandler(IReferentialRepository referentials, IReferentialVersionRepository versions, ITrainingProgramRepository programs, IProgramOfferingRepository offerings, ITrainingSiteRepository sites, ICurrentUser current) : IRequestHandler<GetReferentialsQuery, IReadOnlyCollection<ReferentialVersionDto>>
{
    public async Task<IReadOnlyCollection<ReferentialVersionDto>> Handle(GetReferentialsQuery q, CancellationToken ct)
    {
        var organizationId = TenantScope.Organization(current);
        Guid[]? permittedProgramIds = null;
        if (organizationId.HasValue)
        {
            var paths = await (from offering in offerings.Query(false)
                               join site in sites.Query(false) on offering.SiteId equals site.Id
                               where offering.IsActive && site.OrganizationId == organizationId.Value
                               select new { SiteId = site.Id, offering.ProgramId }).ToArrayAsync(ct);
            permittedProgramIds = paths
                .Where(x => current.CanViewProgram(x.SiteId, x.ProgramId))
                .Select(x => x.ProgramId)
                .Distinct()
                .ToArray();
        }
        var rs = await referentials.Query(false)
            .Where(x => (!q.ProgramId.HasValue || x.ProgramId == q.ProgramId)
                && (permittedProgramIds == null || permittedProgramIds.Contains(x.ProgramId))).ToListAsync(ct);
        if (rs.Count == 0)
            return Array.Empty<ReferentialVersionDto>();
        var referentialsById = rs.ToDictionary(x => x.Id);
        var ids = rs.Select(x => x.Id).ToArray();
        var vs = await versions.Query(false).Where(x => ids.Contains(x.ReferentialId)).OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        if (vs.Count == 0)
            return Array.Empty<ReferentialVersionDto>();
        var caps = await versions.GetCapabilitiesAsync(vs.Select(x => x.Id), ct);
        var programIds = rs.Select(x => x.ProgramId).Distinct().ToArray();
        var ps = await programs.Query(false).Where(x => programIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return vs.Select(v =>
        {
            var referential = referentialsById[v.ReferentialId];
            return ToDto(v, referential, ps[referential.ProgramId], caps.GetValueOrDefault(v.Id, Array.Empty<string>()));
        }).ToArray();
    }

    internal static ReferentialVersionDto ToDto(ReferentialVersion v, Referential r, TrainingProgram p, IReadOnlyCollection<string>? capabilities = null) => new(v.Id, v.ExternalKey ?? v.Id.ToString(), r.Id, r.ProgramId, p.ExternalKey ?? p.Id.ToString(), r.Code, r.Name, v.VersionLabel, v.CertificationCode, v.Status.ToString().ToLowerInvariant(), v.EffectiveFrom, v.EffectiveTo, v.TotalHours, v.SheetCount, v.RequiredDocumentCount, capabilities ?? v.Capabilities, v.NotesKey);
}

public sealed class CreateReferentialVersionCommandHandler(IReferentialRepository refs, IReferentialVersionRepository versions, ITrainingProgramRepository programs, ICurrentUser current) : IRequestHandler<CreateReferentialVersionCommand, ReferentialVersionDto>
{
    public async Task<ReferentialVersionDto> Handle(CreateReferentialVersionCommand r, CancellationToken ct)
    {
        TenantScope.RequirePlatform(current);
        var rf = await refs.GetByIdAsync(r.ReferentialId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialNotFound);
        if (await versions.VersionExistsAsync(r.ReferentialId, r.Version, ct))
            throw new ConflictApplicationException(ErrorKeys.ReferentialVersionAlreadyExists);
        var p = await programs.GetByIdAsync(rf.ProgramId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var v = ReferentialVersion.CreateDraft(r.ReferentialId, r.Version, r.CertificationCode, r.EffectiveFrom, r.TotalHours, r.SheetCount, r.RequiredDocumentCount, r.EnabledModules, r.NotesKey, r.ExternalKey);
        if (r.Publish)
        {
            var actives = await versions.Query(true)
                .Where(x => x.ReferentialId == r.ReferentialId && x.Status == ReferentialVersionStatus.Active)
                .ToListAsync(ct);
            foreach (var old in actives)
                old.Archive(r.EffectiveFrom.AddDays(-1));
            v.Publish(DateTime.UtcNow);
        }
        await versions.AddAsync(v, ct);
        return GetReferentialsQueryHandler.ToDto(v, rf, p);
    }
}

public sealed class PublishReferentialVersionCommandHandler(IReferentialRepository refs, IReferentialVersionRepository versions, ITrainingProgramRepository programs, ICurrentUser current) : IRequestHandler<PublishReferentialVersionCommand, ReferentialVersionDto>
{
    public async Task<ReferentialVersionDto> Handle(PublishReferentialVersionCommand r, CancellationToken ct)
    {
        TenantScope.RequirePlatform(current);
        var v = await versions.GetByIdAsync(r.VersionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialVersionNotFound);
        var rf = await refs.GetByIdAsync(v.ReferentialId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialNotFound);
        var p = await programs.GetByIdAsync(rf.ProgramId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var actives = await versions.Query(true).Where(x => x.ReferentialId == v.ReferentialId && x.Status == ReferentialVersionStatus.Active && x.Id != v.Id).ToListAsync(ct);
        foreach (var old in actives)
            old.Archive(v.EffectiveFrom.AddDays(-1));
        v.Publish(DateTime.UtcNow);
        return GetReferentialsQueryHandler.ToDto(v, rf, p);
    }
}

public sealed record GetReferentialVersionDetailsQuery(Guid VersionId) : IRequest<ReferentialVersionDetailsDto>;

public sealed class GetReferentialVersionDetailsQueryHandler(
    IReferentialVersionRepository versions,
    IReferentialRepository referentials,
    IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites,
    ICompetencyDefinitionRepository competencies,
    IPedagogicalTopicRepository topics,
    IWorkplaceActivityDefinitionRepository workplaceActivities,
    IWorkplaceDocumentRequirementRepository workplaceDocuments,
    ICertificationSchemeRepository certificationSchemes,
    ICohortRepository cohorts,
    IEnrollmentRepository enrollments,
    ICurrentUser current) : IRequestHandler<GetReferentialVersionDetailsQuery, ReferentialVersionDetailsDto>
{
    public async Task<ReferentialVersionDetailsDto> Handle(GetReferentialVersionDetailsQuery request, CancellationToken ct)
    {
        var version = await versions.GetByIdAsync(request.VersionId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialVersionNotFound);
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, request.VersionId, versions, referentials, offerings, sites, ct);

        var definitions = await competencies.Query(false)
            .Where(x => x.ReferentialVersionId == request.VersionId && x.Active)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new ReferentialCompetencyDetailDto(
                x.Id.Value,
                x.ParentId.HasValue ? x.ParentId.Value.Value : null,
                x.Code,
                x.Title,
                x.Kind.ToString(),
                x.SortOrder))
            .ToArrayAsync(ct);

        var topicCount = await topics.Query(false)
            .CountAsync(x => x.ReferentialVersionId == request.VersionId && x.Active, ct);

        var activityTypes = workplaceActivities.Query(false)
            .Where(x => x.ReferentialVersionId == request.VersionId && x.Active)
            .Select(x => x.PeriodTypeCode);
        var documentTypes = workplaceDocuments.Query(false)
            .Where(x => x.ReferentialVersionId == request.VersionId && x.Active)
            .Select(x => x.PeriodTypeCode);
        var periodTypes = await activityTypes.Concat(documentTypes)
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(ct);

        var scheme = await certificationSchemes.Query(false)
            .Include(x => x.Steps)
            .Where(x => x.ReferentialVersionId == request.VersionId && x.Status != CertificationSchemeStatus.Archived)
            .OrderByDescending(x => x.Status == CertificationSchemeStatus.Published)
            .ThenByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        var steps = scheme?.Steps
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new ReferentialCertificationStepDetailDto(
                x.Id.Value, x.Code, x.Title, x.Kind.ToString(), x.DurationMinutes, x.SortOrder))
            .ToArray() ?? [];

        var organizationId = TenantScope.Organization(current);
        var cohortQuery = cohorts.Query(false).Where(x => x.ReferentialVersionId == request.VersionId);
        if (organizationId.HasValue)
            cohortQuery = cohortQuery.Where(x => x.OrganizationId == organizationId.Value);
        var cohortRows = await cohortQuery.OrderByDescending(x => x.StartDate).ToArrayAsync(ct);
        if (current.HasContextualScopeRestrictions && cohortRows.Length > 0)
        {
            var offeringIds = cohortRows.Select(x => x.ProgramOfferingId).Distinct().ToArray();
            var programByOffering = await offerings.Query(false)
                .Where(x => offeringIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.ProgramId, ct);
            cohortRows = cohortRows
                .Where(x => programByOffering.TryGetValue(x.ProgramOfferingId, out var programId)
                    && current.CanViewCohort(x.SiteId, programId, x.Id.Value))
                .ToArray();
        }
        var siteIds = cohortRows.Select(x => x.SiteId).Distinct().ToArray();
        var siteNames = siteIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await sites.Query(false).Where(x => siteIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var cohortIds = cohortRows.Select(x => x.Id).ToArray();
        var enrollmentCounts = cohortIds.Length == 0
            ? new Dictionary<PedagoraPilot.Domain.Identifiers.CohortId, int>()
            : await enrollments.Query(false)
                .Where(x => cohortIds.Contains(x.CohortId) && x.Status == PedagoraPilot.Domain.Training.EnrollmentStatus.Active)
                .GroupBy(x => x.CohortId)
                .Select(g => new { CohortId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CohortId, x => x.Count, ct);
        var linkedCohorts = cohortRows.Select(x => new ReferentialLinkedCohortDto(
            x.Id.Value,
            x.ExternalKey ?? x.Id.Value.ToString(),
            x.Name,
            siteNames.GetValueOrDefault(x.SiteId, string.Empty),
            x.StartDate,
            x.EndDate,
            enrollmentCounts.GetValueOrDefault(x.Id, 0),
            x.Status.ToString().ToLowerInvariant())).ToArray();

        return new ReferentialVersionDetailsDto(
            version.Id,
            topicCount,
            definitions,
            periodTypes,
            scheme?.Name ?? string.Empty,
            steps,
            linkedCohorts);
    }
}

