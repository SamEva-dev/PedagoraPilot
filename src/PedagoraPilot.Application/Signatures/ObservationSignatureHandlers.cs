using System.Security.Cryptography;
using System.Text;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Signatures;

internal static class ObservationSignatureAccess
{
    internal static bool IsOwner(SignatureEnvelope envelope, ICurrentUser current) =>
        current.UserId.HasValue && envelope.SignerAuthGateUserId == current.UserId.Value;

    internal static bool IsAuthor(SignatureEnvelope envelope, ICurrentUser current) =>
        current.UserId.HasValue && string.Equals(envelope.RequestedByUserId,
            current.UserId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase);

    internal static async Task EnsureStaffAccessAsync(SignatureEnvelope envelope, ICurrentUser current,
        ICohortRepository cohorts, IProgramOfferingRepository offerings,
        ICohortTrainerAssignmentRepository assignments, CancellationToken ct, bool write)
    {
        if (!current.UserId.HasValue || !current.HasPermission(write
                ? PedagoraPilotPermissionCodes.Signatures.Request : PedagoraPilotPermissionCodes.Signatures.View))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        TenantScope.Ensure(current, envelope.OrganizationId);
        var cohort = await cohorts.GetByIdAsync(new CohortId(envelope.CohortId), false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.OrganizationId != envelope.OrganizationId || cohort.SiteId != envelope.SiteId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        if (write) await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        else
        {
            var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
            if (current.HasContextualScopeRestrictions
                && !current.CanViewCohort(cohort.SiteId, offering.ProgramId, cohort.Id.Value))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            if (!IsAuthor(envelope, current)
                && !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise))
            {
                var assignment = await assignments.GetByUserAndCohortAsync(envelope.OrganizationId,
                    current.UserId!.Value, cohort.Id, false, ct);
                if (assignment?.IsActive != true)
                    throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            }
        }
    }

    internal static void EnsureObservation(SignatureEnvelope envelope)
    {
        if (envelope.SourceType is not (SignatureSourceType.PedagogicalObservation or SignatureSourceType.DailyObservationSummary))
            throw new ValidationApplicationException(ErrorKeys.SignatureSourceInvalid);
    }
}

public sealed class RequestObservationSignatureCommandHandler(ISignatureEnvelopeRepository repository,
    IDrivingEvaluationRepository evaluations,
    ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ICohortTrainerAssignmentRepository assignments, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<RequestObservationSignatureCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(RequestObservationSignatureCommand request, CancellationToken ct)
    {
        var envelope = await repository.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        ObservationSignatureAccess.EnsureObservation(envelope);
        await ObservationSignatureAccess.EnsureStaffAccessAsync(envelope, current, cohorts, offerings, assignments, ct, true);
        if (!ObservationSignatureAccess.IsAuthor(envelope, current)
            && !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        if (envelope.Status == SignatureEnvelopeStatus.Draft)
        {
            if (envelope.SourceType == SignatureSourceType.DailyObservationSummary)
            {
                // Prevent a prepared daily recap from being sent after any constituent evaluation changes.
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(envelope.SnapshotJson);
                    var parts = doc.RootElement.GetProperty("Observations").EnumerateArray()
                        .Select(e => new { Id = e.GetProperty("EvaluationId").GetGuid(), Version = e.GetProperty("Version").GetInt64() }).ToArray();
                    if (parts.Length is < 1 or > 20 || parts.Select(x => x.Id).Distinct().Count() != parts.Length)
                        throw new ConflictApplicationException("SIGNATURE_DAILY_SUMMARY_CHANGED");
                    var ids = parts.Select(x => new DrivingEvaluationId(x.Id)).ToArray();
                    var records = await evaluations.Query(false).Where(e => e.OrganizationId == envelope.OrganizationId
                        && e.EnrollmentId == envelope.EnrollmentId && ids.Contains(e.Id)).ToListAsync(ct);
                    if (records.Count != parts.Length || parts.Any(p => records.All(e => e.Id.Value != p.Id || e.Version != p.Version)))
                        throw new ConflictApplicationException("SIGNATURE_DAILY_SUMMARY_CHANGED");
                }
                catch (Exception error) when (error is System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException)
                {
                    throw new ConflictApplicationException("SIGNATURE_DAILY_SUMMARY_CHANGED");
                }
            }
            var now = DateTimeOffset.UtcNow;
            envelope.Request(now, now.AddDays(7));
        }
        else if (envelope.Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed))
            throw new ConflictApplicationException(ErrorKeys.SignatureTransitionForbidden);
        return SignatureDtoFactory.ToDto(envelope, mapper);
    }
}

