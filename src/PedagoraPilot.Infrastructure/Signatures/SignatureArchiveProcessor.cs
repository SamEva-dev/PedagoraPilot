using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Application.Signatures;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Infrastructure.Signatures;

/// <summary>
/// Driven by committed outbox events, NEVER by an uncommitted HTTP handler.
/// Content-addressed private keys make a replay harmless. The DB row is the sole published pointer.
/// If S3/MinIO is unavailable the outbox retries and the download endpoint returns 409.
/// </summary>
public sealed class SignatureArchiveProcessor(ISignatureEnvelopeRepository envelopes, IObjectStorage storage,
    IUnitOfWork unitOfWork, SignaturePdfRenderer pdfRenderer, ILogger<SignatureArchiveProcessor> logger)
    : ISignatureArchiveProcessor
{
    public async Task ArchiveAsync(Guid id, CancellationToken ct)
    {
        var envelope = await envelopes.GetByIdAsync(new SignatureEnvelopeId(id), true, ct)
            ?? throw new InvalidOperationException("SIGNATURE_NOT_FOUND");
        if (envelope.Status is not (SignatureEnvelopeStatus.Signed or SignatureEnvelopeStatus.Declined))
            return;
        if (envelope.ArchivedAtUtc.HasValue) return;

        // Verify source-snapshot integrity before writing any artifact.
        var snapshotDigest = Sha(Encoding.UTF8.GetBytes(envelope.SnapshotJson));
        if (!string.Equals(snapshotDigest, envelope.ContentSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("SIGNATURE_SNAPSHOT_HASH_MISMATCH");
        var pdf = pdfRenderer.Render(envelope);
        if (pdf.Length is < 100 or > 4_194_304) throw new InvalidOperationException("SIGNATURE_PDF_INVALID");
        var pdfHash = Sha(pdf);
        var baseKey = $"signature-evidence/{envelope.OrganizationId:D}/{envelope.Id.Value:D}";
        var pdfKey = $"{baseKey}/{pdfHash}.pdf";
        var respondedAt = envelope.SignedAtUtc ?? envelope.DeclinedAtUtc;
        var manifest = new
        {
            Schema = "pedagora-signature-evidence-v1", EnvelopeId = envelope.Id.Value,
            envelope.OrganizationId, envelope.SiteId, envelope.CohortId,
            EnrollmentId = envelope.EnrollmentId.Value,
            envelope.SourceType, envelope.SourceId, envelope.SourceVersion,
            envelope.SignerAuthGateUserId, envelope.RequestedByUserId,
            Status = envelope.Status.ToString(), envelope.CreatedAtUtc, envelope.RequestedAtUtc,
            envelope.ViewedAtUtc, RespondedAtUtc = respondedAt,
            envelope.ResponseComment, envelope.EvidenceReference,
            SnapshotJson = envelope.SnapshotJson, SnapshotSha256 = envelope.ContentSha256,
            PdfSha256 = pdfHash, PdfStorageKey = pdfKey,
            SignatureLevel = "Authenticated acknowledgement; not an eIDAS qualified signature"
        };
        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
        var manifestHash = Sha(manifestBytes);
        var manifestKey = $"{baseKey}/{manifestHash}.json";

        await WriteIfMissingAndVerifyAsync(pdfKey, pdf, "application/pdf", ct);
        await WriteIfMissingAndVerifyAsync(manifestKey, manifestBytes, "application/json", ct);
        envelope.RegisterArchive(pdfKey, pdfHash, manifestKey, manifestHash, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(ct); // Optimistic concurrency protects the single immutable pointer.
        logger.LogInformation("Signature archive persisted for envelope {EnvelopeId} in organization {OrganizationId}", id, envelope.OrganizationId);
    }

    private async Task WriteIfMissingAndVerifyAsync(string key, byte[] bytes, string mime, CancellationToken ct)
    {
        if (!await storage.ExistsAsync(key, ct))
        {
            using var source = new MemoryStream(bytes, writable: false);
            await storage.PutAsync(key, source, mime, ct);
        }
        await using var stored = await storage.OpenReadAsync(key, ct);
        var actual = await SHA256.HashDataAsync(stored, ct);
        if (!CryptographicOperations.FixedTimeEquals(actual, SHA256.HashData(bytes)))
            throw new InvalidOperationException("SIGNATURE_ARCHIVE_STORAGE_CORRUPTED");
    }

    private static string Sha(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
}
