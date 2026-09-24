namespace PedagoraPilot.Domain.DistanceLearning;
public enum DistancePlatform
{
    Teams,
    Zoom,
    Meet,
    Jitsi,
    Other
}

public enum DistanceLearningSessionStatus
{
    Scheduled,
    Live,
    Closed,
    Cancelled
}

public enum DistanceAttendanceStatus
{
    Pending,
    Present,
    Late,
    Absent,
    Disconnected
}

public enum AsyncLearningModuleStatus
{
    NotStarted,
    InProgress,
    Completed,
    Late,
    Archived
}
