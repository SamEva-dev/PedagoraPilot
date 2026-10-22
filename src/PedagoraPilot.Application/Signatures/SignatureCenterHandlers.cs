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
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Signatures;

/// <summary>Tenant-scoped, filtered and paged before returning private learner data.</summary>
public sealed class ListSignatureCenterQueryHandler(
    ISignatureEnvelopeRepository envelopes, ICohortRepository cohorts,
    IProgramOfferingRepository offerings, ITrainingProgramRepository programs,
    ICohortTrainerAssignmentRepository assignments,
    IEnrollmentRepository enrollments, ILearnerProfileRepository profiles, IPersonRepository people,
    ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<ListSignatureCenterQuery, SignatureCenterPageDto>
{
    public async Task<SignatureCenterPageDto> Handle(ListSignatureCenterQuery request, CancellationToken ct)
    {
        if (!current.UserId.HasValue || !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.View))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var organizationId = TenantScope.Organization(current)
            ?? throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var page = Math.Clamp(request.Page, 1, 100000);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var authorId = current.UserId.Value.ToString("D");

        var scopedCohorts = await (from c in cohorts.Query(false)
            join o in offerings.Query(false) on c.ProgramOfferingId equals o.Id
            join program in programs.Query(false) on o.ProgramId equals program.Id
            where c.OrganizationId == organizationId && c.SiteId == o.SiteId &&
                (program.OrganizationId == null || program.OrganizationId == organizationId)
            select new { Cohort = c.Id.Value, c.SiteId, o.ProgramId }).ToListAsync(ct);
        var allowed = scopedCohorts.Where(c => !current.HasContextualScopeRestrictions
            || current.CanViewCohort(c.SiteId, c.ProgramId, c.Cohort)).Select(c => c.Cohort).ToArray();
        // Never show an unrelated cohort even when the user submitted a forged cohortId.
        if (request.CohortId.HasValue && !allowed.Contains(request.CohortId.Value))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        var query = envelopes.Query(false).Where(e => e.OrganizationId == organizationId && allowed.Contains(e.CohortId));
        if (request.CohortId.HasValue) query = query.Where(e => e.CohortId == request.CohortId.Value);
        if (request.Team)
        {
            var assigned = await assignments.Query(false).Where(a => a.OrganizationId == organizationId
                && a.AuthGateUserId == current.UserId.Value && a.IsActive)
                .Select(a => a.CohortId).ToListAsync(ct);
            var assignedCohorts = assigned.Select(id => id.Value).ToArray();
            if (current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise))
            {
                // Only authorized organization/cohort scope; supervisor may see pending cases.
            }
            else
            {
                // Other trainers see only signed documents for cohorts to which they are actively assigned.
                query = query.Where(e => e.RequestedByUserId == authorId
                    || (e.Status == SignatureEnvelopeStatus.Signed && assignedCohorts.Contains(e.CohortId)));
            }
        }
        else query = query.Where(e => e.RequestedByUserId == authorId);

        var counts = (await query.GroupBy(e => e.Status).Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(ct)).ToDictionary(item => item.Key.ToString().ToLowerInvariant(), item => item.Count);
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<SignatureEnvelopeStatus>(request.Status, true, out var status) || !Enum.IsDefined(status))
                throw new ValidationApplicationException("SIGNATURE_CENTER_STATUS_INVALID");
            query = query.Where(e => e.Status == status);
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(e => e.CreatedAtUtc).ThenBy(e => e.SourceId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var enrollmentIds = rows.Select(e => e.EnrollmentId).Distinct().ToArray();
        var identityRows = await (from en in enrollments.Query(false)
            join learner in profiles.Query(false) on en.LearnerProfileId equals learner.Id
            join person in people.Query(false) on learner.PersonId equals person.Id
            where en.OrganizationId == organizationId && enrollmentIds.Contains(en.Id)
            select new { en.Id, person.FirstName, person.LastName }).ToListAsync(ct);
        var names = identityRows.ToDictionary(x => x.Id.Value,
            x => $"{x.FirstName} {x.LastName}".Trim());
        return new SignatureCenterPageDto(rows.Select(e => new SignatureCenterItemDto(
            SignatureDtoFactory.ToDto(e, mapper), e.ExpiresAtUtc, e.LastReminderAtUtc,
            e.ReminderCount, e.Status == SignatureEnvelopeStatus.Declined ? e.ResponseComment : null,
            names.GetValueOrDefault(e.EnrollmentId.Value) ?? string.Empty)).ToArray(),
            total, page, pageSize, counts);
    }
}

