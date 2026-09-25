using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Training.Learners;

public static class LearnerSelfAccess
{
    public static bool Applies(ICurrentUser current) => current.Roles.Any(role =>
        string.Equals(role, "PedagoraPilot.Student", StringComparison.OrdinalIgnoreCase));

    public static async Task EnsureAsync(Enrollment enrollment, ILearnerProfileRepository profiles,
        IPersonRepository people, ICurrentUser current, CancellationToken ct)
    {
        if (!Applies(current)) return;
        var profile = await profiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct)
            ?? throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        if (profile.AuthGateUserId.HasValue)
        {
            if (profile.AuthGateUserId == current.UserId) return;
        }
        else if (!string.IsNullOrWhiteSpace(current.Email))
        {
            var person = await people.GetByIdAsync(profile.PersonId, false, ct);
            if (string.Equals(person?.Email, current.Email, StringComparison.OrdinalIgnoreCase)) return;
        }
        throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}
