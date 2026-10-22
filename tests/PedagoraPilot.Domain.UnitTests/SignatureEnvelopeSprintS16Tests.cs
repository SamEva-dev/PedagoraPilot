using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class SignatureEnvelopeSprintS16Tests
{
    private static readonly DateTimeOffset Initial = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);
    private static SignatureEnvelope NewAttendance() => SignatureEnvelope.CreateDraft(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EnrollmentId.New(), Guid.NewGuid(),
        SignatureSourceType.Attendance, Guid.NewGuid(), 3,
        "{\"Type\":\"attendance\",\"PresentMinutes\":120,\"Status\":\"Present\"}",
        Guid.NewGuid().ToString("D"), Initial);

    [Fact]
    public void Cannot_confirm_attendance_before_request()
    {
        var envelope = NewAttendance();
        Action act = () => envelope.Sign(Initial.AddMinutes(1), "ack-sha256:foo");
        act.Should().Throw<DomainException>()
            .Where(x => x.ErrorKey == "SIGNATURE_TRANSITION_FORBIDDEN");
    }

    [Fact]
    public void Confirmation_never_modifies_original_attendance_snapshot()
    {
        var envelope = NewAttendance();
        var bytes = envelope.SnapshotJson;
        var checksum = envelope.ContentSha256;
        envelope.Request(Initial, Initial.AddDays(7));
        envelope.Sign(Initial.AddMinutes(1), "ack-sha256:foo");
        envelope.Status.Should().Be(SignatureEnvelopeStatus.Signed);
        envelope.SnapshotJson.Should().Be(bytes);
        envelope.ContentSha256.Should().Be(checksum);
        envelope.SourceVersion.Should().Be(3);
    }

    [Fact]
    public void Decline_creates_dispute_without_erasing_recorded_hours()
    {
        var envelope = NewAttendance();
        envelope.Request(Initial, Initial.AddDays(7));
        envelope.Decline(Initial.AddMinutes(1), "Heures incorrectes");
        envelope.Status.Should().Be(SignatureEnvelopeStatus.Declined);
        envelope.ResponseComment.Should().Be("Heures incorrectes");
        envelope.SnapshotJson.Should().Contain("PresentMinutes");
    }

    [Fact]
    public void Refused_record_can_neither_be_confirmed_nor_re_requested()
    {
        var envelope = NewAttendance();
        envelope.Request(Initial, Initial.AddDays(7));
        envelope.Decline(Initial.AddMinutes(1), "A corriger");
        Action confirm = () => envelope.Sign(Initial.AddMinutes(2), "ack-sha256:bar");
        Action requestAgain = () => envelope.Request(Initial.AddMinutes(2), Initial.AddDays(8));
        confirm.Should().Throw<DomainException>();
        requestAgain.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expired_attendance_cannot_be_confirmed()
    {
        var envelope = NewAttendance();
        envelope.Request(Initial, Initial.AddMinutes(15));
        Action act = () => envelope.Sign(Initial.AddMinutes(16), "ack-sha256:foo");
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_EXPIRED");
    }

    [Fact]
    public void Already_signed_record_cannot_be_modified()
    {
        var envelope = NewAttendance();
        envelope.Request(Initial, Initial.AddDays(7));
        envelope.Sign(Initial.AddMinutes(1), "ack-sha256:foo");
        Action decline = () => envelope.Decline(Initial.AddMinutes(2), "New comment");
        decline.Should().Throw<DomainException>();
    }
}
