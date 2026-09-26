using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.DistanceLearning;
using PedagoraPilot.Domain.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.DistanceLearning;
internal static class DlMap
{
    public static DistanceLearningSessionDto Session(DistanceLearningSession x, IObjectMapper mapper)
    {
        var m = mapper.Map<DistanceLearningSession, DistanceLearningSessionReadModel>(x);
        return new(m.Id.Value, m.OrganizationId, m.SiteId, m.ProgramId, m.CohortId.Value, m.Title, m.TrainerDisplayName, m.TrainerEmail, m.StartsAtUtc, m.EndsAtUtc, m.Platform.ToString(), m.JoinUrl, m.Objectives, m.Status.ToString(), x.Participants.Select(p =>
        {
            var q = mapper.Map<DistanceParticipant, DistanceParticipantReadModel>(p);
            return new DistanceParticipantDto(q.Id.Value, q.EnrollmentId.Value, q.DisplayName, q.Attendance.ToString(), q.ConnectedAtUtc, q.DisconnectedAtUtc, q.ConnectedMinutes, q.ParticipationPercent, q.CompletedActivities, q.ActivityCount);
        }).ToArray());
    }

    public static AsyncLearningModuleDto Module(AsyncLearningModule x, IObjectMapper mapper)
    {
        var m = mapper.Map<AsyncLearningModule, AsyncLearningModuleReadModel>(x);
        return new(m.Id.Value, m.OrganizationId, m.SiteId, m.ProgramId, m.CohortId.Value, m.Title, m.Description, m.EstimatedMinutes, m.DueDate, m.TrainerDisplayName, m.Status.ToString(), m.ProgressPercent, m.CompletedStudents, m.ExpectedStudents, m.AverageScore, x.Steps.OrderBy(s => s.SortOrder).Select(s =>
        {
            var q = mapper.Map<AsyncModuleStep, AsyncModuleStepReadModel>(s);
            return new AsyncModuleStepDto(q.Id.Value, q.Code, q.Label, q.SortOrder);
        }).ToArray());
    }
}

