using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Application.Learning.Progress;

public sealed class GetCohortCompetenciesQueryHandler(
    ICohortRepository cohorts, IProgramOfferingRepository offerings, IEnrollmentRepository enrollments,
    ILearnerProfileRepository profiles, IPersonRepository people,
    ICompetencyDefinitionRepository definitions, ILearnerCompetencyRecordRepository records,
    IObjectMapper mapper, ICurrentUser current)
    : IRequestHandler<GetCohortCompetenciesQuery, IReadOnlyCollection<CohortCompetencyRowDto>>
{
    public async Task<IReadOnlyCollection<CohortCompetencyRowDto>> Handle(GetCohortCompetenciesQuery request, CancellationToken ct)
    {
        if (LearnerSelfAccess.Applies(current))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var cohort = await cohorts.GetByIdAsync(request.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);
        await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);

        var members = await enrollments.Query(false)
            .Where(x => x.CohortId == cohort.Id && x.OrganizationId == cohort.OrganizationId)
            .OrderBy(x => x.EnrolledOn).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.LearnerProfileId }).ToListAsync(ct);
        if (members.Count == 0) return Array.Empty<CohortCompetencyRowDto>();

        var profileIds = members.Select(x => x.LearnerProfileId).Distinct().ToArray();
        var profileRows = await profiles.Query(false).Where(x => profileIds.Contains(x.Id))
            .Select(x => new { x.Id, x.PersonId }).ToListAsync(ct);
        var personIds = profileRows.Select(x => x.PersonId).Distinct().ToArray();
        var personRows = await people.Query(false).Where(x => personIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName }).ToListAsync(ct);
        var personById = personRows.ToDictionary(x => x.Id);
        var profileById = profileRows.ToDictionary(x => x.Id);

        var defs = await definitions.Query(false)
            .Where(x => x.ReferentialVersionId == cohort.ReferentialVersionId && x.Active)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(ct);
        var memberIds = members.Select(x => x.Id).ToArray();
        var evaluations = await records.Query(false).Where(x => memberIds.Contains(x.EnrollmentId))
            .ToListAsync(ct);
        var evaluated = evaluations.ToDictionary(x => (x.EnrollmentId, x.CompetencyDefinitionId));

        return members.Select(member =>
        {
            var profile = profileById[member.LearnerProfileId];
            var person = personById[profile.PersonId];
            var rows = defs.Select(def =>
                evaluated.TryGetValue((member.Id, def.Id), out var record)
                    ? LearningProgressDtoFactory.Competency(record, def, mapper)
                    : new LearnerCompetencyDto(Guid.Empty, member.Id.Value, def.Id.Value,
                        def.Code, def.Title, LearningProgressDtoFactory.LevelCode(CompetencyLevel.NotAssessed),
                        null, null, null, null)).ToArray();
            return new CohortCompetencyRowDto(member.Id.Value, person.FirstName, person.LastName, rows);
        }).ToArray();
    }
}
