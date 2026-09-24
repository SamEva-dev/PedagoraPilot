using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Events;

namespace PedagoraPilot.Domain.Training;
public sealed class Enrollment : AggregateRoot<EnrollmentId>
{
    private Enrollment()
    {
    }

    private Enrollment(EnrollmentId id, Guid organizationId, LearnerProfileId learnerProfileId, CohortId cohortId, DateOnly enrolledOn, string? externalKey) : base(id)
    {
        OrganizationId = organizationId;
        LearnerProfileId = learnerProfileId;
        CohortId = cohortId;
        EnrolledOn = enrolledOn;
        ExternalKey = string.IsNullOrWhiteSpace(externalKey) ? null : externalKey.Trim();
        Status = EnrollmentStatus.Active;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public LearnerProfileId LearnerProfileId { get; private set; }
    public CohortId CohortId { get; private set; }
    public DateOnly EnrolledOn { get; private set; }
    public DateOnly? EndedOn { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Enrollment Create(Guid organizationId, LearnerProfileId learnerProfileId, CohortId cohortId, DateOnly enrolledOn, string? externalKey = null)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("ENROLLMENT_ORGANIZATION_REQUIRED");
        if (learnerProfileId.IsEmpty)
            throw new DomainException("ENROLLMENT_LEARNER_REQUIRED");
        if (cohortId.IsEmpty)
            throw new DomainException("ENROLLMENT_COHORT_REQUIRED");
        var enrollment = new Enrollment(EnrollmentId.New(), organizationId, learnerProfileId, cohortId, enrolledOn, externalKey);
        enrollment.RaiseDomainEvent(new EnrollmentCreatedDomainEvent(enrollment.Id, learnerProfileId, cohortId, organizationId));
        return enrollment;
    }

    public void ChangeStatus(EnrollmentStatus status, DateOnly? endedOn = null)
    {
        if (Status == EnrollmentStatus.Completed && status != EnrollmentStatus.Completed)
            throw new DomainException("ENROLLMENT_COMPLETED_LOCKED");
        Status = status;
        EndedOn = status is EnrollmentStatus.Completed or EnrollmentStatus.Withdrawn or EnrollmentStatus.Cancelled ? endedOn ?? DateOnly.FromDateTime(DateTime.UtcNow) : null;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new EnrollmentStatusChangedDomainEvent(Id, OrganizationId, status));
    }
}
