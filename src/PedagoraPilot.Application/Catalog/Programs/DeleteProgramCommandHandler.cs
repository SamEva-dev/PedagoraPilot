using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Catalog.Programs;

public sealed class DeleteProgramCommandHandler(
    ITrainingProgramRepository programs,
    IProgramOfferingRepository offerings,
    ICohortRepository cohorts,
    IReferentialRepository referentials,
    IReferentialVersionRepository versions,
    ICompetencyDefinitionRepository competencies,
    IPedagogicalTopicRepository topics,
    ICertificationSchemeRepository certificationSchemes,
    IWorkplaceActivityDefinitionRepository workplaceActivities,
    IWorkplaceDocumentRequirementRepository workplaceDocuments,
    IWorkplacePeriodRepository workplacePeriods,
    IDistanceLearningSessionRepository distanceSessions,
    IAsyncLearningModuleRepository asyncModules,
    ICurrentUser current) : IRequestHandler<DeleteProgramCommand, bool>
{
    public async Task<bool> Handle(DeleteProgramCommand request, CancellationToken ct)
    {
        var program = await programs.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);

        if (program.OrganizationId.HasValue)
            TenantScope.Ensure(current, program.OrganizationId.Value);
        else
            TenantScope.RequirePlatform(current);

        // A context-restricted user must not be able to delete an organization-wide catalogue item.
        if (current.HasContextualScopeRestrictions)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        var programOfferings = await offerings.Query(true)
            .Where(x => x.ProgramId == program.Id)
            .ToListAsync(ct);
        var offeringIds = programOfferings.Select(x => x.Id).ToArray();

        if ((offeringIds.Length > 0 && await cohorts.Query(false).AnyAsync(x => offeringIds.Contains(x.ProgramOfferingId), ct))
            || await distanceSessions.Query(false).AnyAsync(x => x.ProgramId == program.Id, ct)
            || await asyncModules.Query(false).AnyAsync(x => x.ProgramId == program.Id, ct))
            throw new ConflictApplicationException(ErrorKeys.ProgramDeleteInUse);

        var programReferentials = await referentials.Query(true)
            .Where(x => x.ProgramId == program.Id)
            .ToListAsync(ct);
        var referentialIds = programReferentials.Select(x => x.Id).ToArray();
        var programVersions = referentialIds.Length == 0
            ? new List<PedagoraPilot.Domain.Catalog.ReferentialVersion>()
            : await versions.Query(true).Where(x => referentialIds.Contains(x.ReferentialId)).ToListAsync(ct);
        var versionIds = programVersions.Select(x => x.Id).ToArray();

        if (versionIds.Length > 0)
        {
            var referentialInUse =
                await cohorts.Query(false).AnyAsync(x => versionIds.Contains(x.ReferentialVersionId), ct)
                || await competencies.Query(false).AnyAsync(x => versionIds.Contains(x.ReferentialVersionId), ct)
                || await topics.Query(false).AnyAsync(x => versionIds.Contains(x.ReferentialVersionId), ct)
                || await certificationSchemes.Query(false).AnyAsync(x => versionIds.Contains(x.ReferentialVersionId), ct)
                || await workplaceActivities.Query(false).AnyAsync(x => versionIds.Contains(x.ReferentialVersionId), ct)
                || await workplaceDocuments.Query(false).AnyAsync(x => versionIds.Contains(x.ReferentialVersionId), ct)
                || await workplacePeriods.Query(false).AnyAsync(x => versionIds.Contains(x.ReferentialVersionId), ct);

            if (referentialInUse)
                throw new ConflictApplicationException(ErrorKeys.ProgramDeleteReferentialInUse);
        }

        foreach (var offering in programOfferings) offerings.Remove(offering);
        foreach (var version in programVersions) versions.Remove(version);
        foreach (var referential in programReferentials) referentials.Remove(referential);

        program.MarkDeleted();
        programs.Remove(program);
        return true;
    }
}
