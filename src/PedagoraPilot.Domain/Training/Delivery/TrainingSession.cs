using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery.Events;

namespace PedagoraPilot.Domain.Training.Delivery;
public sealed class TrainingSession : AggregateRoot<TrainingSessionId>
{
    private readonly List<TrainingSessionParticipant> _participants = [];
    private TrainingSession()
    {
    }

    private TrainingSession(TrainingSessionId id, Guid organizationId, Guid siteId, CohortId cohortId, TrainingSessionType type, TrainingSessionModality modality, string title, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string timeZoneId, string? trainerAuthGateUserId, string? trainerDisplayName, string? location, string? objective, string? supports, string? comments, SessionAudienceMode audienceMode, IEnumerable<EnrollmentId>? participantEnrollmentIds, string? externalKey) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("SESSION_ORGANIZATION_REQUIRED");
        if (siteId == Guid.Empty)
            throw new DomainException("SESSION_SITE_REQUIRED");
        if (cohortId.IsEmpty)
            throw new DomainException("SESSION_COHORT_REQUIRED");
        ValidatePeriod(startsAtUtc, endsAtUtc);
        OrganizationId = organizationId;
        SiteId = siteId;
        CohortId = cohortId;
        Type = type;
        Modality = modality;
        Title = NormalizeRequired(title, "SESSION_TITLE_REQUIRED", 240);
        StartsAtUtc = startsAtUtc.ToUniversalTime();
        EndsAtUtc = endsAtUtc.ToUniversalTime();
        TimeZoneId = NormalizeRequired(timeZoneId, "SESSION_TIMEZONE_REQUIRED", 100);
        TrainerAuthGateUserId = NormalizeOptional(trainerAuthGateUserId, 200);
        TrainerDisplayName = NormalizeOptional(trainerDisplayName, 200);
        Location = NormalizeOptional(location, 240);
        Objective = NormalizeOptional(objective, 2000);
        Supports = NormalizeOptional(supports, 2000);
        Comments = NormalizeOptional(comments, 4000);
        AudienceMode = audienceMode;
        SetParticipants(audienceMode, participantEnrollmentIds);
        ExternalKey = NormalizeOptional(externalKey, 100);
        Status = TrainingSessionStatus.Planned;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid SiteId { get; private set; }
    public CohortId CohortId { get; private set; }
    public TrainingSessionType Type { get; private set; }
    public TrainingSessionModality Modality { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public string TimeZoneId { get; private set; } = "Europe/Paris";
    public string? TrainerAuthGateUserId { get; private set; }
    public string? TrainerDisplayName { get; private set; }
    public string? Location { get; private set; }
    public string? Objective { get; private set; }
    public string? Supports { get; private set; }
    public string? Comments { get; private set; }
    public TrainingSessionStatus Status { get; private set; }
    public SessionAudienceMode AudienceMode { get; private set; }
    public IReadOnlyCollection<TrainingSessionParticipant> Participants => _participants.AsReadOnly();
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public int PlannedMinutes => checked((int)(EndsAtUtc - StartsAtUtc).TotalMinutes);

    public static TrainingSession Create(Guid organizationId, Guid siteId, CohortId cohortId, TrainingSessionType type, TrainingSessionModality modality, string title, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string timeZoneId, string? trainerAuthGateUserId = null, string? trainerDisplayName = null, string? location = null, string? objective = null, string? supports = null, string? comments = null, SessionAudienceMode audienceMode = SessionAudienceMode.WholeCohort, IEnumerable<EnrollmentId>? participantEnrollmentIds = null, string? externalKey = null)
    {
        var entity = new TrainingSession(TrainingSessionId.New(), organizationId, siteId, cohortId, type, modality, title, startsAtUtc, endsAtUtc, timeZoneId, trainerAuthGateUserId, trainerDisplayName, location, objective, supports, comments, audienceMode, participantEnrollmentIds, externalKey);
        entity.RaiseDomainEvent(new TrainingSessionCreatedDomainEvent(entity.Id, organizationId, cohortId));
        return entity;
    }

    public void Update(TrainingSessionType type, TrainingSessionModality modality, string title, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string timeZoneId, string? trainerAuthGateUserId, string? trainerDisplayName, string? location, string? objective, string? supports, string? comments, SessionAudienceMode audienceMode, IEnumerable<EnrollmentId>? participantEnrollmentIds)
    {
        if (Status is TrainingSessionStatus.Completed or TrainingSessionStatus.Cancelled)
            throw new DomainException("SESSION_LOCKED");
        ValidatePeriod(startsAtUtc, endsAtUtc);
        Type = type;
        Modality = modality;
        Title = NormalizeRequired(title, "SESSION_TITLE_REQUIRED", 240);
        StartsAtUtc = startsAtUtc.ToUniversalTime();
        EndsAtUtc = endsAtUtc.ToUniversalTime();
        TimeZoneId = NormalizeRequired(timeZoneId, "SESSION_TIMEZONE_REQUIRED", 100);
        TrainerAuthGateUserId = NormalizeOptional(trainerAuthGateUserId, 200);
        TrainerDisplayName = NormalizeOptional(trainerDisplayName, 200);
        Location = NormalizeOptional(location, 240);
        Objective = NormalizeOptional(objective, 2000);
        Supports = NormalizeOptional(supports, 2000);
        Comments = NormalizeOptional(comments, 4000);
        AudienceMode = audienceMode;
        SetParticipants(audienceMode, participantEnrollmentIds);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingSessionUpdatedDomainEvent(Id, OrganizationId));
    }

    public void Start()
    {
        if (Status != TrainingSessionStatus.Planned)
            throw new DomainException("SESSION_CANNOT_START");
        Status = TrainingSessionStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status is TrainingSessionStatus.Cancelled or TrainingSessionStatus.Completed)
            throw new DomainException("SESSION_CANNOT_COMPLETE");
        Status = TrainingSessionStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingSessionUpdatedDomainEvent(Id, OrganizationId));
    }

    public void Cancel()
    {
        if (Status == TrainingSessionStatus.Completed)
            throw new DomainException("SESSION_COMPLETED_LOCKED");
        if (Status == TrainingSessionStatus.Cancelled)
            return;
        Status = TrainingSessionStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingSessionCancelledDomainEvent(Id, OrganizationId));
    }

    private void SetParticipants(SessionAudienceMode audienceMode, IEnumerable<EnrollmentId>? participantEnrollmentIds)
    {
        _participants.Clear();
        if (audienceMode == SessionAudienceMode.WholeCohort)
            return;
        var ids = (participantEnrollmentIds ?? Array.Empty<EnrollmentId>()).Where(x => !x.IsEmpty).Distinct().ToArray();
        if (ids.Length == 0)
            throw new DomainException("SESSION_PARTICIPANTS_REQUIRED");
        foreach (var enrollmentId in ids)
            _participants.Add(new TrainingSessionParticipant(TrainingSessionParticipantId.New(), enrollmentId));
    }

    private static void ValidatePeriod(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new DomainException("SESSION_DATE_RANGE_INVALID");
        if ((end - start).TotalHours > 24)
            throw new DomainException("SESSION_DURATION_INVALID");
    }

    private static string NormalizeRequired(string value, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(key);
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainException(key);
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : normalized[..max];
    }
}
