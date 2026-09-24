using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning.Events;

namespace PedagoraPilot.Domain.Learning;
public sealed class LearnerProfile : AggregateRoot<LearnerProfileId>
{
    private LearnerProfile()
    {
    }

    private LearnerProfile(LearnerProfileId id, PersonId personId, Guid? authGateUserId, string? externalKey) : base(id)
    {
        PersonId = personId;
        AuthGateUserId = authGateUserId;
        ExternalKey = string.IsNullOrWhiteSpace(externalKey) ? null : externalKey.Trim();
        Status = LearnerProfileStatus.Active;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public PersonId PersonId { get; private set; }
    public Guid? AuthGateUserId { get; private set; }
    public LearnerProfileStatus Status { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static LearnerProfile Create(PersonId personId, Guid? authGateUserId = null, string? externalKey = null)
    {
        if (personId.IsEmpty)
            throw new DomainException("LEARNER_PERSON_REQUIRED");
        var profile = new LearnerProfile(LearnerProfileId.New(), personId, authGateUserId, externalKey);
        profile.RaiseDomainEvent(new LearnerProfileCreatedDomainEvent(profile.Id, personId));
        return profile;
    }

    public void LinkAuthGateUser(Guid authGateUserId)
    {
        if (authGateUserId == Guid.Empty)
            throw new DomainException("LEARNER_AUTHGATE_USER_INVALID");
        AuthGateUserId = authGateUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
