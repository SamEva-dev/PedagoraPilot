using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Signatures;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Signatures;

/// <summary>
/// Only the intended AuthGate signer may mark a delivered document as viewed.
/// Idempotent for repeated opening while it is still valid; never changes signed/refused records.
/// The standard command UnitOfWork behavior persists the tracked aggregate.
/// </summary>
public sealed class MarkSignatureViewedCommandHandler(
    ISignatureEnvelopeRepository envelopes, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<MarkSignatureViewedCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(MarkSignatureViewedCommand request, CancellationToken ct)
    {
        var envelope = await envelopes.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        TenantScope.Ensure(current, envelope.OrganizationId);
        if (!current.UserId.HasValue || envelope.SignerAuthGateUserId != current.UserId.Value
            || !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var validSource = request.Attendance
            ? envelope.SourceType == SignatureSourceType.Attendance
            : envelope.SourceType is SignatureSourceType.PedagogicalObservation
                or SignatureSourceType.DailyObservationSummary;
        if (!validSource)
            throw new ValidationApplicationException(ErrorKeys.SignatureSourceInvalid);
        if (envelope.Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed))
            throw new ConflictApplicationException(ErrorKeys.SignatureTransitionForbidden);
        var now = DateTimeOffset.UtcNow;
        if (envelope.ExpiresAtUtc <= now)
            throw new ConflictApplicationException(ErrorKeys.SignatureExpired);
        envelope.MarkViewed(now);
        return SignatureDtoFactory.ToDto(envelope, mapper);
    }
}
