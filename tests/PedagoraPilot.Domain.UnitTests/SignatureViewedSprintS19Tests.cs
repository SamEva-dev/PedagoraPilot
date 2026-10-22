using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class SignatureViewedSprintS19Tests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private static SignatureEnvelope NewPending()
    {
        var envelope = SignatureEnvelope.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            EnrollmentId.New(), Guid.NewGuid(), SignatureSourceType.PedagogicalObservation,
            Guid.NewGuid(), 1, "{\"text\":\"observation\"}", Guid.NewGuid().ToString("D"), Start);
        envelope.Request(Start.AddMinutes(1), Start.AddDays(1));
        return envelope;
    }

    [Fact]
    public void First_view_transitions_pending_to_viewed_without_altering_frozen_snapshot()
    {
        var envelope = NewPending();
        var sha = envelope.ContentSha256;
        var version = envelope.Version;
        envelope.MarkViewed(Start.AddMinutes(2));
        Assert.Equal(SignatureEnvelopeStatus.Viewed, envelope.Status);
        Assert.Equal(Start.AddMinutes(2), envelope.ViewedAtUtc);
        Assert.Equal(version + 1, envelope.Version);
        Assert.Equal(sha, envelope.ContentSha256);
    }

    [Fact]
    public void Repeated_view_is_idempotent()
    {
        var envelope = NewPending();
        envelope.MarkViewed(Start.AddMinutes(2));
        var version = envelope.Version;
        envelope.MarkViewed(Start.AddMinutes(3));
        Assert.Equal(version, envelope.Version);
        Assert.Equal(Start.AddMinutes(2), envelope.ViewedAtUtc);
    }

    [Fact]
    public void Expired_or_signed_document_cannot_be_marked_viewed_again()
    {
        var expired = NewPending();
        Assert.Throws<DomainException>(() => expired.MarkViewed(Start.AddDays(2)));
        var signed = NewPending();
        signed.Sign(Start.AddMinutes(2), "ack-proof");
        Assert.Throws<DomainException>(() => signed.MarkViewed(Start.AddMinutes(3)));
    }
}
