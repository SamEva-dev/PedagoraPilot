using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class AttendanceSprintS06Tests
{
    private readonly EnrollmentId _learner = EnrollmentId.New();
    private static readonly DateTimeOffset Start = new(2026, 10, 9, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddHours(2);

    private AttendanceSheet NewSheet() => AttendanceSheet.Create(Guid.NewGuid(), TrainingSessionId.New(), CohortId.New(), 120, [_learner]);

    [Fact]
    public void Timestamps_are_authoritative_and_recompute_minutes()
    {
        var sheet = NewSheet();
        sheet.Record(_learner, AttendanceStatus.Late, Start.AddMinutes(15), End, 105, true, "Trafic", Start, End);
        var entry = sheet.Entries.Single();
        entry.PresentMinutes.Should().Be(105);
        entry.MissedMinutes.Should().Be(15);
        entry.CatchupMinutes.Should().Be(15);
    }

    [Fact]
    public void Correction_recomputes_without_accumulating_catchup()
    {
        var sheet = NewSheet();
        sheet.Record(_learner, AttendanceStatus.Absent, null, null, 0, true, null, Start, End);
        sheet.Record(_learner, AttendanceStatus.Present, Start, End, 120, false, "Corrigé", Start, End);
        sheet.Entries.Single().CatchupMinutes.Should().Be(0);
        sheet.Entries.Single().PresentMinutes.Should().Be(120);
    }

    [Theory]
    [InlineData(AttendanceStatus.Pending)]
    [InlineData((AttendanceStatus)987)]
    public void Pending_or_unknown_status_is_rejected(AttendanceStatus status)
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, status, null, null, 120, false, null, Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_STATUS_INVALID");
    }

    [Fact]
    public void Late_status_requires_real_arrival_and_departure()
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, AttendanceStatus.Late, null, null, 120, false, null, Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_LATE_TIMES_REQUIRED");
    }

    [Fact]
    public void Late_arrival_must_follow_session_start()
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, AttendanceStatus.Late, Start, End, 120, false, null, Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_LATE_ARRIVAL_INVALID");
    }

    [Fact]
    public void Partial_time_pair_is_rejected()
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, AttendanceStatus.Present, Start, null, 120, false, null, Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_TIME_PAIR_REQUIRED");
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(121)]
    public void Negative_and_excess_present_minutes_are_rejected(int value)
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, AttendanceStatus.Present, null, null, value, false, null, Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_PRESENT_MINUTES_INVALID");
    }

    [Fact]
    public void Outside_session_is_rejected()
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, AttendanceStatus.Present, Start.AddMinutes(-5), End, 125, false, null, Start, End);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Timestamp_duration_mismatch_is_rejected()
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, AttendanceStatus.Present, Start, End, 60, false, null, Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_DURATION_MISMATCH");
    }

    [Theory]
    [InlineData(AttendanceStatus.Absent)]
    [InlineData(AttendanceStatus.Excused)]
    public void Absence_with_clock_or_positive_minutes_is_rejected(AttendanceStatus status)
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, status, Start, End, 120, false, null, Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_ABSENCE_TIME_INVALID");
    }

    [Fact]
    public void Fully_attended_session_does_not_create_catchup_flag()
    {
        var sheet = NewSheet();
        sheet.Record(_learner, AttendanceStatus.Present, Start, End, 120, true, null, Start, End);
        var entry = sheet.Entries.Single();
        entry.AddToCatchup.Should().BeFalse();
        entry.CatchupMinutes.Should().Be(0);
    }

    [Fact]
    public void Sheet_version_advances_after_recorded_mutation()
    {
        var sheet = NewSheet();
        var before = sheet.Version;
        sheet.Record(_learner, AttendanceStatus.Present, null, null, 120, false, null, Start, End);
        sheet.MarkRecorded();
        sheet.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Comment_cannot_exceed_database_limit()
    {
        var sheet = NewSheet();
        Action act = () => sheet.Record(_learner, AttendanceStatus.Present, null, null, 120, false, new string('x', 1001), Start, End);
        act.Should().Throw<DomainException>().Where(ex => ex.ErrorKey == "ATTENDANCE_COMMENT_TOO_LONG");
    }
}
