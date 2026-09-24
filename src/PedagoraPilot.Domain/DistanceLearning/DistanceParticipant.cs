using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.DistanceLearning;
public sealed class DistanceParticipant : Entity<DistanceParticipantId>
{
    private DistanceParticipant()
    {
    }

    internal DistanceParticipant(DistanceParticipantId id, DistanceLearningSessionId sessionId, EnrollmentId enrollmentId, string displayName) : base(id)
    {
        SessionId = sessionId;
        EnrollmentId = enrollmentId;
        DisplayName = Required(displayName, "DISTANCE_PARTICIPANT_NAME_REQUIRED", 200);
        Attendance = DistanceAttendanceStatus.Pending;
    }

    public DistanceLearningSessionId SessionId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public DistanceAttendanceStatus Attendance { get; private set; }
    public DateTimeOffset? ConnectedAtUtc { get; private set; }
    public DateTimeOffset? DisconnectedAtUtc { get; private set; }
    public int ConnectedMinutes { get; private set; }
    public int ParticipationPercent { get; private set; }
    public int CompletedActivities { get; private set; }
    public int ActivityCount { get; private set; }

    internal void RecordAttendance(DistanceAttendanceStatus attendance, DateTimeOffset? connectedAtUtc, DateTimeOffset? disconnectedAtUtc, int connectedMinutes, int participationPercent, int completedActivities, int activityCount)
    {
        if (connectedMinutes < 0)
            throw new DomainException("DISTANCE_CONNECTED_MINUTES_INVALID");
        if (participationPercent is < 0 or > 100)
            throw new DomainException("DISTANCE_PARTICIPATION_INVALID");
        if (completedActivities < 0 || activityCount < 0 || completedActivities > activityCount)
            throw new DomainException("DISTANCE_ACTIVITIES_INVALID");
        Attendance = attendance;
        ConnectedAtUtc = connectedAtUtc;
        DisconnectedAtUtc = disconnectedAtUtc;
        ConnectedMinutes = connectedMinutes;
        ParticipationPercent = participationPercent;
        CompletedActivities = completedActivities;
        ActivityCount = activityCount;
    }

    private static string Required(string? value, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(key);
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainException(key);
        return normalized;
    }
}