public sealed class RemindSignatureCommandHandler(ISignatureEnvelopeRepository envelopes,
    ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ICohortTrainerAssignmentRepository assignments, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<RemindSignatureCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(RemindSignatureCommand request, CancellationToken ct)
    {
        var envelope = await envelopes.GetByIdAsync(request.Id, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
        await ObservationSignatureAccess.EnsureStaffAccessAsync(envelope, current, cohorts, offerings, assignments, ct, true);
        if (!ObservationSignatureAccess.IsAuthor(envelope, current)
            && !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var now = DateTimeOffset.UtcNow;
        if (envelope.Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed)
            || envelope.ExpiresAtUtc <= now)
            throw new ConflictApplicationException(ErrorKeys.SignatureTransitionForbidden);
        if (envelope.ReminderCount >= 3 || envelope.RequestedAtUtc > now.AddHours(-24)
            || (envelope.LastReminderAtUtc.HasValue && envelope.LastReminderAtUtc > now.AddHours(-24)))
            throw new ConflictApplicationException("SIGNATURE_REMINDER_RATE_LIMITED");
        envelope.Remind(now);
        return SignatureDtoFactory.ToDto(envelope, mapper);
    }
}

/// <summary>All-or-nothing batch; existing request handlers enforce source-specific business rules.</summary>
public sealed class RequestSignatureBatchCommandHandler(
    IRequestHandler<RequestObservationSignatureCommand, SignatureEnvelopeDto> observations,
    IRequestHandler<RequestAttendanceSignatureCommand, SignatureEnvelopeDto> attendances,
    ISignatureEnvelopeRepository envelopes, ICurrentUser current)
    : IRequestHandler<RequestSignatureBatchCommand, SignatureBatchResultDto>
{
    public async Task<SignatureBatchResultDto> Handle(RequestSignatureBatchCommand request, CancellationToken ct)
    {
        if (!current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)
            || request.Ids.Count is < 1 or > 50 || request.Ids.Any(id => id.IsEmpty)
            || request.Ids.Distinct().Count() != request.Ids.Count)
            throw new ValidationApplicationException("SIGNATURE_BATCH_INVALID");
        var result = new List<SignatureEnvelopeDto>();
        foreach (var id in request.Ids)
        {
            var envelope = await envelopes.GetByIdAsync(id, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.SignatureNotFound);
            TenantScope.Ensure(current, envelope.OrganizationId);
            result.Add(envelope.SourceType == SignatureSourceType.Attendance
                ? await attendances.Handle(new RequestAttendanceSignatureCommand(id), ct)
                : await observations.Handle(new RequestObservationSignatureCommand(id), ct));
        }
        return new SignatureBatchResultDto(result);
    }
}

