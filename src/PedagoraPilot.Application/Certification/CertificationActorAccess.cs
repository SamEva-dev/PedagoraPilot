using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Certification;

internal static class CertificationActorAccess
{
    public static bool IsStudent(ICurrentUser current) => current.Roles.Any(role =>
        string.Equals(role, PedagoraPilotRoleCodes.Student, StringComparison.OrdinalIgnoreCase));

    public static bool IsJury(ICurrentUser current) => current.Roles.Any(role =>
        string.Equals(role, PedagoraPilotRoleCodes.Jury, StringComparison.OrdinalIgnoreCase));

    public static async Task EnsureJuryAssignmentIfRequiredAsync(
        CertificationExamSession session,
        IJuryAssignmentRepository juries,
        ICurrentUser current,
        CancellationToken ct)
    {
        // Privileges are additive. A user who is also an organization administrator/direction
        // can use their certification management rights without being constrained by the jury role.
        if (!IsJury(current) || current.HasPermission(PedagoraPilotPermissionCodes.Certification.Manage))
            return;

        if (!current.UserId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        var assigned = await juries.Query(false).AnyAsync(x =>
            x.OrganizationId == session.OrganizationId
            && x.ExamSessionId == session.Id
            && x.AuthGateUserId == current.UserId.Value, ct);

        if (!assigned)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}
