using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Training.Delivery;
public sealed class AttendanceEntry : Entity<AttendanceEntryId>
{
    private AttendanceEntry()
    {
    }

    internal AttendanceEntry(AttendanceEntryId id, EnrollmentId enrollmentId, int expectedMinutes) : base(id)
    {
        if (enrollmentId.IsEmpty)
            throw new DomainException("ATTENDANCE_ENROLLMENT_REQUIRED");
        if (expectedMinutes <= 0)
            throw new DomainException("ATTENDANCE_EXPECTED_MINUTES_INVALID");
        EnrollmentId = enrollmentId;
        ExpectedMinutes = expectedMinutes;
        Status = AttendanceStatus.Pending;
        PresentMinutes = 0;
        MissedMinutes = 0;
        CatchupMinutes = 0;
    }

    public EnrollmentId EnrollmentId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public DateTimeOffset? ArrivalAtUtc { get; private set; }
    public DateTimeOffset? DepartureAtUtc { get; private set; }
    public int ExpectedMinutes { get; private set; }
    public int PresentMinutes { get; private set; }
    public int MissedMinutes { get; private set; }
    public int CatchupMinutes { get; private set; }
    public bool AddToCatchup { get; private set; }
    public string? Comment { get; private set; }

    internal void Record(AttendanceStatus status, DateTimeOffset? arrivalAtUtc, DateTimeOffset? departureAtUtc, int? presentMinutes, bool addToCatchup, string? comment, DateTimeOffset? sessionStartsAtUtc = null, DateTimeOffset? sessionEndsAtUtc = null)
    {
        if (status is AttendanceStatus.Pending || !Enum.IsDefined(status))
            throw new DomainException("ATTENDANCE_STATUS_INVALID");
        if (arrivalAtUtc.HasValue != departureAtUtc.HasValue)
            throw new DomainException("ATTENDANCE_TIME_PAIR_REQUIRED");
        if (presentMinutes < 0 || presentMinutes > ExpectedMinutes)
            throw new DomainException("ATTENDANCE_PRESENT_MINUTES_INVALID");
        if (comment?.Trim().Length > 1000)
            throw new DomainException("ATTENDANCE_COMMENT_TOO_LONG");

        if (status == AttendanceStatus.Late && !arrivalAtUtc.HasValue)
            throw new DomainException("ATTENDANCE_LATE_TIMES_REQUIRED");

        var absent = status is AttendanceStatus.Absent or AttendanceStatus.Excused;
        if (absent && (arrivalAtUtc.HasValue || departureAtUtc.HasValue || presentMinutes is > 0))
            throw new DomainException("ATTENDANCE_ABSENCE_TIME_INVALID");

        var resolvedPresent = absent ? 0 : presentMinutes ?? ExpectedMinutes;
        if (arrivalAtUtc.HasValue && departureAtUtc.HasValue)
        {
            var arrival = arrivalAtUtc.Value.ToUniversalTime();
            var departure = departureAtUtc.Value.ToUniversalTime();
            if (departure <= arrival)
                throw new DomainException("ATTENDANCE_TIME_RANGE_INVALID");
            if (sessionStartsAtUtc.HasValue && sessionEndsAtUtc.HasValue &&
                (arrival < sessionStartsAtUtc.Value.ToUniversalTime() || departure > sessionEndsAtUtc.Value.ToUniversalTime()))
                throw new DomainException("ATTENDANCE_OUTSIDE_SESSION");
            if (status == AttendanceStatus.Late && sessionStartsAtUtc.HasValue &&
                arrival <= sessionStartsAtUtc.Value.ToUniversalTime())
                throw new DomainException("ATTENDANCE_LATE_ARRIVAL_INVALID");
            var minutes = checked((int)Math.Round((departure - arrival).TotalMinutes, MidpointRounding.AwayFromZero));
            if (minutes <= 0 || minutes > ExpectedMinutes)
                throw new DomainException("ATTENDANCE_PRESENT_MINUTES_INVALID");
            if (presentMinutes.HasValue && Math.Abs(presentMinutes.Value - minutes) > 1)
                throw new DomainException("ATTENDANCE_DURATION_MISMATCH");
            resolvedPresent = minutes;
        }
        if (!absent && resolvedPresent <= 0)
            throw new DomainException("ATTENDANCE_PRESENT_MINUTES_INVALID");

        Status = status;
        ArrivalAtUtc = arrivalAtUtc?.ToUniversalTime();
        DepartureAtUtc = departureAtUtc?.ToUniversalTime();
        PresentMinutes = resolvedPresent;
        MissedMinutes = ExpectedMinutes - resolvedPresent;
        AddToCatchup = addToCatchup && status != AttendanceStatus.Excused && MissedMinutes > 0;
        CatchupMinutes = AddToCatchup ? MissedMinutes : 0;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}
