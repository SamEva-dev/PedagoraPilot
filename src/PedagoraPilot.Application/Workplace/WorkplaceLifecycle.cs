using DomainRelay.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Application.Workplace;

public sealed record ChangeWorkplaceLifecycleCommand(Guid PeriodId, string Action, string? Reason) : ICommand<bool>;
public sealed class ChangeWorkplaceLifecycleCommandValidator : AbstractValidator<ChangeWorkplaceLifecycleCommand>
{
    public ChangeWorkplaceLifecycleCommandValidator()
    {
        RuleFor(x => x.PeriodId).NotEmpty();
        RuleFor(x => x.Action).Must(x => x is "cancel" or "archive" or "delete");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).When(x => x.Action is "cancel" or "delete");
    }
}

// Transactional: lifecycle, tutor revocation and cancellation of orphan invitations
// are committed in the same UoW; outbox events are persisted with the domain change.
public sealed class ChangeWorkplaceLifecycleCommandHandler(
    IWorkplacePeriodRepository periods,
    IWorkplaceTutorAssignmentRepository assignments,
    IAccessInvitationRepository invitations,
    ICurrentUser current,
    WorkplaceContextualAccess access)
    : IRequestHandler<ChangeWorkplaceLifecycleCommand, bool>
{
    public async Task<bool> Handle(ChangeWorkplaceLifecycleCommand request, CancellationToken ct)
    {
        if (current.UserId is not Guid actor || actor == Guid.Empty)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        var periodId = new WorkplacePeriodId(request.PeriodId);
        // Same advisory lock used by tutor reassignment: serializes assignment changes
        // with cancellation, even when multiple invitations share one tutor.
        var period = await periods.Query(true).Include(x => x.Activities).Include(x => x.Documents)
            .Include(x => x.Evaluations).SingleOrDefaultAsync(x => x.Id == periodId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
        TenantScope.Ensure(current, period.OrganizationId);
        await access.EnsureAsync(period, manage: true, ct);
        await invitations.AcquireCreationLockAsync(period.OrganizationId, $"workplace-period:{request.PeriodId:N}", ct);
        // Refresh inside the serialized transaction; the first load may be stale.
        // A concurrent reassignment takes the same lock before committing.
        switch (request.Action)
        {
            case "cancel": period.Cancel(request.Reason ?? "", actor); break;
            case "archive": period.Archive(actor); break;
            case "delete": period.DeleteUnused(request.Reason ?? "", actor); break;
            default: throw new ValidationApplicationException("WORKPLACE_LIFECYCLE_INVALID");
        }
        var links = await assignments.GetByPeriodAsync(period.OrganizationId, period.Id.Value, ct);
        foreach (var link in links.Where(x => x.Status != WorkplaceTutorAssignmentStatus.Revoked)) link.Revoke();
        foreach (var invitationId in links.Where(x => x.InvitationId.HasValue).Select(x => x.InvitationId!.Value).Distinct())
        {
            var associated = await assignments.GetByInvitationAsync(period.OrganizationId, invitationId, ct);
            if (associated.All(x => x.Status != WorkplaceTutorAssignmentStatus.Pending))
            {
                var invite = await invitations.GetByIdAsync(new AccessInvitationId(invitationId), true, ct);
                if (invite?.Status == AccessInvitationStatus.Pending) invite.Cancel();
            }
        }
        return true;
    }
}
