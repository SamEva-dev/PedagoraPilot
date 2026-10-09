using FluentAssertions;
using PedagoraPilot.Application.Training.Delivery;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class AttendanceSprintS06ValidatorTests
{
    private static AttendanceEntryUpdateRequest Entry(Guid enrollmentId, int? minutes = 90, string? comment = null)
        => new(enrollmentId, "present", null, null, minutes, false, comment);

    [Fact]
    public async Task Repeated_enrollment_is_rejected_with_business_error_code()
    {
        var id = Guid.NewGuid();
        var command = new SaveAttendanceCommand(TrainingSessionId.New(), [Entry(id), Entry(id)]);
        var result = await new SaveAttendanceCommandValidator().ValidateAsync(command);
        result.Errors.Should().Contain(x => x.ErrorCode == "ATTENDANCE_DUPLICATE_ENROLLMENT");
    }

    [Fact]
    public async Task Empty_submission_is_rejected()
    {
        var command = new SaveAttendanceCommand(TrainingSessionId.New(), []);
        var result = await new SaveAttendanceCommandValidator().ValidateAsync(command);
        result.Errors.Should().Contain(x => x.ErrorCode == "ATTENDANCE_NO_ENTRIES");
    }

    [Fact]
    public async Task Comment_too_long_is_rejected()
    {
        var command = new SaveAttendanceCommand(TrainingSessionId.New(), [Entry(Guid.NewGuid(), comment: new string('x', 1001))]);
        var result = await new SaveAttendanceCommandValidator().ValidateAsync(command);
        result.Errors.Should().Contain(x => x.ErrorCode == "ATTENDANCE_COMMENT_TOO_LONG");
    }

    [Fact]
    public async Task Different_enrollments_are_accepted()
    {
        var command = new SaveAttendanceCommand(TrainingSessionId.New(), [Entry(Guid.NewGuid()), Entry(Guid.NewGuid())]);
        (await new SaveAttendanceCommandValidator().ValidateAsync(command)).IsValid.Should().BeTrue();
    }
}