/// <summary>One immutable daily recap per selected versioned set; no fabricated observations.</summary>
public sealed class CreateDailyObservationSummaryCommandHandler(
    IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings,
    IOrganizationRepository organizations, ILearnerProfileRepository profiles, IDrivingEvaluationRepository evaluations,
    ISignatureEnvelopeRepository envelopes, ICurrentUser current, IObjectMapper mapper)
    : IRequestHandler<CreateDailyObservationSummaryCommand, SignatureEnvelopeDto>
{
    public async Task<SignatureEnvelopeDto> Handle(CreateDailyObservationSummaryCommand request, CancellationToken ct)
    {
        if (!current.UserId.HasValue || !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)
            || !current.HasPermission(PedagoraPilotPermissionCodes.Driving.View)
            || request.EvaluationIds.Count is < 1 or > 20 || request.EvaluationIds.Distinct().Count() != request.EvaluationIds.Count)
            throw new ValidationApplicationException("SIGNATURE_DAILY_SUMMARY_INVALID");
        var enrollment = await enrollments.GetByIdAsync(request.EnrollmentId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        if (enrollment.Status is EnrollmentStatus.Pending or EnrollmentStatus.Withdrawn or EnrollmentStatus.Cancelled)
            throw new ConflictApplicationException("SIGNATURE_ENROLLMENT_NOT_ELIGIBLE");
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.OrganizationId != enrollment.OrganizationId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        var profile = await profiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        if (!profile.AuthGateUserId.HasValue)
            throw new ConflictApplicationException("SIGNATURE_SIGNER_UNLINKED");
        var ids = request.EvaluationIds.Select(id => new DrivingEvaluationId(id)).ToArray();
        var found = await evaluations.Query(false).Include(x => x.Criteria)
            .Where(x => x.OrganizationId == enrollment.OrganizationId && x.EnrollmentId == enrollment.Id
                && ids.Contains(x.Id)).ToListAsync(ct);
        var organization = await organizations.GetByIdAsync(enrollment.OrganizationId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
        var zone = SignatureTime.ResolveOrganizationZone(organization.Timezone);
        if (found.Count != ids.Length || found.Any(e =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(e.EvaluatedAtUtc, zone).DateTime) != request.Date))
            throw new ConflictApplicationException("SIGNATURE_DAILY_SUMMARY_INVALID");
        var author = current.UserId.Value.ToString("D");
        if (!current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Supervise)
            && found.Any(e => !string.Equals(e.TrainerAuthGateUserId, author, StringComparison.OrdinalIgnoreCase)))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var ordered = found.OrderBy(e => e.EvaluatedAtUtc).ThenBy(e => e.Id.Value).ToArray();
        var items = ordered.Select(e => new {
            EvaluationId = e.Id.Value, e.Version, e.EvaluatedAtUtc, e.TrainerDisplayName,
            e.Subject, e.Positive, e.Difficulty, e.NextGoal, e.FreeObservation,
            Criteria = e.Criteria.OrderBy(c => c.Code).Select(c => new { c.Code, c.Label, Level = c.Level.ToString() }).ToArray()
        }).ToArray();
        var snapshot = JsonSerializer.Serialize(new { Type = "daily-observation-summary", Date = request.Date.ToString("yyyy-MM-dd"), Timezone = organization.Timezone,
            EnrollmentId = enrollment.Id.Value, Observations = items });
        var fingerprint = $"{enrollment.Id.Value:N}|{request.Date:yyyy-MM-dd}|{organization.Timezone}|" +
            string.Join("|", ordered.OrderBy(e => e.Id.Value).Select(e => $"{e.Id.Value:N}:{e.Version}"));
        var sourceId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)).AsSpan(0, 16));
        var existing = await envelopes.FindBySourceAsync(enrollment.OrganizationId,
            SignatureSourceType.DailyObservationSummary, sourceId, 1, enrollment.Id, ct);
        if (existing is not null) return SignatureDtoFactory.ToDto(existing, mapper);
        var entity = SignatureEnvelope.CreateDraft(enrollment.OrganizationId, cohort.SiteId, cohort.Id.Value,
            enrollment.Id, profile.AuthGateUserId.Value, SignatureSourceType.DailyObservationSummary,
            sourceId, 1, snapshot, author, DateTimeOffset.UtcNow);
        await envelopes.AddAsync(entity, ct);
        return SignatureDtoFactory.ToDto(entity, mapper);
    }
}
