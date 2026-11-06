using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Workplace;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Workplace;

internal static class ExistingTutorMembership
{
    internal static async Task<OrganizationMember?> ResolveAsync(
        IOrganizationMemberRepository members, IWorkplaceTutorAssignmentRepository assignments,
        Guid organizationId, string normalizedEmail, CancellationToken ct)
    {
        var member = await members.Query(false).SingleOrDefaultAsync(x =>
            x.OrganizationId == organizationId && x.Email == normalizedEmail, ct);
        if (member is null) return null;
        if (!member.IsActive || member.AuthGateUserId == Guid.Empty)
            throw new ConflictApplicationException("WORKPLACE_TUTOR_MEMBER_INACTIVE");
        var roles = await members.GetRoleCodesAsync(organizationId, member.AuthGateUserId, ct);
        var allowed = roles.Any(x => x is
            PedagoraPilotRoleCodes.Trainer or PedagoraPilotRoleCodes.PedagogicalManager or
            PedagoraPilotRoleCodes.OrganizationAdministrator or PedagoraPilotRoleCodes.OrganizationDirection or
            PedagoraPilotRoleCodes.SiteDirection)
            || (await assignments.GetActivePeriodIdsAsync(organizationId, member.AuthGateUserId, ct)).Count > 0;
        if (!allowed)
        {
            // No locally indexed tutor profile exists. An account with no other
            // locally indexed role may still be a prior InternshipTutor. Route it
            // through verified invitation acceptance rather than trusting email.
            if (roles.Count == 0) return null;
            throw new ConflictApplicationException("WORKPLACE_TUTOR_EXISTING_ROLE_INCOMPATIBLE");
        }
        return member;
    }
}
