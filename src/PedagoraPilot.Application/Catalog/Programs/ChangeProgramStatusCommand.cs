using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Training;
using Microsoft.EntityFrameworkCore;

namespace PedagoraPilot.Application.Catalog.Programs;

public sealed record ChangeProgramStatusCommand(Guid ProgramId, string Status) : ICommand<TrainingProgramDto>;

public sealed class ChangeProgramStatusCommandHandler(
    ITrainingProgramRepository programs,
    IOrganizationMemberRepository members,
    IProgramFamilyRepository families,
    IProgramOfferingRepository offerings,
    ICohortRepository cohorts,
    IReferentialRepository referentials,
    IReferentialVersionRepository versions,
    ICurrentUser current) : IRequestHandler<ChangeProgramStatusCommand, TrainingProgramDto>
{
    public async Task<TrainingProgramDto> Handle(ChangeProgramStatusCommand request, CancellationToken ct)
    {
        var program = await programs.GetByIdAsync(request.ProgramId, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        if (program.OrganizationId.HasValue) TenantScope.Ensure(current, program.OrganizationId.Value);
        else TenantScope.RequirePlatform(current);
        if (!Enum.TryParse<ProgramStatus>(request.Status, true, out var target) ||
            !Enum.IsDefined(target))
            throw new ConflictApplicationException("PROGRAM_STATUS_INVALID");
        if (target == ProgramStatus.Draft && program.Status != ProgramStatus.Draft)
            throw new ConflictApplicationException("PROGRAM_STATUS_TRANSITION_INVALID");
        if (target == ProgramStatus.Inactive && program.Status == ProgramStatus.Active)
        {
            var offeringIds = await offerings.Query(false).Where(x => x.ProgramId == program.Id).Select(x => x.Id).ToArrayAsync(ct);
            if (offeringIds.Length > 0 && await cohorts.Query(false).AnyAsync(c => offeringIds.Contains(c.ProgramOfferingId)
                && c.Status != CohortStatus.Completed && c.Status != CohortStatus.Cancelled, ct))
                throw new ConflictApplicationException("PROGRAM_DEACTIVATION_ACTIVE_COHORTS");
        }
        if (target == ProgramStatus.Active && program.Status != ProgramStatus.Active)
        {
            if (!program.ResponsibleTrainerAuthGateUserId.HasValue || !program.OrganizationId.HasValue)
                throw new ConflictApplicationException("PROGRAM_ACTIVATION_RESPONSIBLE_REQUIRED");
            var userId = program.ResponsibleTrainerAuthGateUserId.Value;
            var member = await members.GetByAuthGateUserAsync(program.OrganizationId.Value, userId, false, ct);
            var roles = await members.GetRoleCodesAsync(program.OrganizationId.Value, userId, ct);
            if (member is null || !member.IsActive || !roles.Any(x => x.Equals("PedagoraPilot.PedagogicalManager", StringComparison.OrdinalIgnoreCase)))
                throw new ConflictApplicationException("PROGRAM_ACTIVATION_RESPONSIBLE_REQUIRED");
            var hasActiveVersion = await referentials.Query(false)
                .Where(x => x.ProgramId == program.Id)
                .Join(versions.Query(false), r => r.Id, v => v.ReferentialId, (r, v) => v)
                .AnyAsync(v => v.Status == ReferentialVersionStatus.Active, ct);
            if (!hasActiveVersion)
                throw new ConflictApplicationException("PROGRAM_ACTIVATION_REFERENTIAL_REQUIRED");
        }
        if (target != program.Status) program.ChangeStatus(target);
        var family = await families.GetByIdAsync(program.FamilyId, false, ct);
        var siteKeys = await offerings.Query(false).Where(x => x.ProgramId == program.Id && x.IsActive)
            .Select(x => x.ExternalKey ?? x.SiteId.ToString()).ToArrayAsync(ct);
        var referential = await referentials.Query(false).FirstOrDefaultAsync(x => x.ProgramId == program.Id, ct);
        var activeVersion = referential is null ? null : await versions.Query(false)
            .Where(x => x.ReferentialId == referential.Id && x.Status == ReferentialVersionStatus.Active)
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        return ProgramDtoFactory.Create(program, family?.Code ?? "OTHER", siteKeys,
            activeVersion is null ? null : ReferentialVersionText.Display(activeVersion));
    }
}
