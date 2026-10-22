using System.Security.Cryptography;
using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Application.Common.Errors;
using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Signatures;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;

/// <summary>
/// S14 drafts; S15 observations; S16 authenticated attendance acknowledgement.
/// Permanent proof/qualified signatures remain a separate S18 concern.
/// </summary>
[ApiController, Route("api/v1/signatures"), Authorize]
public sealed class SignaturesController(IMediator mediator, IObjectStorage storage, ILogger<SignaturesController> logger) : ControllerBase
{
    [HttpGet("{id:guid}/archive")]
    public Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct) => DownloadArchive(id, false, ct);

    [HttpGet("{id:guid}/evidence")]
    public Task<IActionResult> DownloadEvidence(Guid id, CancellationToken ct) => DownloadArchive(id, true, ct);

    private async Task<IActionResult> DownloadArchive(Guid id, bool manifest, CancellationToken ct)
    {
        var access = await mediator.Send(new GetSignatureArchiveQuery(new SignatureEnvelopeId(id), manifest), ct);
        await using var content = await storage.OpenReadAsync(access.StorageKey, ct);
        using var buffer = new MemoryStream();
        var limit = manifest ? 524_288 : 4_194_304;
        var chunk = new byte[8192];
        int read;
        while ((read = await content.ReadAsync(chunk.AsMemory(), ct)) != 0)
        {
            if (buffer.Length + read > limit) throw new InvalidOperationException("SIGNATURE_ARCHIVE_SIZE_INVALID");
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        var bytes = buffer.ToArray();
        var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(digest, access.Sha256, StringComparison.Ordinal))
        {
            logger.LogError("Signature archive integrity mismatch: {EnvelopeId}", access.EnvelopeId);
            throw new ConflictApplicationException(ErrorKeys.SignatureArchiveIntegrityFailed);
        }
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        logger.LogInformation("Private signature archive accessed: envelope {EnvelopeId}, organization {OrganizationId}, manifest {Manifest}",
            access.EnvelopeId, access.OrganizationId, manifest);
        return File(bytes, access.ContentType, access.FileName);
    }

    [HttpGet("center"), HasPermission(PedagoraPilotPermissionCodes.Signatures.View)]
    public Task<SignatureCenterPageDto> Center([FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] string? status = null, [FromQuery] Guid? cohortId = null,
        [FromQuery] bool team = false, CancellationToken ct = default)
        => mediator.Send(new ListSignatureCenterQuery(page, pageSize, status, cohortId, team), ct);

    [HttpPost("{id:guid}/remind"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)]
    public Task<SignatureEnvelopeDto> Remind(Guid id, CancellationToken ct)
        => mediator.Send(new RemindSignatureCommand(new SignatureEnvelopeId(id)), ct);

    [HttpPost("batch/request"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)]
    public Task<SignatureBatchResultDto> RequestBatch([FromBody] SignatureBatchRequest request, CancellationToken ct)
        => mediator.Send(new RequestSignatureBatchCommand((request.EnvelopeIds ?? Array.Empty<Guid>()).Select(id => new SignatureEnvelopeId(id)).ToArray()), ct);

    [HttpPost("observations/daily-summary"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)]
    public Task<SignatureEnvelopeDto> CreateDailySummary([FromBody] CreateDailyObservationSummaryRequest request, CancellationToken ct)
        => mediator.Send(new CreateDailyObservationSummaryCommand(new EnrollmentId(request.EnrollmentId),
            request.Date, request.EvaluationIds ?? Array.Empty<Guid>()), ct);

    [HttpPost("drafts"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)]
    public async Task<ActionResult<SignatureEnvelopeDto>> CreateDraft(
        [FromBody] CreateSignatureDraftRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateSignatureDraftCommand(request.SourceType, request.SourceId,
            new EnrollmentId(request.EnrollmentId)), ct);
        return Ok(dto); // idempotent: may return an already-existing draft
    }

    [HttpGet("drafts"), HasPermission(PedagoraPilotPermissionCodes.Signatures.View)]
    public Task<IReadOnlyCollection<SignatureEnvelopeDto>> ListDrafts([FromQuery] Guid? cohortId, CancellationToken ct)
        => mediator.Send(new ListMySignatureDraftsQuery(cohortId), ct);

    [HttpGet("drafts/{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Signatures.View)]
    public Task<SignatureEnvelopeDto> GetDraft(Guid id, CancellationToken ct)
        => mediator.Send(new GetSignatureDraftQuery(new SignatureEnvelopeId(id)), ct);
    // AuthGate protected. A URL is an identifier, never a bearer capability.
    [HttpGet("observations/my-pending")]
    public Task<IReadOnlyCollection<SignatureEnvelopeDto>> MyPendingObservations(CancellationToken ct)
        => mediator.Send(new ListMyPendingObservationsQuery(), ct);

    [HttpGet("observations/enrollment/{enrollmentId:guid}")]
    public Task<IReadOnlyCollection<SignatureEnvelopeDto>> ObservationHistory(Guid enrollmentId, CancellationToken ct)
        => mediator.Send(new ListEnrollmentObservationSignaturesQuery(new EnrollmentId(enrollmentId)), ct);

    [HttpGet("observations/{id:guid}/content")]
    public Task<SignatureContentDto> ObservationContent(Guid id, CancellationToken ct)
        => mediator.Send(new GetSignatureContentQuery(new SignatureEnvelopeId(id)), ct);

    [HttpPost("observations/{id:guid}/request"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)]
    public Task<SignatureEnvelopeDto> RequestObservation(Guid id, CancellationToken ct)
        => mediator.Send(new RequestObservationSignatureCommand(new SignatureEnvelopeId(id)), ct);

    [HttpPost("observations/{id:guid}/view"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign)]
    public Task<SignatureEnvelopeDto> MarkObservationViewed(Guid id, CancellationToken ct)
        => mediator.Send(new MarkSignatureViewedCommand(new SignatureEnvelopeId(id), false), ct);

    [HttpPost("observations/{id:guid}/respond")]
    public Task<SignatureEnvelopeDto> RespondObservation(Guid id,
        [FromBody] SignatureAcknowledgementRequest request, CancellationToken ct)
        => mediator.Send(new RespondObservationSignatureCommand(new SignatureEnvelopeId(id), request.Acknowledge, request.Comment), ct);
    [HttpGet("attendance/my")]
    public Task<IReadOnlyCollection<SignatureEnvelopeDto>> MyAttendance(CancellationToken ct)
        => mediator.Send(new ListMyAttendanceSignaturesQuery(), ct);

    [HttpGet("attendance/session/{sessionId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Signatures.View)]
    public Task<IReadOnlyCollection<SignatureEnvelopeDto>> SessionAttendance(Guid sessionId, CancellationToken ct)
        => mediator.Send(new ListSessionAttendanceSignaturesQuery(new TrainingSessionId(sessionId)), ct);

    [HttpGet("attendance/{id:guid}/content")]
    public Task<SignatureContentDto> AttendanceContent(Guid id, CancellationToken ct)
        => mediator.Send(new GetAttendanceSignatureContentQuery(new SignatureEnvelopeId(id)), ct);

    [HttpPost("attendance/{id:guid}/request"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)]
    public Task<SignatureEnvelopeDto> RequestAttendance(Guid id, CancellationToken ct)
        => mediator.Send(new RequestAttendanceSignatureCommand(new SignatureEnvelopeId(id)), ct);

    [HttpPost("attendance/{id:guid}/view"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign)]
    public Task<SignatureEnvelopeDto> MarkAttendanceViewed(Guid id, CancellationToken ct)
        => mediator.Send(new MarkSignatureViewedCommand(new SignatureEnvelopeId(id), true), ct);

    [HttpPost("attendance/{id:guid}/respond")]
    public Task<SignatureEnvelopeDto> RespondAttendance(Guid id,
        [FromBody] SignatureAcknowledgementRequest request, CancellationToken ct)
        => mediator.Send(new RespondAttendanceSignatureCommand(new SignatureEnvelopeId(id), request.Acknowledge, request.Comment), ct);

    [HttpGet("observations/daily"), HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)]
    public Task<IReadOnlyCollection<DailyObservationSignatureCandidateDto>> DailyObservations([FromQuery] DateOnly date, CancellationToken ct)
        => mediator.Send(new ListMyDailyObservationsQuery(date), ct);
}