public sealed class GetDistanceLearningSessionsQueryHandler(IDistanceLearningSessionRepository repo, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<GetDistanceLearningSessionsQuery, IReadOnlyCollection<DistanceLearningSessionDto>>
{
    public async Task<IReadOnlyCollection<DistanceLearningSessionDto>> Handle(GetDistanceLearningSessionsQuery r, CancellationToken ct)
    {
        IQueryable<DistanceLearningSession> q = repo.Query(false).Include(x => x.Participants);
        var organizationId = TenantScope.Organization(current);
        if (organizationId.HasValue)
            q = q.Where(x => x.OrganizationId == organizationId.Value);
        if (r.CohortId.HasValue)
            q = q.Where(x => x.CohortId == r.CohortId.Value);
        var rows = await q.OrderByDescending(x => x.StartsAtUtc).ToListAsync(ct);
        if (current.HasContextualScopeRestrictions)
            rows = rows.Where(x => current.CanViewCohort(x.SiteId, x.ProgramId, x.CohortId.Value)).ToList();
        return rows.Select(x => DlMap.Session(x, mapper)).ToArray();
    }
}

public sealed class CreateDistanceLearningSessionCommandHandler(IDistanceLearningSessionRepository repo, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<CreateDistanceLearningSessionCommand, DistanceLearningSessionDto>
{
    public async Task<DistanceLearningSessionDto> Handle(CreateDistanceLearningSessionCommand r, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(r.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);
        if (r.SiteId != cohort.SiteId)
            throw new ForbiddenApplicationException(ErrorKeys.DistanceForbidden);
        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        if (offering.ProgramId != r.ProgramId)
            throw new ForbiddenApplicationException(ErrorKeys.DistanceForbidden);
        ContextualScope.EnsureCanManageCohort(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value);
        if (!Enum.TryParse<DistancePlatform>(r.Platform, true, out var platform))
            throw new ValidationApplicationException(ErrorKeys.DistancePlatformInvalid);
        var x = DistanceLearningSession.Create(cohort.OrganizationId, r.SiteId, r.ProgramId, cohort.Id, r.Title, r.TrainerDisplayName, r.TrainerEmail, r.StartsAtUtc, r.EndsAtUtc, platform, r.JoinUrl, r.Objectives);
        await repo.AddAsync(x, ct);
        return DlMap.Session(x, mapper);
    }
}

public sealed class AddDistanceParticipantCommandHandler(IDistanceLearningSessionRepository repo, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<AddDistanceParticipantCommand, DistanceLearningSessionDto>
{
    public async Task<DistanceLearningSessionDto> Handle(AddDistanceParticipantCommand r, CancellationToken ct)
    {
        var x = await repo.Query(true).Include(a => a.Participants).SingleOrDefaultAsync(a => a.Id == r.SessionId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DistanceSessionNotFound);
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        ContextualScope.EnsureCanManageCohort(current, x.SiteId, x.ProgramId, x.CohortId.Value);
        if (enrollment.CohortId != x.CohortId)
            throw new ConflictApplicationException(ErrorKeys.DistanceParticipantCohortMismatch);
        x.AddParticipant(r.EnrollmentId, r.DisplayName);
        return DlMap.Session(x, mapper);
    }
}

public sealed class ChangeDistanceSessionStatusCommandHandler(IDistanceLearningSessionRepository repo, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<ChangeDistanceSessionStatusCommand, DistanceLearningSessionDto>
{
    public async Task<DistanceLearningSessionDto> Handle(ChangeDistanceSessionStatusCommand r, CancellationToken ct)
    {
        var x = await repo.Query(true).Include(a => a.Participants).SingleOrDefaultAsync(a => a.Id == r.SessionId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DistanceSessionNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        ContextualScope.EnsureCanManageCohort(current, x.SiteId, x.ProgramId, x.CohortId.Value);
        if (!Enum.TryParse<DistanceLearningSessionStatus>(r.Status, true, out var status))
            throw new ValidationApplicationException(ErrorKeys.DistanceSessionStatusInvalid);
        x.ChangeStatus(status);
        return DlMap.Session(x, mapper);
    }
}

public sealed class UpdateDistanceAttendanceCommandHandler(IDistanceLearningSessionRepository repo, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<UpdateDistanceAttendanceCommand, DistanceLearningSessionDto>
{
    public async Task<DistanceLearningSessionDto> Handle(UpdateDistanceAttendanceCommand r, CancellationToken ct)
    {
        var x = await repo.Query(true).Include(a => a.Participants).SingleOrDefaultAsync(a => a.Id == r.SessionId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DistanceSessionNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        ContextualScope.EnsureCanManageCohort(current, x.SiteId, x.ProgramId, x.CohortId.Value);
        if (!Enum.TryParse<DistanceAttendanceStatus>(r.Attendance, true, out var status))
            throw new ValidationApplicationException(ErrorKeys.DistanceAttendanceInvalid);
        x.RecordParticipantAttendance(r.ParticipantId, status, r.ConnectedAtUtc, r.DisconnectedAtUtc, r.ConnectedMinutes, r.ParticipationPercent, r.CompletedActivities, r.ActivityCount);
        return DlMap.Session(x, mapper);
    }
}

public sealed class GetAsyncLearningModulesQueryHandler(IAsyncLearningModuleRepository repo, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<GetAsyncLearningModulesQuery, IReadOnlyCollection<AsyncLearningModuleDto>>
{
    public async Task<IReadOnlyCollection<AsyncLearningModuleDto>> Handle(GetAsyncLearningModulesQuery r, CancellationToken ct)
    {
        IQueryable<AsyncLearningModule> q = repo.Query(false).Include(x => x.Steps);
        var organizationId = TenantScope.Organization(current);
        if (organizationId.HasValue)
            q = q.Where(x => x.OrganizationId == organizationId.Value);
        if (r.CohortId.HasValue)
            q = q.Where(x => x.CohortId == r.CohortId.Value);
        var rows = await q.OrderBy(x => x.DueDate).ToListAsync(ct);
        if (current.HasContextualScopeRestrictions)
            rows = rows.Where(x => current.CanViewCohort(x.SiteId, x.ProgramId, x.CohortId.Value)).ToList();
        return rows.Select(x => DlMap.Module(x, mapper)).ToArray();
    }
}

public sealed class CreateAsyncLearningModuleCommandHandler(IAsyncLearningModuleRepository repo, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<CreateAsyncLearningModuleCommand, AsyncLearningModuleDto>
{
    public async Task<AsyncLearningModuleDto> Handle(CreateAsyncLearningModuleCommand r, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(r.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);
        if (r.SiteId != cohort.SiteId)
            throw new ForbiddenApplicationException(ErrorKeys.DistanceForbidden);
        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        if (offering.ProgramId != r.ProgramId)
            throw new ForbiddenApplicationException(ErrorKeys.DistanceForbidden);
        ContextualScope.EnsureCanManageCohort(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value);
        var x = AsyncLearningModule.Create(cohort.OrganizationId, r.SiteId, r.ProgramId, cohort.Id, r.Title, r.Description, r.EstimatedMinutes, r.DueDate, r.TrainerDisplayName, r.ExpectedStudents);
        foreach (var step in r.Steps ?? [])
            x.AddStep(step.Code, step.Label, step.SortOrder);
        await repo.AddAsync(x, ct);
        return DlMap.Module(x, mapper);
    }
}

public sealed class UpdateAsyncModuleProgressCommandHandler(IAsyncLearningModuleRepository repo, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<UpdateAsyncModuleProgressCommand, AsyncLearningModuleDto>
{
    public async Task<AsyncLearningModuleDto> Handle(UpdateAsyncModuleProgressCommand r, CancellationToken ct)
    {
        var x = await repo.Query(true).Include(a => a.Steps).SingleOrDefaultAsync(a => a.Id == r.ModuleId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DistanceModuleNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        ContextualScope.EnsureCanManageCohort(current, x.SiteId, x.ProgramId, x.CohortId.Value);
        x.UpdateProgress(r.ProgressPercent, r.CompletedStudents, r.AverageScore);
        return DlMap.Module(x, mapper);
    }
}
