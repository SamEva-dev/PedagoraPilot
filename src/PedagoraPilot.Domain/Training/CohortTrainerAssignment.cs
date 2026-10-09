using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Events;

namespace PedagoraPilot.Domain.Training;

/// <summary>
/// Business assignment of an organization trainer to a concrete cohort.
/// Authentication/authorization stays in AuthGate; this relation belongs to PedagoraPilot.
/// </summary>
public sealed class CohortTrainerAssignment : AggregateRoot<CohortTrainerAssignmentId>
{
    private CohortTrainerAssignment() { }

    private CohortTrainerAssignment(
        CohortTrainerAssignmentId id,
        Guid organizationId,
        CohortId cohortId,
        OrganizationMemberId memberId,
        Guid authGateUserId) : base(id)
    {
        OrganizationId = organizationId;
        CohortId = cohortId;
        MemberId = memberId;
        AuthGateUserId = authGateUserId;
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
        RaiseDomainEvent(new CohortTrainerAssignmentChangedDomainEvent(Id, cohortId, organizationId, authGateUserId, true));
    }

    public Guid OrganizationId { get; private set; }
    public CohortId CohortId { get; private set; }
    public OrganizationMemberId MemberId { get; private set; }
    public Guid AuthGateUserId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static CohortTrainerAssignment Create(
        Guid organizationId,
        CohortId cohortId,
        OrganizationMemberId memberId,
        Guid authGateUserId)
    {
        if (organizationId == Guid.Empty) throw new DomainException("TRAINER_ASSIGNMENT_ORGANIZATION_REQUIRED");
        if (cohortId.IsEmpty) throw new DomainException("TRAINER_ASSIGNMENT_COHORT_REQUIRED");
        if (memberId.IsEmpty) throw new DomainException("TRAINER_ASSIGNMENT_MEMBER_REQUIRED");
        if (authGateUserId == Guid.Empty) throw new DomainException("TRAINER_ASSIGNMENT_USER_REQUIRED");
        return new CohortTrainerAssignment(CohortTrainerAssignmentId.New(), organizationId, cohortId, memberId, authGateUserId);
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CohortTrainerAssignmentChangedDomainEvent(Id, CohortId, OrganizationId, AuthGateUserId, true));
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CohortTrainerAssignmentChangedDomainEvent(Id, CohortId, OrganizationId, AuthGateUserId, false));
    }
}
