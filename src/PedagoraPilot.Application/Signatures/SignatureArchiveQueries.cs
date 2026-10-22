using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Signatures;

public sealed record SignatureArchiveAccessDto(string StorageKey, string Sha256, string ContentType, string FileName,
    Guid OrganizationId, Guid EnvelopeId);
public sealed record GetSignatureArchiveQuery(SignatureEnvelopeId Id, bool Manifest)
    : IQuery<SignatureArchiveAccessDto>;

/// <summary>Server-side authorization for EACH PDF and JSON download. No public object-storage URLs.</summary>
public sealed class GetSignatureArchiveQueryHandler(ISignatureEnvelopeRepository envelopes,
    ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ICohortTrainerAssignmentRepository assignments, ICurrentUser current)
    : IRequestHandler<GetSignatureArchiveQuery, SignatureArchiveAccessDto>
{
    public async Task<SignatureArchiveAccessDto> Handle(GetSignatureArchiveQuery request, CancellationToken ct)
    {
        var e = await envelopes.GetByIdAsync(request.Id, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        TenantScope.Ensure(current, e.OrganizationId);
        if (!current.UserId.HasValue) throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var self = e.SignerAuthGateUserId == current.UserId.Value;
        if (!self)
        {
            await ObservationSignatureAccess.EnsureStaffAccessAsync(e, current, cohorts, offerings, assignments, ct, false);
            if (!ObservationSignatureAccess.IsAuthor(e, current)
                && !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise)
                && e.Status != SignatureEnvelopeStatus.Signed)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        }
        else if (!current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        if (e.Status is not (SignatureEnvelopeStatus.Signed or SignatureEnvelopeStatus.Declined))
            throw new ConflictApplicationException("SIGNATURE_ARCHIVE_NOT_FINAL");
        var key = request.Manifest ? e.ArchiveManifestStorageKey : e.ArchivePdfStorageKey;
        var hash = request.Manifest ? e.ArchiveManifestSha256 : e.ArchivePdfSha256;
        if (!e.ArchivedAtUtc.HasValue || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(hash))
            throw new ConflictApplicationException("SIGNATURE_ARCHIVE_NOT_READY");
        var mime = request.Manifest ? "application/json" : "application/pdf";
        var file = $"pedagora-attestation-{e.Id.Value:D}.{(request.Manifest ? "json" : "pdf")}";
        return new SignatureArchiveAccessDto(key, hash, mime, file, e.OrganizationId, e.Id.Value);
    }
}
