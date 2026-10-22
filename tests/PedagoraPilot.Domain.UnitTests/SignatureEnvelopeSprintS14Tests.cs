using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class SignatureEnvelopeSprintS14Tests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);

    private static SignatureEnvelope Draft(string snapshot = "{\"remark\":\"Priorité à droite\"}") =>
        SignatureEnvelope.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            EnrollmentId.New(), Guid.NewGuid(), SignatureSourceType.PedagogicalObservation,
            Guid.NewGuid(), 1, snapshot, Guid.NewGuid().ToString("D"), Now);

    [Fact]
    public void Draft_is_not_a_signed_or_sent_document()
    {
        var item = Draft();
        item.Status.Should().Be(SignatureEnvelopeStatus.Draft);
        item.RequestedAtUtc.Should().BeNull();
        item.SignedAtUtc.Should().BeNull();
        item.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Hash_matches_exact_original_snapshot_bytes()
    {
        var item = Draft();
        var expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(item.SnapshotJson))).ToLowerInvariant();
        item.ContentSha256.Should().Be(expected);
    }

    [Fact]
    public void Identical_text_and_revision_are_immutable_through_lifecycle()
    {
        var item = Draft();
        var originalJson = item.SnapshotJson;
        var hash = item.ContentSha256;
        item.Request(Now.AddMinutes(1), Now.AddDays(1));
        item.MarkViewed(Now.AddMinutes(2));
        item.Sign(Now.AddMinutes(3), "private/evidence/envelope/sha256");
        item.Status.Should().Be(SignatureEnvelopeStatus.Signed);
        item.SnapshotJson.Should().Be(originalJson);
        item.ContentSha256.Should().Be(hash);
        item.DomainEvents.Should().HaveCount(3);
    }

    [Fact]
    public void Signature_after_expiration_is_refused()
    {
        var item = Draft();
        item.Request(Now, Now.AddMinutes(10));
        Action act = () => item.Sign(Now.AddMinutes(10), "private/proof");
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_EXPIRED");
    }

    [Fact]
    public void Signed_document_cannot_be_cancelled_or_declined()
    {
        var item = Draft();
        item.Request(Now, Now.AddHours(1));
        item.Sign(Now.AddMinutes(1), "private/proof");
        Action cancel = () => item.Cancel(Now.AddMinutes(2));
        Action decline = () => item.Decline(Now.AddMinutes(2), "Désaccord");
        cancel.Should().Throw<DomainException>();
        decline.Should().Throw<DomainException>();
    }

    [Fact]
    public void Decline_does_not_delete_the_trainer_observations()
    {
        var item = Draft();
        item.Request(Now, Now.AddHours(1));
        item.Decline(Now.AddMinutes(1), "Je conteste le contenu");
        item.Status.Should().Be(SignatureEnvelopeStatus.Declined);
        item.ResponseComment.Should().Be("Je conteste le contenu");
        item.ContentSha256.Should().HaveLength(64);
    }

    [Fact]
    public void Sending_with_expiry_before_request_is_refused()
    {
        var item = Draft();
        Action act = () => item.Request(Now.AddDays(1), Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Retroactive_acknowledgement_is_refused()
    {
        var item = Draft();
        item.Request(Now, Now.AddHours(1));
        Action act = () => item.Sign(Now.AddMinutes(-1), "proof");
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_TIME_INVALID");
    }

    [Fact]
    public void Unlinked_signer_is_rejected_in_domain()
    {
        Action act = () => SignatureEnvelope.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            EnrollmentId.New(), Guid.Empty, SignatureSourceType.Attendance, Guid.NewGuid(), 1,
            "{}", "trainer", Now);
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_SIGNER_REQUIRED");
    }

    [Fact]
    public void Unknown_document_type_is_rejected()
    {
        Action act = () => SignatureEnvelope.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            EnrollmentId.New(), Guid.NewGuid(), (SignatureSourceType)999, Guid.NewGuid(), 1,
            "{}", "trainer", Now);
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_SOURCE_INVALID");
    }

    [Fact]
    public void S14_does_not_allow_direct_signing_of_a_draft()
    {
        var item = Draft();
        Action act = () => item.Sign(Now, "proof");
        act.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_TRANSITION_FORBIDDEN");
    }
}
