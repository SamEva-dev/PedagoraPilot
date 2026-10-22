using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class SignatureEnvelopeSprintS18Tests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 10, 7, 0, 0, TimeSpan.Zero);
    private const string DigestA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string DigestB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static SignatureEnvelope Draft() => SignatureEnvelope.CreateDraft(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EnrollmentId.New(), Guid.NewGuid(),
        SignatureSourceType.PedagogicalObservation, Guid.NewGuid(), 1,
        "{\"subject\":\"priorités\"}", Guid.NewGuid().ToString("D"), T0);

    [Fact]
    public void Unanswered_documents_cannot_be_archived()
    {
        var draft = Draft();
        Action save = () => draft.RegisterArchive("private/doc.pdf", DigestA, "private/proof.json", DigestB, T0.AddDays(1));
        save.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_ARCHIVE_NOT_FINAL");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Responses_are_frozen_with_one_idempotent_archive(bool sign)
    {
        var doc = Draft();
        doc.Request(T0, T0.AddDays(7));
        if (sign) doc.Sign(T0.AddHours(2), "ack-sha256:proof");
        else doc.Decline(T0.AddHours(2), "Horaire contesté");
        var content = doc.SnapshotJson;
        var digest = doc.ContentSha256;
        var versionBefore = doc.Version;
        doc.RegisterArchive("private/file.pdf", DigestA, "private/proof.json", DigestB, T0.AddHours(3));
        doc.ArchivePdfSha256.Should().Be(DigestA);
        doc.ArchiveManifestSha256.Should().Be(DigestB);
        doc.ArchivedAtUtc.Should().Be(T0.AddHours(3));
        doc.Version.Should().Be(versionBefore + 1);
        doc.RegisterArchive("private/file.pdf", DigestA, "private/proof.json", DigestB, T0.AddDays(1));
        doc.Version.Should().Be(versionBefore + 1);
        doc.SnapshotJson.Should().Be(content);
        doc.ContentSha256.Should().Be(digest);
    }

    [Fact]
    public void An_archived_document_cannot_be_replaced()
    {
        var doc = Draft();
        doc.Request(T0, T0.AddDays(7));
        doc.Sign(T0.AddHours(1), "ack-sha256:proof");
        doc.RegisterArchive("private/file.pdf", DigestA, "private/proof.json", DigestB, T0.AddHours(2));
        Action change = () => doc.RegisterArchive("private/other.pdf", DigestA, "private/proof.json", DigestB, T0.AddHours(3));
        change.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_ARCHIVE_IMMUTABLE");
    }

    [Fact]
    public void Invalid_checksum_is_rejected()
    {
        var doc = Draft();
        doc.Request(T0, T0.AddDays(7));
        doc.Sign(T0.AddHours(1), "ack-sha256:proof");
        Action invalid = () => doc.RegisterArchive("private/file.pdf", "not-a-sha", "private/proof.json", DigestB, T0.AddHours(2));
        invalid.Should().Throw<DomainException>().Where(x => x.ErrorKey == "SIGNATURE_ARCHIVE_INVALID");
    }
}
