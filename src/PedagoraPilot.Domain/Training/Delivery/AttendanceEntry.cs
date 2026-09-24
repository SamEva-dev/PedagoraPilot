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

    internal void Record(AttendanceStatus status, DateTimeOffset? arrivalAtUtc, DateTimeOffset? departureAtUtc, int? presentMinutes, bool addToCatchup, string? comment)
    {
        if (arrivalAtUtc.HasValue && departureAtUtc.HasValue && departureAtUtc <= arrivalAtUtc)
            throw new DomainException("ATTENDANCE_TIME_RANGE_INVALID");
        var resolvedPresent = status switch
        {
            AttendanceStatus.Absent or AttendanceStatus.Excused => 0,
            _ => Math.Clamp(presentMinutes ?? ExpectedMinutes, 0, ExpectedMinutes)};
        Status = status;
        ArrivalAtUtc = arrivalAtUtc?.ToUniversalTime();
        DepartureAtUtc = departureAtUtc?.ToUniversalTime();
        PresentMinutes = resolvedPresent;
        MissedMinutes = Math.Max(0, ExpectedMinutes - resolvedPresent);
        AddToCatchup = addToCatchup && status != AttendanceStatus.Excused;
        CatchupMinutes = AddToCatchup ? MissedMinutes : 0;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}
