using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workplace;

/// <summary>Identity-bound, period-scoped tutor access. An email alone never grants access.</summary>
public sealed class WorkplaceTutorAssignment
{
    private WorkplaceTutorAssignment() { }
    private WorkplaceTutorAssignment(Guid organizationId, Guid periodId, Guid? invitationId, string email)
    {
        if (organizationId == Guid.Empty || periodId == Guid.Empty || (invitationId.HasValue && invitationId.Value == Guid.Empty))
            throw new DomainException("WORKPLACE_TUTOR_SCOPE_INVALID");
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 250 || !email.Contains('@'))
            throw new DomainException("WORKPLACE_TUTOR_EMAIL_INVALID");
        Id = Guid.NewGuid(); OrganizationId = organizationId; WorkplacePeriodId = new WorkplacePeriodId(periodId); InvitationId = invitationId;
        InvitedEmail = email.Trim().ToLowerInvariant(); CreatedAtUtc = DateTime.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public WorkplacePeriodId WorkplacePeriodId { get; private set; }
    public Guid? InvitationId { get; private set; }
    public string InvitedEmail { get; private set; } = "";
    public Guid? AuthGateUserId { get; private set; }
    public WorkplaceTutorAssignmentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public static WorkplaceTutorAssignment Invite(Guid organizationId, Guid periodId, Guid invitationId, string email) => new(organizationId, periodId, invitationId, email);

    // Authorized organization management may bind a VERIFIED, active local AuthGate member
    // directly. This NEVER looks up an unverified identity by an arbitrary email.
    public static WorkplaceTutorAssignment AssignExisting(Guid organizationId, Guid periodId, Guid verifiedUserId, string verifiedEmail)
    {
        if (verifiedUserId == Guid.Empty) throw new DomainException("WORKPLACE_TUTOR_IDENTITY_MISMATCH");
        var result = new WorkplaceTutorAssignment(organizationId, periodId, null, verifiedEmail);
        result.Activate(verifiedUserId, verifiedEmail);
        return result;
    }
    // Only the verified invitation acceptance workflow may invoke Activate.
    public void Activate(Guid verifiedUserId, string verifiedEmail)
    {
        if (Status != WorkplaceTutorAssignmentStatus.Pending)
            throw new DomainException("WORKPLACE_TUTOR_ASSIGNMENT_NOT_PENDING");
        if (verifiedUserId == Guid.Empty || !string.Equals(InvitedEmail, verifiedEmail?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("WORKPLACE_TUTOR_IDENTITY_MISMATCH");
        AuthGateUserId = verifiedUserId; Status = WorkplaceTutorAssignmentStatus.Active;
        ActivatedAtUtc = DateTime.UtcNow;
    }
    public void Revoke()
    {
        if (Status == WorkplaceTutorAssignmentStatus.Revoked) return;
        Status = WorkplaceTutorAssignmentStatus.Revoked; RevokedAtUtc = DateTime.UtcNow;
    }
    public bool GrantsAccess(Guid organizationId, Guid periodId, Guid userId) =>
        Status == WorkplaceTutorAssignmentStatus.Active && organizationId != Guid.Empty
        && organizationId == OrganizationId && periodId != Guid.Empty && periodId == WorkplacePeriodId.Value
        && userId != Guid.Empty && AuthGateUserId == userId;
}
public enum WorkplaceTutorAssignmentStatus { Pending, Active, Revoked }
