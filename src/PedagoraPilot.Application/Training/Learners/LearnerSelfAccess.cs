using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Application.Training.Learners;

public static class LearnerSelfAccess
{
    public static bool Applies(ICurrentUser current) => current.Roles.Any(role =>
        string.Equals(role, "PedagoraPilot.Student", StringComparison.OrdinalIgnoreCase));

    public static async Task<LearnerProfile?> ResolveProfileAsync(
        ILearnerProfileRepository profiles,
        IPersonRepository people,
        ICurrentUser current,
        CancellationToken ct)
    {
        LearnerProfile? profile = null;

        if (current.UserId.HasValue)
            profile = await profiles.Query(false)
                .FirstOrDefaultAsync(x => x.AuthGateUserId == current.UserId.Value, ct);

        if (profile is null && !string.IsNullOrWhiteSpace(current.Email))
        {
            var person = await people.GetByEmailAsync(current.Email, false, ct);
            if (person is not null)
                profile = await profiles.GetByPersonIdAsync(person.Id, false, ct);
        }

        // The email fallback is only valid while the learner profile is not linked
        // to another AuthGate identity. This prevents an email match from crossing identities.
        if (profile?.AuthGateUserId.HasValue == true && profile.AuthGateUserId != current.UserId)
            return null;

        return profile;
    }

    public static async Task EnsureAsync(Enrollment enrollment, ILearnerProfileRepository profiles,
        IPersonRepository people, ICurrentUser current, CancellationToken ct)
    {
        if (!Applies(current)) return;

        var self = await ResolveProfileAsync(profiles, people, current, ct);
        if (self is not null && self.Id == enrollment.LearnerProfileId) return;

        throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}
