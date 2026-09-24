using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Certification;
public sealed class JuryAssignment : AggregateRoot<JuryAssignmentId>
{
    private JuryAssignment()
    {
    }

    private JuryAssignment(JuryAssignmentId id, Guid organizationId, CertificationExamSessionId examSessionId, Guid authGateUserId, string displayName, string role) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("CERTIFICATION_ORGANIZATION_REQUIRED");
        if (examSessionId.IsEmpty)
            throw new DomainException("CERTIFICATION_SESSION_REQUIRED");
        if (authGateUserId == Guid.Empty)
            throw new DomainException("CERTIFICATION_JURY_USER_REQUIRED");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("CERTIFICATION_JURY_REQUIRED");
        OrganizationId = organizationId;
        ExamSessionId = examSessionId;
        AuthGateUserId = authGateUserId;
        DisplayName = displayName.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? "jury" : role.Trim();
        AssignedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid OrganizationId { get; private set; }
    public CertificationExamSessionId ExamSessionId { get; private set; }
    public Guid AuthGateUserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public DateTimeOffset AssignedAtUtc { get; private set; }

    public static JuryAssignment Create(Guid organizationId, CertificationExamSessionId examSessionId, Guid authGateUserId, string displayName, string role) => new(JuryAssignmentId.New(), organizationId, examSessionId, authGateUserId, displayName, role);
}
