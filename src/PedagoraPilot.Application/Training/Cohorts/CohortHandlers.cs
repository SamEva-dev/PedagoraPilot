using PedagoraPilot.Application.Abstractions.Security;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Training.Cohorts;
public sealed class CreateCohortCommandHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, ITrainingSiteRepository sites, IReferentialVersionRepository referentialVersions, IReferentialRepository referentials, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<CreateCohortCommand, CohortDto>
{
    public async Task<CohortDto> Handle(CreateCohortCommand request, CancellationToken ct)
    {
        var offering = await offerings.GetByIdAsync(request.ProgramOfferingId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        var site = await sites.GetByIdAsync(offering.SiteId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
        TenantScope.Ensure(current, site.OrganizationId);
        ContextualScope.EnsureCanManageProgram(current, site.Id, offering.ProgramId);
        if (!offering.IsActive)
            throw new ConflictApplicationException(ErrorKeys.ProgramOfferingInactive);
        var referentialVersion = await referentialVersions.GetByIdAsync(request.ReferentialVersionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialVersionNotFound);
        if (referentialVersion.Status != ReferentialVersionStatus.Active)
            throw new ConflictApplicationException(ErrorKeys.ReferentialVersionNotActive);
        var referential = await referentials.GetByIdAsync(referentialVersion.ReferentialId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialNotFound);
        if (referential.ProgramId != offering.ProgramId)
            throw new ConflictApplicationException(ErrorKeys.CohortReferentialProgramMismatch);
        if (await cohorts.CodeExistsAsync(site.OrganizationId, request.Code, null, ct))
            throw new ConflictApplicationException(ErrorKeys.CohortCodeAlreadyExists);
        var cohort = Cohort.Create(site.OrganizationId, site.Id, offering.Id, referentialVersion.Id, request.Code, request.Name, request.StartDate, request.EndDate, request.Capacity, request.ExternalKey);
        await cohorts.AddAsync(cohort, ct);
        return ToDto(cohort, 0, mapper);
    }

    internal static CohortDto ToDto(Cohort cohort, int learnerCount, IObjectMapper mapper)
    {
        var model = mapper.Map<Cohort, CohortReadModel>(cohort);
        return new CohortDto(model.Id.Value, model.ExternalKey ?? model.Id.Value.ToString(), model.OrganizationId, model.SiteId, model.ProgramOfferingId, model.ReferentialVersionId, model.Code, model.Name, model.StartDate, model.EndDate, model.Capacity, learnerCount, model.Status.ToString().ToLowerInvariant());
    }
}

public sealed class UpdateCohortCommandHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<UpdateCohortCommand, CohortDto>
{
    public async Task<CohortDto> Handle(UpdateCohortCommand request, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);
        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        ContextualScope.EnsureCanManageCohort(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value);
        if (!Enum.TryParse<CohortStatus>(request.Status, true, out var status))
            throw new ValidationApplicationException(ErrorKeys.CohortStatusInvalid);
        var learnerCount = await enrollments.CountActiveByCohortAsync(cohort.Id, ct);
        if (request.Capacity < learnerCount)
            throw new ConflictApplicationException(ErrorKeys.CohortCapacityBelowEnrollmentCount);
        cohort.Update(request.Name, request.StartDate, request.EndDate, request.Capacity, status);
        return CreateCohortCommandHandler.ToDto(cohort, learnerCount, mapper);
    }
}

public sealed class GetCohortsQueryHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetCohortsQuery, IReadOnlyCollection<CohortDto>>
{
    public async Task<IReadOnlyCollection<CohortDto>> Handle(GetCohortsQuery request, CancellationToken ct)
    {
        var query = cohorts.Query(false);
        var organizationId = TenantScope.Organization(current);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (request.OrganizationId.HasValue)
            TenantScope.Ensure(current, request.OrganizationId.Value);
        if (request.OrganizationId.HasValue)
            query = query.Where(x => x.OrganizationId == request.OrganizationId.Value);
        if (request.SiteId.HasValue)
            query = query.Where(x => x.SiteId == request.SiteId.Value);
        if (request.ProgramId.HasValue)
        {
            var offeringIds = await offerings.Query(false).Where(x => x.ProgramId == request.ProgramId.Value).Select(x => x.Id).ToArrayAsync(ct);
            query = query.Where(x => offeringIds.Contains(x.ProgramOfferingId));
        }

        var items = await query.OrderByDescending(x => x.StartDate).ThenBy(x => x.Name).ToListAsync(ct);
        if (items.Count == 0) return Array.Empty<CohortDto>();

        if (current.HasContextualScopeRestrictions)
        {
            var offeringIdsForScope = items.Select(x => x.ProgramOfferingId).Distinct().ToArray();
            var programByOffering = await offerings.Query(false)
                .Where(x => offeringIdsForScope.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.ProgramId, ct);
            items = items.Where(x => programByOffering.TryGetValue(x.ProgramOfferingId, out var programId)
                && current.CanViewCohort(x.SiteId, programId, x.Id.Value)).ToList();
            if (items.Count == 0) return Array.Empty<CohortDto>();
        }

        var ids = items.Select(x => x.Id).ToArray();
        var counts = await enrollments.Query(false)
            .Where(x => ids.Contains(x.CohortId) && x.Status == EnrollmentStatus.Active)
            .GroupBy(x => x.CohortId)
            .Select(group => new { CohortId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.CohortId, x => x.Count, ct);
        return items.Select(cohort => CreateCohortCommandHandler.ToDto(
            cohort, counts.GetValueOrDefault(cohort.Id), mapper)).ToArray();
    }
}
