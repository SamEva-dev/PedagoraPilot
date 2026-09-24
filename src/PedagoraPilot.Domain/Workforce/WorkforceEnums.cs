namespace PedagoraPilot.Domain.Workforce;
public enum RemoteWorkRequestStatus
{
    Requested,
    Approved,
    Rejected,
    Completed,
    Cancelled
}

public enum RemoteWorkPeriod
{
    FullDay,
    Morning,
    Afternoon,
    Custom
}

public enum RemoteWorkActivityStatus
{
    Todo,
    InProgress,
    Done
}
