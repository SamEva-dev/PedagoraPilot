using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Access.Members;

/// <summary>
/// Keeps the product-side learner projection aligned with an organization member that holds
/// the Student role. Authentication remains owned by AuthGate; PedagoraPilot owns the learner
/// profile and optional cohort enrollment.
/// </summary>
public interface IStudentMemberProvisioner
{
    Task<LearnerProfileId> EnsureAsync(Guid organizationId, Guid authGateUserId, string email, string firstName,
        string lastName, Guid? cohortId = null, string? phone = null, DateOnly? birthDate = null, DateOnly? enrolledOn = null, CancellationToken ct = default);
}

public sealed class StudentMemberProvisioner(
    IPersonRepository persons,
    ILearnerProfileRepository learnerProfiles,
    ICohortRepository cohorts,
    IEnrollmentRepository enrollments) : IStudentMemberProvisioner
{
    public async Task<LearnerProfileId> EnsureAsync(Guid organizationId, Guid authGateUserId, string email,
        string firstName, string lastName, Guid? cohortId = null, string? phone = null, DateOnly? birthDate = null, DateOnly? enrolledOn = null, CancellationToken ct = default)
    {
        var person = await persons.GetByEmailAsync(email, true, ct);
        if (person is null)
        {
            person = Person.Create(firstName, lastName, email, phone, birthDate);
            await persons.AddAsync(person, ct);
        }
        // Delay contact updates until the AuthGate binding is verified. An email
        // collision must not mutate another identity's personal data.

        var learner = await learnerProfiles.GetByPersonIdAsync(person.Id, true, ct);
        if (learner is null)
        {
            learner = LearnerProfile.Create(person.Id, authGateUserId);
            await learnerProfiles.AddAsync(learner, ct);
        }
        else if (!learner.AuthGateUserId.HasValue)
        {
            learner.LinkAuthGateUser(authGateUserId);
        }
        else if (learner.AuthGateUserId.Value != authGateUserId)
        {
            throw new ConflictApplicationException("LEARNER_AUTHGATE_IDENTITY_CONFLICT");
        }

        if (person is not null)
            person.UpdateContact(firstName, lastName, email, phone ?? person.Phone, birthDate ?? person.BirthDate);

        if (!cohortId.HasValue) return learner.Id;
        var cohort = await cohorts.GetByIdAsync(new CohortId(cohortId.Value), false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.OrganizationId != organizationId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.CohortClosed);

        await cohorts.LockForEnrollmentCapacityAsync(cohort.Id, ct);
        cohort = await cohorts.GetByIdAsync(cohort.Id, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.CohortClosed);
        var existing = await enrollments.GetByLearnerAndCohortAsync(learner.Id, cohort.Id, true, ct);
        if (existing is not null)
        {
            if (existing.Status is EnrollmentStatus.Completed or EnrollmentStatus.Withdrawn or EnrollmentStatus.Cancelled)
                throw new ConflictApplicationException(ErrorKeys.EnrollmentTerminalReenrollmentForbidden);
            if (existing.Status != EnrollmentStatus.Active)
            {
                // Suspended seats remain reserved. Pending needs an available seat.
                if (existing.Status != EnrollmentStatus.Suspended
                    && await enrollments.CountSeatOccupyingByCohortAsync(cohort.Id, ct) >= cohort.Capacity)
                    throw new ConflictApplicationException(ErrorKeys.CohortCapacityReached);
                existing.ChangeStatus(EnrollmentStatus.Active);
            }
            return learner.Id;
        }
        if (await enrollments.CountSeatOccupyingByCohortAsync(cohort.Id, ct) >= cohort.Capacity)
            throw new ConflictApplicationException(ErrorKeys.CohortCapacityReached);

        await enrollments.AddAsync(Enrollment.Create(organizationId, learner.Id, cohort.Id, enrolledOn ?? cohort.StartDate), ct);
        return learner.Id;
    }
}
