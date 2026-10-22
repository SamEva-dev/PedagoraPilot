using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Delivery;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Signatures;

internal static class AttendanceSignatureAccess
{
    internal static void RequireAttendance(SignatureEnvelope envelope)
    {
        if (envelope.SourceType != SignatureSourceType.Attendance)
            throw new ValidationApplicationException(ErrorKeys.SignatureSourceInvalid);
    }

    internal static bool IsSigner(SignatureEnvelope envelope, ICurrentUser current) =>
        current.UserId.HasValue && envelope.SignerAuthGateUserId == current.UserId.Value;

    internal static bool IsAuthor(SignatureEnvelope envelope, ICurrentUser current) =>
        current.UserId.HasValue && string.Equals(envelope.RequestedByUserId,
            current.UserId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase);

    internal static Task EnsureStaffAsync(SignatureEnvelope envelope, ICurrentUser current,
        ICohortRepository cohorts, IProgramOfferingRepository offerings,
        ICohortTrainerAssignmentRepository assignments, CancellationToken ct, bool write) =>
        ObservationSignatureAccess.EnsureStaffAccessAsync(envelope, current, cohorts, offerings, assignments, ct, write);

    internal static void EnsureSignable(TrainingSession session, AttendanceSheet sheet, SignatureEnvelope envelope, DateTimeOffset now)
    {
        if (session.OrganizationId != envelope.OrganizationId || session.SiteId != envelope.SiteId ||
            session.CohortId.Value != envelope.CohortId || session.Id.Value != envelope.SourceId ||
            sheet.OrganizationId != envelope.OrganizationId || sheet.CohortId.Value != envelope.CohortId ||
            sheet.SessionId != session.Id)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        if (session.Status == TrainingSessionStatus.Cancelled || session.EndsAtUtc > now ||
            session.Status is not (TrainingSessionStatus.InProgress or TrainingSessionStatus.Completed))
            throw new ConflictApplicationException(ErrorKeys.SignatureAttendanceSessionNotFinished);
        if (sheet.Version != envelope.SourceVersion)
            throw new ConflictApplicationException(ErrorKeys.SignatureAttendanceChanged);
        if (session.AudienceMode == SessionAudienceMode.SelectedEnrollments &&
            !session.Participants.Any(x => x.EnrollmentId == envelope.EnrollmentId))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var entry = sheet.Entries.SingleOrDefault(x => x.EnrollmentId == envelope.EnrollmentId);
        if (entry is null || entry.Status is not (AttendanceStatus.Present or AttendanceStatus.Late) || entry.PresentMinutes <= 0)
            throw new ConflictApplicationException(ErrorKeys.SignatureAttendanceNotSignable);
        // The sheet may not have changed but the session schedule can still be edited.
        // Never request a signature for a stale snapshot with different hours or status.
        try
        {
            using var document = JsonDocument.Parse(envelope.SnapshotJson);
            var data = document.RootElement;
            if (data.GetProperty("SessionId").GetGuid() != session.Id.Value ||
                data.GetProperty("EnrollmentId").GetGuid() != envelope.EnrollmentId.Value ||
                data.GetProperty("StartsAtUtc").GetDateTimeOffset() != session.StartsAtUtc ||
                data.GetProperty("EndsAtUtc").GetDateTimeOffset() != session.EndsAtUtc ||
                data.GetProperty("Status").GetString() != entry.Status.ToString() ||
                data.GetProperty("PresentMinutes").GetInt32() != entry.PresentMinutes ||
                data.GetProperty("MissedMinutes").GetInt32() != entry.MissedMinutes ||
                !SameOptionalInstant(data.GetProperty("ArrivalAtUtc"), entry.ArrivalAtUtc) ||
                !SameOptionalInstant(data.GetProperty("DepartureAtUtc"), entry.DepartureAtUtc))
                throw new ConflictApplicationException(ErrorKeys.SignatureAttendanceChanged);
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new ConflictApplicationException(ErrorKeys.SignatureAttendanceChanged);
        }
    }

