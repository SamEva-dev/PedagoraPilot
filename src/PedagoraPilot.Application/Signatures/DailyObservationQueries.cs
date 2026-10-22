using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Signatures;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Signatures;

/// <summary>Trainer's real evaluations for a selected UTC date, tenant and cohort scoped.</summary>
public sealed class ListMyDailyObservationsQueryHandler(
    IDrivingEvaluationRepository evaluations, IOrganizationRepository organizations, IEnrollmentRepository enrollments,
    ILearnerProfileRepository learners, IPersonRepository people, ICohortRepository cohorts,
    IProgramOfferingRepository offerings, ISignatureEnvelopeRepository signatures,
    ICurrentUser current)
    : IRequestHandler<ListMyDailyObservationsQuery, IReadOnlyCollection<DailyObservationSignatureCandidateDto>>
{
    public async Task<IReadOnlyCollection<DailyObservationSignatureCandidateDto>> Handle(
        ListMyDailyObservationsQuery request, CancellationToken ct)
    {
        if (!current.UserId.HasValue || !current.HasPermission(PedagoraPilotPermissionCodes.Signatures.Request)
            || !current.HasPermission(PedagoraPilotPermissionCodes.Driving.View))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var org = TenantScope.Organization(current);
        if (!org.HasValue) throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        // Interpret "day" in the organization's configured timezone (DST-safe start/end instants).
        var organization = await organizations.GetByIdAsync(org.Value, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
        var zone = SignatureTime.ResolveOrganizationZone(organization.Timezone);
        var startLocal = DateTime.SpecifyKind(request.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(1);
        var start = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, zone), TimeSpan.Zero);
        var end = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocal, zone), TimeSpan.Zero);
        var author = current.UserId.Value.ToString("D");
        var rows = await (
            from d in evaluations.Query(false)
            join e in enrollments.Query(false) on d.EnrollmentId equals e.Id
            join l in learners.Query(false) on e.LearnerProfileId equals l.Id
            join p in people.Query(false) on l.PersonId equals p.Id
            join c in cohorts.Query(false) on e.CohortId equals c.Id
            join o in offerings.Query(false) on c.ProgramOfferingId equals o.Id
            where d.OrganizationId == org.Value && e.OrganizationId == org.Value
                && c.OrganizationId == org.Value && d.TrainerAuthGateUserId == author
                && d.EvaluatedAtUtc >= start && d.EvaluatedAtUtc < end
            orderby d.EvaluatedAtUtc descending
            select new { Id = d.Id, d.EnrollmentId, d.Version, d.Subject, d.EvaluatedAtUtc,
                p.FirstName, p.LastName, CohortId = c.Id, c.SiteId, o.ProgramId }
        ).Take(200).ToListAsync(ct);
        var accessible = rows.Where(x => current.CanManageCohort(x.SiteId, x.ProgramId, x.CohortId.Value)).ToArray();
        var sourceIds = accessible.Select(x => x.Id.Value).ToArray();
        var existing = await signatures.Query(false).Where(x => x.OrganizationId == org.Value
            && x.SourceType == SignatureSourceType.PedagogicalObservation && sourceIds.Contains(x.SourceId))
            .Select(x => new { x.SourceId, x.SourceVersion, x.EnrollmentId, x.Status })
            .ToListAsync(ct);
        return accessible.Select(x => new DailyObservationSignatureCandidateDto(x.Id.Value, x.EnrollmentId.Value,
            $"{x.FirstName} {x.LastName}".Trim(), x.Subject, x.EvaluatedAtUtc,
            existing.FirstOrDefault(s => s.SourceId == x.Id.Value && s.SourceVersion == x.Version
                && s.EnrollmentId == x.EnrollmentId)?.Status.ToString().ToLowerInvariant() ?? "not-requested"))
            .ToArray();
    }
}
