using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery.Events;

namespace PedagoraPilot.Domain.Training.Delivery;
public sealed class AttendanceSheet : AggregateRoot<AttendanceSheetId>
{
    private readonly List<AttendanceEntry> _entries = [];
    private AttendanceSheet()
    {
    }

    private AttendanceSheet(AttendanceSheetId id, Guid organizationId, TrainingSessionId sessionId, CohortId cohortId, int expectedMinutes) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("ATTENDANCE_ORGANIZATION_REQUIRED");
        if (sessionId.IsEmpty)
            throw new DomainException("ATTENDANCE_SESSION_REQUIRED");
        if (cohortId.IsEmpty)
            throw new DomainException("ATTENDANCE_COHORT_REQUIRED");
        if (expectedMinutes <= 0)
            throw new DomainException("ATTENDANCE_EXPECTED_MINUTES_INVALID");
        OrganizationId = organizationId;
        SessionId = sessionId;
        CohortId = cohortId;
        ExpectedMinutes = expectedMinutes;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public TrainingSessionId SessionId { get; private set; }
    public CohortId CohortId { get; private set; }
    public int ExpectedMinutes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<AttendanceEntry> Entries => _entries.AsReadOnly();

    public static AttendanceSheet Create(Guid organizationId, TrainingSessionId sessionId, CohortId cohortId, int expectedMinutes, IEnumerable<EnrollmentId> enrollments)
    {
        var sheet = new AttendanceSheet(AttendanceSheetId.New(), organizationId, sessionId, cohortId, expectedMinutes);
        foreach (var enrollmentId in enrollments.Distinct())
            sheet._entries.Add(new AttendanceEntry(AttendanceEntryId.New(), enrollmentId, expectedMinutes));
        sheet.RaiseDomainEvent(new AttendanceSheetInitializedDomainEvent(sheet.Id, sessionId, organizationId));
        return sheet;
    }

    public static AttendanceSheet Preview(Guid organizationId, TrainingSessionId sessionId, CohortId cohortId, int expectedMinutes, IEnumerable<EnrollmentId> enrollments)
    {
        var sheet = new AttendanceSheet(AttendanceSheetId.New(), organizationId, sessionId, cohortId, expectedMinutes);
        foreach (var enrollmentId in enrollments.Distinct())
            sheet._entries.Add(new AttendanceEntry(AttendanceEntryId.New(), enrollmentId, expectedMinutes));
        return sheet;
    }

    public void SynchronizeEnrollments(IEnumerable<EnrollmentId> enrollmentIds)
    {
        var known = _entries.Select(x => x.EnrollmentId).ToHashSet();
        foreach (var enrollmentId in enrollmentIds.Distinct().Where(x => !known.Contains(x)))
            _entries.Add(new AttendanceEntry(AttendanceEntryId.New(), enrollmentId, ExpectedMinutes));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Record(EnrollmentId enrollmentId, AttendanceStatus status, DateTimeOffset? arrivalAtUtc, DateTimeOffset? departureAtUtc, int? presentMinutes, bool addToCatchup, string? comment)
    {
        var entry = _entries.SingleOrDefault(x => x.EnrollmentId == enrollmentId) ?? throw new DomainException("ATTENDANCE_ENTRY_NOT_FOUND");
        entry.Record(status, arrivalAtUtc, departureAtUtc, presentMinutes, addToCatchup, comment);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkRecorded()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new AttendanceRecordedDomainEvent(Id, SessionId, OrganizationId));
    }
}
