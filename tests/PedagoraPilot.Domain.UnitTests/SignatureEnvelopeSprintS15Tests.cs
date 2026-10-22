using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class SignatureEnvelopeSprintS15Tests
{
    private static readonly DateTimeOffset Initial = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);
    private static SignatureEnvelope NewObservation() => SignatureEnvelope.CreateDraft(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EnrollmentId.New(), Guid.NewGuid(),
        SignatureSourceType.PedagogicalObservation, Guid.NewGuid(), 1, "{\"positive\":\"attention\"}",
        Guid.NewGuid().ToString("D"), Initial);

    [Fact]
    public void Cannot_acknowledge_before_request()
    {
        var envelope = NewObservation();
        Action act = () => envelope.Sign(Initial.AddMinutes(1), "ack-sha256:some-digest");
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_TRANSITION_FORBIDDEN");
    }

    [Fact]
    public void Acknowledgement_keeps_the_frozen_document_and_hash()
    {
        var envelope = NewObservation();
        var json = envelope.SnapshotJson;
        var hash = envelope.ContentSha256;
        envelope.Request(Initial.AddMinutes(1), Initial.AddDays(7));
        envelope.Sign(Initial.AddMinutes(2), "ack-sha256:some-digest");
        envelope.Status.Should().Be(SignatureEnvelopeStatus.Signed);
        envelope.SnapshotJson.Should().Be(json);
        envelope.ContentSha256.Should().Be(hash);
        envelope.SignedAtUtc.Should().Be(Initial.AddMinutes(2));
    }

    [Fact]
    public void Cannot_sign_twice()
    {
        var envelope = NewObservation();
        envelope.Request(Initial, Initial.AddDays(7));
        envelope.Sign(Initial.AddMinutes(1), "ack-sha256:some-digest");
        Action act = () => envelope.Sign(Initial.AddMinutes(2), "ack-sha256:another-digest");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Refusal_remains_traceable_without_deleting_observation()
    {
        var envelope = NewObservation();
        envelope.Request(Initial, Initial.AddDays(7));
        envelope.Decline(Initial.AddMinutes(1), "Je conteste ce point");
        envelope.Status.Should().Be(SignatureEnvelopeStatus.Declined);
        envelope.ResponseComment.Should().Be("Je conteste ce point");
        envelope.SnapshotJson.Should().Contain("attention");
    }

    [Fact]
    public void Expired_requests_cannot_be_acknowledged()
    {
        var envelope = NewObservation();
        envelope.Request(Initial, Initial.AddMinutes(20));
        Action act = () => envelope.Sign(Initial.AddMinutes(21), "ack-sha256:some-digest");
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_EXPIRED");
    }

    [Fact]
    public void Pending_document_cannot_be_sent_again_as_new_request()
    {
        var envelope = NewObservation();
        envelope.Request(Initial, Initial.AddDays(7));
        Action act = () => envelope.Request(Initial.AddMinutes(2), Initial.AddDays(8));
        act.Should().Throw<DomainException>();
    }
}