    private static bool SameOptionalInstant(JsonElement item, DateTimeOffset? instant) =>
        item.ValueKind == JsonValueKind.Null ? !instant.HasValue :
        instant.HasValue && item.GetDateTimeOffset() == instant.Value;
}

/// <summary>Requests an individual attendance acknowledgement for a frozen, persisted sheet version.</summary>
public sealed class RequestAttendanceSignatureCommandHandler(
    ISignatureEnvelopeRepository repository, ITrainingSessionRepository sessions,
    IAttendanceSheetRepository sheets, IEnrollmentRepository enrollments,
    ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ICohortTrainerAssignmentRepository assignments, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<RequestAttendanceSignatureCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(RequestAttendanceSignatureCommand request, CancellationToken ct)
    {
        var envelope = await repository.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        AttendanceSignatureAccess.RequireAttendance(envelope);
        await AttendanceSignatureAccess.EnsureStaffAsync(envelope, current, cohorts, offerings, assignments, ct, true);
        if (!AttendanceSignatureAccess.IsAuthor(envelope, current) &&
            !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        if (envelope.Status == SignatureEnvelopeStatus.Draft)
        {
            var now = DateTimeOffset.UtcNow;
            var session = await sessions.GetByIdAsync(new TrainingSessionId(envelope.SourceId), false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
            var sheet = await sheets.GetBySessionIdAsync(session.Id, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.AttendanceNotFound);
            AttendanceSignatureAccess.EnsureSignable(session, sheet, envelope, now);
            var enrollment = await enrollments.GetByIdAsync(envelope.EnrollmentId, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
            if (enrollment.OrganizationId != envelope.OrganizationId || enrollment.CohortId.Value != envelope.CohortId)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            if (enrollment.Status is EnrollmentStatus.Pending or EnrollmentStatus.Withdrawn or EnrollmentStatus.Cancelled)
                throw new ConflictApplicationException(ErrorKeys.SignatureEnrollmentNotEligible);
            envelope.Request(now, now.AddDays(7)); // DomainEvent -> transaction Outbox -> email
        }
        else if (envelope.Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed))
            throw new ConflictApplicationException(ErrorKeys.SignatureTransitionForbidden);
        return SignatureDtoFactory.ToDto(envelope, mapper);
    }
}

/// <summary>Only the exact linked AuthGate account may attest or contest its own attendance.</summary>
public sealed class RespondAttendanceSignatureCommandHandler(
    ISignatureEnvelopeRepository repository, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<RespondAttendanceSignatureCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(RespondAttendanceSignatureCommand request, CancellationToken ct)
    {
        var envelope = await repository.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        AttendanceSignatureAccess.RequireAttendance(envelope);
        TenantScope.Ensure(current, envelope.OrganizationId);
        if (!AttendanceSignatureAccess.IsSigner(envelope, current) ||
            !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var now = DateTimeOffset.UtcNow;
        if (envelope.Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed))
            throw new ConflictApplicationException(ErrorKeys.SignatureTransitionForbidden);
        if (envelope.ExpiresAtUtc <= now)
            throw new ConflictApplicationException(ErrorKeys.SignatureExpired);
        if (request.Acknowledge)
        {
            var payload = $"attendance|{envelope.Id.Value:N}|{envelope.ContentSha256}|{current.UserId!.Value:N}|{now:O}|{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
            var evidence = "ack-sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            envelope.Sign(now, evidence);
        }
        else
        {
            // Disagreement is a contestation, NEVER a deletion or mutation of attendance time.
            envelope.Decline(now, request.Comment);
        }
        return SignatureDtoFactory.ToDto(envelope, mapper);
    }
}

public sealed class GetAttendanceSignatureContentQueryHandler(
    ISignatureEnvelopeRepository repository, ICohortRepository cohorts,
    IProgramOfferingRepository offerings, ICohortTrainerAssignmentRepository assignments,
    ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<GetAttendanceSignatureContentQuery, SignatureContentDto>
{
    public async Task<SignatureContentDto> Handle(GetAttendanceSignatureContentQuery request, CancellationToken ct)
    {
        var envelope = await repository.GetByIdAsync(request.Id, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        AttendanceSignatureAccess.RequireAttendance(envelope);
        TenantScope.Ensure(current, envelope.OrganizationId);
        var self = AttendanceSignatureAccess.IsSigner(envelope, current);
        if (!self)
        {
            await AttendanceSignatureAccess.EnsureStaffAsync(envelope, current, cohorts, offerings, assignments, ct, false);
            if (!SignatureVisibilityPolicy.CanReadContent(false,
                    AttendanceSignatureAccess.IsAuthor(envelope, current),
                    current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise), envelope.Status))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        }
        return new SignatureContentDto(SignatureDtoFactory.ToDto(envelope, mapper), envelope.SnapshotJson,
            envelope.ExpiresAtUtc, envelope.ViewedAtUtc, envelope.ResponseComment,
            self && current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign)
                && envelope.Status is (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed)
                && envelope.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }
}

public sealed class ListMyAttendanceSignaturesQueryHandler(
    ISignatureEnvelopeRepository repository, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<ListMyAttendanceSignaturesQuery, IReadOnlyCollection<SignatureEnvelopeDto>>
{
    public async Task<IReadOnlyCollection<SignatureEnvelopeDto>> Handle(ListMyAttendanceSignaturesQuery request, CancellationToken ct)
    {
        if (!current.UserId.HasValue) throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var org = TenantScope.Organization(current);
        if (!org.HasValue) throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var rows = await repository.Query(false).Where(x => x.OrganizationId == org.Value
            && x.SignerAuthGateUserId == current.UserId.Value && x.SourceType == SignatureSourceType.Attendance
            && x.Status != SignatureEnvelopeStatus.Draft)
            .OrderByDescending(x => x.RequestedAtUtc).Take(100).ToListAsync(ct);
        return rows.Select(x => SignatureDtoFactory.ToDto(x, mapper)).ToArray();
    }
}

public sealed class ListSessionAttendanceSignaturesQueryHandler(
    ISignatureEnvelopeRepository repository, ITrainingSessionRepository sessions, IAttendanceSheetRepository sheets,
    ICohortRepository cohorts, IProgramOfferingRepository offerings, ICohortTrainerAssignmentRepository assignments,
    ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<ListSessionAttendanceSignaturesQuery, IReadOnlyCollection<SignatureEnvelopeDto>>
{
    public async Task<IReadOnlyCollection<SignatureEnvelopeDto>> Handle(ListSessionAttendanceSignaturesQuery request, CancellationToken ct)
    {
        if (!current.UserId.HasValue || !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.View))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var session = await sessions.GetByIdAsync(request.SessionId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
        TenantScope.Ensure(current, session.OrganizationId);
        var cohort = await cohorts.GetByIdAsync(session.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.OrganizationId != session.OrganizationId || cohort.SiteId != session.SiteId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
        var isTrainer = string.Equals(session.TrainerAuthGateUserId, current.UserId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase);
        var supervisor = current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise);
        var assignment = await assignments.GetByUserAndCohortAsync(session.OrganizationId, current.UserId.Value, cohort.Id, false, ct);
        if (!isTrainer && !supervisor && assignment?.IsActive != true)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        // The active signing queue is tied to the current saved sheet revision.
        // Historic signed documents remain accessible by their immutable identifiers.
        var sheet = await sheets.GetBySessionIdAsync(session.Id, false, ct);
        if (sheet is null) return Array.Empty<SignatureEnvelopeDto>();
        var rows = await repository.Query(false).Where(x => x.OrganizationId == session.OrganizationId
            && x.SourceType == SignatureSourceType.Attendance && x.SourceId == session.Id.Value
            && x.SourceVersion == sheet.Version)
            .OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(ct);
        return rows.Where(x => isTrainer && AttendanceSignatureAccess.IsAuthor(x, current) ||
                supervisor || x.Status == SignatureEnvelopeStatus.Signed && assignment?.IsActive == true)
            .Select(x => SignatureDtoFactory.ToDto(x, mapper)).ToArray();
    }
}
