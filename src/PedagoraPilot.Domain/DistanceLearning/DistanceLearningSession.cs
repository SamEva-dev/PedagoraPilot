using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.DistanceLearning.Events;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.DistanceLearning;
public sealed class DistanceLearningSession : AggregateRoot<DistanceLearningSessionId>
{
    private readonly List<DistanceParticipant> _participants = [];
    private DistanceLearningSession()
    {
    }

    private DistanceLearningSession(DistanceLearningSessionId id, Guid organizationId, Guid siteId, Guid programId, CohortId cohortId, string title, string trainerDisplayName, string? trainerEmail, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DistancePlatform platform, string joinUrl, string? objectives) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("DISTANCE_ORGANIZATION_REQUIRED");
        if (siteId == Guid.Empty)
            throw new DomainException("DISTANCE_SITE_REQUIRED");
        if (programId == Guid.Empty)
            throw new DomainException("DISTANCE_PROGRAM_REQUIRED");
        if (cohortId.IsEmpty)
            throw new DomainException("DISTANCE_COHORT_REQUIRED");
        if (endsAtUtc <= startsAtUtc)
            throw new DomainException("DISTANCE_SESSION_RANGE_INVALID");
        OrganizationId = organizationId;
        SiteId = siteId;
        ProgramId = programId;
        CohortId = cohortId;
        Title = Req(title, "DISTANCE_SESSION_TITLE_REQUIRED", 240);
        TrainerDisplayName = Req(trainerDisplayName, "DISTANCE_TRAINER_REQUIRED", 200);
        TrainerEmail = Opt(trainerEmail, 250);
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Platform = platform;
        JoinUrl = Req(joinUrl, "DISTANCE_JOIN_URL_REQUIRED", 2000);
        Objectives = Opt(objectives, 4000);
        Status = DistanceLearningSessionStatus.Scheduled;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid ProgramId { get; private set; }
    public CohortId CohortId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string TrainerDisplayName { get; private set; } = string.Empty;
    public string? TrainerEmail { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public DistancePlatform Platform { get; private set; }
    public string JoinUrl { get; private set; } = string.Empty;
    public string? Objectives { get; private set; }
    public DistanceLearningSessionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<DistanceParticipant> Participants => _participants.AsReadOnly();

    public static DistanceLearningSession Create(Guid organizationId, Guid siteId, Guid programId, CohortId cohortId, string title, string trainerDisplayName, string? trainerEmail, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, DistancePlatform platform, string joinUrl, string? objectives)
    {
        var x = new DistanceLearningSession(DistanceLearningSessionId.New(), organizationId, siteId, programId, cohortId, title, trainerDisplayName, trainerEmail, startsAtUtc, endsAtUtc, platform, joinUrl, objectives);
        x.RaiseDomainEvent(new DistanceLearningSessionCreatedDomainEvent(x.Id, organizationId, cohortId, x.TrainerEmail));
        return x;
    }

    public DistanceParticipant AddParticipant(EnrollmentId enrollmentId, string displayName)
    {
        if (_participants.Any(x => x.EnrollmentId == enrollmentId))
            throw new DomainException("DISTANCE_PARTICIPANT_ALREADY_EXISTS");
        var participant = new DistanceParticipant(DistanceParticipantId.New(), Id, enrollmentId, displayName);
        _participants.Add(participant);
        UpdatedAtUtc = DateTime.UtcNow;
        return participant;
    }

    public void ChangeStatus(DistanceLearningSessionStatus status)
    {
        if (Status == DistanceLearningSessionStatus.Closed && status != DistanceLearningSessionStatus.Closed)
            throw new DomainException("DISTANCE_SESSION_CLOSED");
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DistanceLearningSessionStatusChangedDomainEvent(Id, OrganizationId, status));
    }

    public void RecordParticipantAttendance(DistanceParticipantId participantId, DistanceAttendanceStatus attendance, DateTimeOffset? connectedAtUtc, DateTimeOffset? disconnectedAtUtc, int connectedMinutes, int participationPercent, int completedActivities, int activityCount)
    {
        var participant = _participants.SingleOrDefault(x => x.Id == participantId) ?? throw new DomainException("DISTANCE_PARTICIPANT_NOT_FOUND");
        participant.RecordAttendance(attendance, connectedAtUtc, disconnectedAtUtc, connectedMinutes, participationPercent, completedActivities, activityCount);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DistanceParticipantAttendanceUpdatedDomainEvent(Id, participantId, OrganizationId, attendance));
    }

    private static string Req(string? v, string k, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(k);
        var x = v.Trim();
        if (x.Length > max)
            throw new DomainException(k);
        return x;
    }

    private static string? Opt(string? v, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            return null;
        var x = v.Trim();
        return x.Length <= max ? x : x[..max];
    }
}
