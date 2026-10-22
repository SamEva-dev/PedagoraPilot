using System.Text.Json;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Delivery;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Signatures;

internal static class SignatureDtoFactory
{
    public static SignatureEnvelopeDto ToDto(SignatureEnvelope entity, IObjectMapper mapper)
    {
        var model = mapper.Map<SignatureEnvelope, SignatureEnvelopeReadModel>(entity);
        return new SignatureEnvelopeDto(model.Id.Value, model.OrganizationId, model.CohortId,
            model.EnrollmentId.Value, model.SourceType == SignatureSourceType.Attendance ? "attendance" :
                model.SourceType == SignatureSourceType.DailyObservationSummary ? "daily-observation-summary" : "pedagogical-observation",
            model.SourceId, model.SourceVersion, model.Status.ToString().ToLowerInvariant(),
            model.ContentSha256, model.CreatedAtUtc, model.RequestedAtUtc, model.SignedAtUtc, model.Version, model.ArchivedAtUtc);
    }
}

/// <summary>Creates an authenticated, scoped DRAFT only; no delivery URL or signature is issued in S14.</summary>
public sealed class CreateSignatureDraftCommandHandler(
    IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ILearnerProfileRepository learnerProfiles, IDrivingEvaluationRepository evaluations,
    IAttendanceSheetRepository attendance, ITrainingSessionRepository sessions,
    ISignatureEnvelopeRepository envelopes, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<CreateSignatureDraftCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(CreateSignatureDraftCommand request, CancellationToken ct)
    {
        if (!current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Request) || !current.UserId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        var enrollment = await enrollments.GetByIdAsync(request.EnrollmentId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.OrganizationId != enrollment.OrganizationId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        if (enrollment.Status is EnrollmentStatus.Cancelled or EnrollmentStatus.Withdrawn or EnrollmentStatus.Pending)
            throw new ConflictApplicationException("SIGNATURE_ENROLLMENT_NOT_ELIGIBLE");
        var profile = await learnerProfiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        if (!profile.AuthGateUserId.HasValue)
            throw new ConflictApplicationException("SIGNATURE_SIGNER_UNLINKED");

        SignatureSourceType sourceType;
        long sourceVersion;
        string snapshot;
        if (string.Equals(request.SourceType, "pedagogical-observation", StringComparison.OrdinalIgnoreCase))
        {
            if (!current.HasPermission(PedagoraPilotPermissionCodes.Driving.View))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            sourceType = SignatureSourceType.PedagogicalObservation;
            var evaluation = await evaluations.Query(false).Include(x => x.Criteria)
                .SingleOrDefaultAsync(x => x.Id == new DrivingEvaluationId(request.SourceId)
                    && x.EnrollmentId == enrollment.Id && x.OrganizationId == enrollment.OrganizationId, ct)
                ?? throw new NotFoundApplicationException("SIGNATURE_SOURCE_NOT_FOUND");
            if (!current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise)
                && !current.HasPermission(PedagoraPilotPermissionCodes.Driving.ViewOthers)
                && !string.Equals(evaluation.TrainerAuthGateUserId, current.UserId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            sourceVersion = evaluation.Version;
            snapshot = JsonSerializer.Serialize(new
            {
                Type = "pedagogical-observation", EvaluationId = evaluation.Id.Value,
                EnrollmentId = enrollment.Id.Value, evaluation.EvaluatedAtUtc, evaluation.TrainerDisplayName,
                evaluation.Subject, evaluation.Positive, evaluation.Difficulty,
                evaluation.NextGoal, evaluation.FreeObservation,
                Criteria = evaluation.Criteria.OrderBy(x => x.Code).Select(x => new { x.Code, x.Label, Level = x.Level.ToString() }).ToArray()
            });
        }
        else if (string.Equals(request.SourceType, "attendance", StringComparison.OrdinalIgnoreCase))
        {
            if (!current.HasPermission(PedagoraPilotPermissionCodes.Attendance.Manage))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            sourceType = SignatureSourceType.Attendance;
            var session = await sessions.GetByIdAsync(new TrainingSessionId(request.SourceId), false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
            if (session.OrganizationId != enrollment.OrganizationId || session.CohortId != enrollment.CohortId || session.SiteId != cohort.SiteId)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            if (session.AudienceMode == SessionAudienceMode.SelectedEnrollments
                && !session.Participants.Any(x => x.EnrollmentId == enrollment.Id))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            if (!current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise)
                && !current.HasPermission(PedagoraPilotPermissionCodes.Sessions.ViewOthers)
                && !string.Equals(session.TrainerAuthGateUserId, current.UserId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            var sheet = await attendance.GetBySessionIdAsync(session.Id, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.AttendanceNotFound);
            if (sheet.OrganizationId != enrollment.OrganizationId || sheet.CohortId != enrollment.CohortId)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            var entry = sheet.Entries.SingleOrDefault(x => x.EnrollmentId == enrollment.Id)
                ?? throw new NotFoundApplicationException("SIGNATURE_SOURCE_NOT_FOUND");
            if (entry.Status is not (AttendanceStatus.Present or AttendanceStatus.Late))
                throw new ConflictApplicationException("SIGNATURE_ATTENDANCE_NOT_SIGNABLE");
            sourceVersion = sheet.Version;
            snapshot = JsonSerializer.Serialize(new
            {
                Type = "attendance", SessionId = session.Id.Value, EnrollmentId = enrollment.Id.Value,
                session.Title, session.StartsAtUtc, session.EndsAtUtc, session.TimeZoneId,
                Status = entry.Status.ToString(), entry.ArrivalAtUtc, entry.DepartureAtUtc,
                entry.ExpectedMinutes, entry.PresentMinutes, entry.MissedMinutes
            });
        }
        else throw new ValidationApplicationException("SIGNATURE_SOURCE_INVALID");

        // Database unique index also protects against concurrent duplicate creations.
        var existing = await envelopes.FindBySourceAsync(enrollment.OrganizationId, sourceType,
            request.SourceId, sourceVersion, enrollment.Id, ct);
        if (existing is not null) return SignatureDtoFactory.ToDto(existing, mapper);
        var entity = SignatureEnvelope.CreateDraft(enrollment.OrganizationId, cohort.SiteId,
            cohort.Id.Value, enrollment.Id, profile.AuthGateUserId.Value, sourceType,
            request.SourceId, sourceVersion, snapshot, current.UserId.Value.ToString("D"), DateTimeOffset.UtcNow);
        await envelopes.AddAsync(entity, ct);
        return SignatureDtoFactory.ToDto(entity, mapper);
    }
}

public sealed class GetSignatureDraftQueryHandler(ISignatureEnvelopeRepository envelopes,
    ICurrentUser current, IObjectMapper mapper) : IRequestHandler<GetSignatureDraftQuery, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(GetSignatureDraftQuery request, CancellationToken ct)
    {
        if (!current.UserId.HasValue || !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.View))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var envelope = await envelopes.GetByIdAsync(request.Id, false, ct)
            ?? throw new NotFoundApplicationException("SIGNATURE_NOT_FOUND");
        TenantScope.Ensure(current, envelope.OrganizationId);
        if (!string.Equals(envelope.RequestedByUserId, current.UserId.Value.ToString("D"), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        return SignatureDtoFactory.ToDto(envelope, mapper);
    }
}

public sealed class ListMySignatureDraftsQueryHandler(ISignatureEnvelopeRepository envelopes,
    ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<ListMySignatureDraftsQuery, IReadOnlyCollection<SignatureEnvelopeDto>>
{
    public async Task<IReadOnlyCollection<SignatureEnvelopeDto>> Handle(ListMySignatureDraftsQuery request, CancellationToken ct)
    {
        if (!current.UserId.HasValue || !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.View))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var organizationId = TenantScope.Organization(current);
        if (!organizationId.HasValue) // Platform users must select an actual tenant context; never enumerate across tenants.
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var identity = current.UserId.Value.ToString("D");
        var query = envelopes.Query(false).Where(x => x.OrganizationId == organizationId.Value && x.RequestedByUserId == identity);
        if (request.CohortId.HasValue) query = query.Where(x => x.CohortId == request.CohortId.Value);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync(ct);
        if (!current.HasContextualScopeRestrictions)
            return items.Select(x => SignatureDtoFactory.ToDto(x, mapper)).ToArray();
        var cohortIds = items.Select(x => new CohortId(x.CohortId)).Distinct().ToArray();
        var cohortRows = await cohorts.Query(false).Where(x => cohortIds.Contains(x.Id)).ToListAsync(ct);
        var cohortMap = cohortRows.ToDictionary(x => x.Id.Value);
        var offeringIds = cohortRows.Select(x => x.ProgramOfferingId).Distinct().ToArray();
        var programMap = await offerings.Query(false).Where(x => offeringIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.ProgramId, ct);
        return items.Where(x => cohortMap.TryGetValue(x.CohortId, out var cohort)
                && programMap.TryGetValue(cohort.ProgramOfferingId, out var programId)
                && current.CanViewCohort(cohort.SiteId, programId, cohort.Id.Value))
            .Select(x => SignatureDtoFactory.ToDto(x, mapper)).ToArray();
    }
}
