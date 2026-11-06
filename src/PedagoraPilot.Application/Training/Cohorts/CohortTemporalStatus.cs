using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Training.Cohorts;

/// <summary>Calendar projection; administrative Draft/Cancelled are never overwritten.</summary>
public static class CohortTemporalStatus
{
    // Default operational calendar for the pilot. A configurable per-site timezone
    // should replace this when the application serves sites outside metropolitan France.
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public static DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone));

    public static CohortStatus FromDates(DateOnly start, DateOnly end, DateOnly today)
        => today < start ? CohortStatus.Planned : today > end ? CohortStatus.Completed : CohortStatus.Active;

    public static CohortStatus DisplayStatus(CohortStatus persisted, DateOnly start, DateOnly end, DateOnly today)
        => persisted is CohortStatus.Draft or CohortStatus.Cancelled
            ? persisted
            : FromDates(start, end, today);
}
