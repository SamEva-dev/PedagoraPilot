using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Application.Training.Delivery;

internal static class StudentSessionAccess
{
    internal static async Task<Enrollment[]> EnrollmentsAsync(
        ICurrentUser current, ILearnerProfileRepository profiles, IPersonRepository people,
        IEnrollmentRepository enrollments, CancellationToken ct)
    {
        if (!LearnerSelfAccess.Applies(current)) return Array.Empty<Enrollment>();

        var profile = current.UserId.HasValue
            ? await profiles.Query(false).FirstOrDefaultAsync(x => x.AuthGateUserId == current.UserId.Value, ct)
            : null;
        if (profile is null && !string.IsNullOrWhiteSpace(current.Email))
        {
            var person = await people.GetByEmailAsync(current.Email, false, ct);
            if (person is not null)
                profile = await profiles.GetByPersonIdAsync(person.Id, false, ct);
        }

        if (profile is null || (profile.AuthGateUserId.HasValue && profile.AuthGateUserId != current.UserId))
            return Array.Empty<Enrollment>();

        var organizationId = TenantScope.Organization(current);
        return await enrollments.Query(false)
            .Where(x => x.LearnerProfileId == profile.Id
                && (!organizationId.HasValue || x.OrganizationId == organizationId.Value))
            .ToArrayAsync(ct);
    }

    internal static EnrollmentId EnsureParticipant(TrainingSession session, IReadOnlyCollection<Enrollment> own)
    {
        var enrollment = own.FirstOrDefault(x => IsParticipant(session, x));
        return enrollment?.Id ?? throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }

    internal static bool IsParticipant(TrainingSession session, Enrollment enrollment)
    {
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(session.StartsAtUtc,
            TimeZoneInfo.FindSystemTimeZoneById(session.TimeZoneId)).DateTime);
        return enrollment.CohortId == session.CohortId
            && enrollment.EnrolledOn <= date
            && (!enrollment.EndedOn.HasValue || date <= enrollment.EndedOn.Value)
            && (session.AudienceMode == SessionAudienceMode.WholeCohort
                || session.Participants.Any(p => p.EnrollmentId == enrollment.Id));
    }

    internal static TrainingSessionDto Redact(TrainingSessionDto dto, EnrollmentId self) =>
        dto with {
            ParticipantEnrollmentIds = dto.ParticipantEnrollmentIds.Where(id => id == self.Value).ToArray(),
            Comments = null,
            ExternalKey = null
        };
}
