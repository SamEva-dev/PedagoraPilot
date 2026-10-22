using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class SignatureEnvelopeSprintS17Tests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 10, 7, 0, 0, TimeSpan.Zero);
    private static SignatureEnvelope Draft(SignatureSourceType type = SignatureSourceType.DailyObservationSummary) =>
        SignatureEnvelope.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EnrollmentId.New(),
            Guid.NewGuid(), type, Guid.NewGuid(), 1,
            "{\"Type\":\"daily-observation-summary\",\"Observations\":[{\"Subject\":\"Maîtrise\"}]}",
            Guid.NewGuid().ToString("D"), Start);

    [Fact]
    public void Reminder_requires_an_already_requested_envelope()
    {
        var envelope = Draft();
        Action action = () => envelope.Remind(Start.AddDays(1));
        action.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_TRANSITION_FORBIDDEN");
    }

    [Fact]
    public void Reminder_cannot_be_sent_during_first_24_hours()
    {
        var envelope = Draft();
        envelope.Request(Start, Start.AddDays(7));
        Action action = () => envelope.Remind(Start.AddHours(23));
        action.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_REMINDER_RATE_LIMITED");
    }

    [Fact]
    public void Reminder_keeps_snapshot_hash_source_and_signature_status()
    {
        var envelope = Draft();
        envelope.Request(Start, Start.AddDays(7));
        var snapshot = envelope.SnapshotJson;
        var hash = envelope.ContentSha256;
        var id = envelope.SourceId;
        envelope.Remind(Start.AddHours(24));
        envelope.Status.Should().Be(SignatureEnvelopeStatus.Pending);
        envelope.ReminderCount.Should().Be(1);
        envelope.LastReminderAtUtc.Should().Be(Start.AddHours(24));
        envelope.SnapshotJson.Should().Be(snapshot);
        envelope.ContentSha256.Should().Be(hash);
        envelope.SourceId.Should().Be(id);
    }

    [Fact]
    public void Reminder_is_rate_limited_even_when_status_is_viewed()
    {
        var envelope = Draft();
        envelope.Request(Start, Start.AddDays(7));
        envelope.MarkViewed(Start.AddHours(1));
        envelope.Remind(Start.AddHours(24));
        Action again = () => envelope.Remind(Start.AddHours(25));
        again.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_REMINDER_RATE_LIMITED");
    }

    [Fact]
    public void Three_reminders_is_hard_limit()
    {
        var envelope = Draft();
        envelope.Request(Start, Start.AddDays(10));
        envelope.Remind(Start.AddHours(24));
        envelope.Remind(Start.AddHours(48));
        envelope.Remind(Start.AddHours(72));
        Action fourth = () => envelope.Remind(Start.AddHours(96));
        fourth.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_REMINDER_RATE_LIMITED");
    }

    [Fact]
    public void Cannot_remind_after_signing()
    {
        var envelope = Draft();
        envelope.Request(Start, Start.AddDays(7));
        envelope.Sign(Start.AddMinutes(10), "ack-sha256:test");
        Action action = () => envelope.Remind(Start.AddDays(2));
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cannot_remind_after_expiry()
    {
        var envelope = Draft();
        envelope.Request(Start, Start.AddDays(2));
        Action action = () => envelope.Remind(Start.AddDays(3));
        action.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_EXPIRED");
    }

    [Fact]
    public void Recap_can_be_acknowledged_without_approving_its_observations()
    {
        var envelope = Draft();
        envelope.Request(Start, Start.AddDays(7));
        envelope.Sign(Start.AddHours(2), "ack-sha256:test");
        envelope.Status.Should().Be(SignatureEnvelopeStatus.Signed);
        envelope.SnapshotJson.Should().Contain("Maîtrise");
    }
}
