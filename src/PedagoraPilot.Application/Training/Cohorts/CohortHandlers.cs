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
public sealed class CreateCohortCommandHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, ITrainingSiteRepository sites, IReferentialVersionRepository referentialVersions, IReferentialRepository referentials, IObjectMapper mapper) : IRequestHandler<CreateCohortCommand, CohortDto>
{
    public async Task<CohortDto> Handle(CreateCohortCommand request, CancellationToken ct)
    {
        var offering = await offerings.GetByIdAsync(request.ProgramOfferingId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        if (!offering.IsActive)
            throw new ConflictApplicationException(ErrorKeys.ProgramOfferingInactive);
        var site = await sites.GetByIdAsync(offering.SiteId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
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

public sealed class UpdateCohortCommandHandler(ICohortRepository cohorts, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<UpdateCohortCommand, CohortDto>
{
    public async Task<CohortDto> Handle(UpdateCohortCommand request, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (!Enum.TryParse<CohortStatus>(request.Status, true, out var status))
            throw new ValidationApplicationException(ErrorKeys.CohortStatusInvalid);
        var learnerCount = await enrollments.CountActiveByCohortAsync(cohort.Id, ct);
        if (request.Capacity < learnerCount)
            throw new ConflictApplicationException(ErrorKeys.CohortCapacityBelowEnrollmentCount);
        cohort.Update(request.Name, request.StartDate, request.EndDate, request.Capacity, status);
        return CreateCohortCommandHandler.ToDto(cohort, learnerCount, mapper);
    }
}

public sealed class GetCohortsQueryHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<GetCohortsQuery, IReadOnlyCollection<CohortDto>>
{
    public async Task<IReadOnlyCollection<CohortDto>> Handle(GetCohortsQuery request, CancellationToken ct)
    {
        var query = cohorts.Query(false);
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
        var result = new List<CohortDto>(items.Count);
        foreach (var cohort in items)
        {
            var count = await enrollments.CountActiveByCohortAsync(cohort.Id, ct);
            result.Add(CreateCohortCommandHandler.ToDto(cohort, count, mapper));
        }

        return result;
    }
}