/// <summary>Only the exact AuthGate identity of the signer may acknowledge or decline.</summary>
public sealed class RespondObservationSignatureCommandHandler(ISignatureEnvelopeRepository repository,
    ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<RespondObservationSignatureCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(RespondObservationSignatureCommand request, CancellationToken ct)
    {
        var envelope = await repository.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        ObservationSignatureAccess.EnsureObservation(envelope);
        TenantScope.Ensure(current, envelope.OrganizationId);
        if (!ObservationSignatureAccess.IsOwner(envelope, current) ||
            !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var now = DateTimeOffset.UtcNow;
        if (envelope.ExpiresAtUtc.HasValue && now >= envelope.ExpiresAtUtc.Value)
            throw new ConflictApplicationException(ErrorKeys.SignatureExpired);
        if (envelope.Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed))
            throw new ConflictApplicationException(ErrorKeys.SignatureTransitionForbidden);
        if (request.Acknowledge)
        {
            // The reference is an integrity/audit pointer, NOT a qualified eIDAS signature certificate.
            var payload = $"{envelope.Id.Value:N}|{envelope.ContentSha256}|{current.UserId!.Value:N}|{now:O}|{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
            var evidence = "ack-sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            envelope.Sign(now, evidence);
        }
        else envelope.Decline(now, request.Comment);
        return SignatureDtoFactory.ToDto(envelope, mapper);
    }
}

public sealed class GetSignatureContentQueryHandler(ISignatureEnvelopeRepository repository,
    ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ICohortTrainerAssignmentRepository assignments, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<GetSignatureContentQuery, SignatureContentDto>
{
    public async Task<SignatureContentDto> Handle(GetSignatureContentQuery request, CancellationToken ct)
    {
        var envelope = await repository.GetByIdAsync(request.Id, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        ObservationSignatureAccess.EnsureObservation(envelope);
        TenantScope.Ensure(current, envelope.OrganizationId);
        var owner = ObservationSignatureAccess.IsOwner(envelope, current);
        if (!owner)
        {
            await ObservationSignatureAccess.EnsureStaffAccessAsync(envelope, current, cohorts, offerings, assignments, ct, false);
            if (!SignatureVisibilityPolicy.CanReadContent(false,
                    ObservationSignatureAccess.IsAuthor(envelope, current),
                    current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise), envelope.Status))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        }
        return new SignatureContentDto(SignatureDtoFactory.ToDto(envelope, mapper), envelope.SnapshotJson,
            envelope.ExpiresAtUtc, envelope.ViewedAtUtc, envelope.ResponseComment,
            owner && current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Sign)
                && envelope.Status is (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed)
                && envelope.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }
}

public sealed class ListEnrollmentObservationSignaturesQueryHandler(ISignatureEnvelopeRepository repository,
    IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ILearnerProfileRepository profiles, ICohortTrainerAssignmentRepository assignments,
    ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<ListEnrollmentObservationSignaturesQuery, IReadOnlyCollection<SignatureEnvelopeDto>>
{
    public async Task<IReadOnlyCollection<SignatureEnvelopeDto>> Handle(ListEnrollmentObservationSignaturesQuery request, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(request.EnrollmentId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        var learner = await profiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        var self = current.UserId.HasValue && learner.AuthGateUserId == current.UserId.Value;
        if (!self)
        {
            // Verify scope even for empty lists to avoid enrollment enumeration.
            var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
            if (cohort.OrganizationId != enrollment.OrganizationId)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            var access = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
            if (!current.HasPermission(PedagoraPilotPermissionCodes.Signatures.View)
                || (current.HasContextualScopeRestrictions && !current.CanViewCohort(cohort.SiteId, access.ProgramId, cohort.Id.Value)))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        }
        var rows = await repository.Query(false).Where(x => x.OrganizationId == enrollment.OrganizationId
            && x.EnrollmentId == request.EnrollmentId && x.SourceType != SignatureSourceType.Attendance)
            .OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(ct);
        var supervised = !self && current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise);
        var assigned = false;
        if (!self && !supervised && current.UserId.HasValue)
        {
            var assignment = await assignments.GetByUserAndCohortAsync(enrollment.OrganizationId,
                current.UserId.Value, enrollment.CohortId, false, ct);
            assigned = assignment?.IsActive == true;
        }
        return rows.Where(x => SignatureVisibilityPolicy.CanListInLearnerHistory(
                self, !self && ObservationSignatureAccess.IsAuthor(x, current),
                !self && assigned, supervised, x.Status))
            .Select(x => SignatureDtoFactory.ToDto(x, mapper)).ToArray();
    }
}

public sealed class ListMyPendingObservationsQueryHandler(ISignatureEnvelopeRepository repository,
    ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<ListMyPendingObservationsQuery, IReadOnlyCollection<SignatureEnvelopeDto>>
{
    public async Task<IReadOnlyCollection<SignatureEnvelopeDto>> Handle(ListMyPendingObservationsQuery request, CancellationToken ct)
    {
        if (!current.UserId.HasValue) throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var org = TenantScope.Organization(current);
        if (!org.HasValue) throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var rows = await repository.Query(false).Where(x => x.OrganizationId == org.Value
            && x.SignerAuthGateUserId == current.UserId.Value
            && x.SourceType != SignatureSourceType.Attendance
            && (x.Status == SignatureEnvelopeStatus.Pending || x.Status == SignatureEnvelopeStatus.Viewed))
            .OrderByDescending(x => x.RequestedAtUtc).Take(100).ToListAsync(ct);
        return rows.Select(x => SignatureDtoFactory.ToDto(x, mapper)).ToArray();
    }
}
