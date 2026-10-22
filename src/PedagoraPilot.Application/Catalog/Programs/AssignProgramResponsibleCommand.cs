using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace PedagoraPilot.Application.Catalog.Programs;

/// <summary>Only this command changes the single responsible trainer of a program.</summary>
public sealed record AssignProgramResponsibleCommand(Guid ProgramId, Guid AuthGateUserId) : ICommand<TrainingProgramDto>;

public sealed class AssignProgramResponsibleCommandHandler(
    ITrainingProgramRepository programs,
    IOrganizationMemberRepository members,
    IProgramFamilyRepository families,
    IProgramOfferingRepository offerings,
    IReferentialRepository referentials,
    IReferentialVersionRepository versions,
    ICurrentUser current) : IRequestHandler<AssignProgramResponsibleCommand, TrainingProgramDto>
{
    public async Task<TrainingProgramDto> Handle(AssignProgramResponsibleCommand request, CancellationToken ct)
    {
        if (request.AuthGateUserId == Guid.Empty)
            throw new ConflictApplicationException("PROGRAM_RESPONSIBLE_TRAINER_INVALID");
        var program = await programs.GetByIdAsync(request.ProgramId, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        if (!program.OrganizationId.HasValue)
            throw new ConflictApplicationException("PROGRAM_RESPONSIBLE_ORGANIZATION_REQUIRED");
        TenantScope.Ensure(current, program.OrganizationId.Value);
        var member = await members.GetByAuthGateUserAsync(program.OrganizationId.Value, request.AuthGateUserId, false, ct);
        if (member is null || !member.IsActive)
            throw new ConflictApplicationException("PROGRAM_RESPONSIBLE_MEMBER_REQUIRED");
        var managerRoles = await members.GetRoleCodesAsync(program.OrganizationId.Value, request.AuthGateUserId, ct);
        if (!managerRoles.Any(x => x.Equals("PedagoraPilot.PedagogicalManager", StringComparison.OrdinalIgnoreCase)))
            throw new ConflictApplicationException("PROGRAM_RESPONSIBLE_TRAINER_REQUIRED");
        program.AssignResponsibleTrainer(request.AuthGateUserId, member.DisplayName);
        var family = await families.GetByIdAsync(program.FamilyId, false, ct);
        var siteKeys = await offerings.Query(false).Where(x => x.ProgramId == program.Id && x.IsActive)
            .Select(x => x.SiteId.ToString()).ToArrayAsync(ct);
        var referential = await referentials.Query(false).FirstOrDefaultAsync(x => x.ProgramId == program.Id, ct);
        var activeVersion = referential is null ? null : await versions.Query(false)
            .Where(x => x.ReferentialId == referential.Id && x.Status == ReferentialVersionStatus.Active)
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        return ProgramDtoFactory.Create(program, family?.Code ?? "OTHER", siteKeys,
            activeVersion is null ? null : ReferentialVersionText.Display(activeVersion));
    }
}
