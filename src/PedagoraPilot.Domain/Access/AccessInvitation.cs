using System.Security.Cryptography;
using System.Text;
using PedagoraPilot.Domain.Access.Events;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Access;

public enum AccessInvitationStatus
{
    Pending = 0,
    Processing = 1,
    Accepted = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5
}

/// <summary>
/// Product-owned invitation. AuthGate owns identity/authentication only; PedagoraPilot owns
/// who is being invited into a Pedagora organization, with which product role and scope.
/// </summary>
public sealed class AccessInvitation : AggregateRoot<AccessInvitationId>
{
    private AccessInvitation() { }

    private AccessInvitation(AccessInvitationId id, Guid organizationId, string email, string firstName,
        string lastName, string requestedRole, string tokenHash, Guid invitedByUserId, DateTime expiresAtUtc) : base(id)
    {
        OrganizationId = organizationId;
        Email = NormalizeEmail(email);
        FirstName = Normalize(firstName, 120);
        LastName = Normalize(lastName, 120);
        RequestedRole = Normalize(requestedRole, 128);
        TokenHash = tokenHash;
        InvitedByUserId = invitedByUserId;
        Status = AccessInvitationStatus.Pending;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string RequestedRole { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public AccessInvitationStatus Status { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public Guid? AcceptedAuthGateUserId { get; private set; }
    public Guid? SiteId { get; private set; }
    public Guid? ProgramId { get; private set; }
    public Guid? CohortId { get; private set; }
    public Guid? ExamSessionId { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public DateOnly? EnrolledOn { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static (AccessInvitation Invitation, string Token) Create(Guid organizationId, string email, string firstName,
        string lastName, string requestedRole, Guid invitedByUserId, int expirationDays = 7,
        Guid? siteId = null, Guid? programId = null, Guid? cohortId = null, Guid? examSessionId = null,
        string? phone = null, DateOnly? birthDate = null, DateOnly? enrolledOn = null)
    {
        if (organizationId == Guid.Empty) throw new DomainException("ACCESS_INVITATION_ORGANIZATION_REQUIRED");
        if (invitedByUserId == Guid.Empty) throw new DomainException("ACCESS_INVITATION_INVITER_REQUIRED");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("ACCESS_INVITATION_EMAIL_REQUIRED");
        if (string.IsNullOrWhiteSpace(requestedRole)) throw new DomainException("ACCESS_INVITATION_ROLE_REQUIRED");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var hash = ComputeTokenHash(token);
        var entity = new AccessInvitation(AccessInvitationId.New(), organizationId, email, firstName, lastName,
            requestedRole, hash, invitedByUserId, DateTime.UtcNow.AddDays(expirationDays))
        {
            SiteId = siteId,
            ProgramId = programId,
            CohortId = cohortId,
            ExamSessionId = examSessionId,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : Normalize(phone, 40),
            BirthDate = birthDate,
            EnrolledOn = enrolledOn
        };
        entity.RaiseDomainEvent(new AccessInvitationCreatedDomainEvent(entity.Id, entity.OrganizationId, token));
        return (entity, token);
    }

    public bool MatchesToken(string token) => FixedEquals(TokenHash, ComputeTokenHash(token));
    public bool IsUsable(DateTime utcNow) => Status == AccessInvitationStatus.Pending && ExpiresAtUtc > utcNow;

    public void MarkProcessing()
    {
        if (Status != AccessInvitationStatus.Pending) return;
        Status = AccessInvitationStatus.Processing;
        LastAttemptAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        FailureReason = null;
    }

    public void MarkPendingAfterDispatch()
    {
        if (Status != AccessInvitationStatus.Processing) return;
        Status = AccessInvitationStatus.Pending;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Accept(Guid authGateUserId)
    {
        if (authGateUserId == Guid.Empty) throw new DomainException("ACCESS_INVITATION_AUTH_USER_REQUIRED");
        AcceptedAuthGateUserId = authGateUserId;
        Status = AccessInvitationStatus.Accepted;
        AcceptedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        FailureReason = null;
        RaiseDomainEvent(new AccessInvitationAcceptedDomainEvent(Id, OrganizationId, authGateUserId));
    }

    public void Fail(string reason)
    {
        Status = AccessInvitationStatus.Failed;
        FailureReason = Normalize(reason, 2000);
        LastAttemptAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordRetryableFailure(string reason)
    {
        Status = AccessInvitationStatus.Pending;
        FailureReason = Normalize(reason, 2000);
        LastAttemptAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is AccessInvitationStatus.Accepted or AccessInvitationStatus.Cancelled) return;
        Status = AccessInvitationStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Expire(DateTime utcNow)
    {
        if (Status == AccessInvitationStatus.Pending && ExpiresAtUtc <= utcNow)
        {
            Status = AccessInvitationStatus.Expired;
            UpdatedAtUtc = utcNow;
        }
    }

    private static string Normalize(string? value, int max)
    {
        var v = (value ?? string.Empty).Trim();
        return v.Length <= max ? v : v[..max];
    }

    private static string NormalizeEmail(string value)
    {
        var v = value.Trim().ToLowerInvariant();
        if (v.Length > 254) throw new DomainException("ACCESS_INVITATION_EMAIL_INVALID");
        return v;
    }

    public static string ComputeTokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();
    private static bool FixedEquals(string left, string right) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
