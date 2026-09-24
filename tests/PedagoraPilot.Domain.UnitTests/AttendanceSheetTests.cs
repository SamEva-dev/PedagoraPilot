using FluentAssertions;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class AttendanceSheetTests
{
    [Fact]
    public void Absent_entry_computes_missed_and_catchup_minutes()
    {
        var enrollment = EnrollmentId.New();
        var sheet = AttendanceSheet.Create(Guid.NewGuid(), TrainingSessionId.New(), CohortId.New(), 120, [enrollment]);
        sheet.Record(enrollment, AttendanceStatus.Absent, null, null, null, true, "Absent");
        var entry = sheet.Entries.Single();
        entry.PresentMinutes.Should().Be(0);
        entry.MissedMinutes.Should().Be(120);
        entry.CatchupMinutes.Should().Be(120);
    }

    [Fact]
    public void Excused_entry_never_adds_catchup()
    {
        var enrollment = EnrollmentId.New();
        var sheet = AttendanceSheet.Create(Guid.NewGuid(), TrainingSessionId.New(), CohortId.New(), 60, [enrollment]);
        sheet.Record(enrollment, AttendanceStatus.Excused, null, null, null, true, null);
        sheet.Entries.Single().CatchupMinutes.Should().Be(0);
    }

    [Fact]
    public void Preview_does_not_raise_a_domain_event()
    {
        var sheet = AttendanceSheet.Preview(Guid.NewGuid(), TrainingSessionId.New(), CohortId.New(), 60, [EnrollmentId.New()]);
        sheet.DomainEvents.Should().BeEmpty();
    }
}
