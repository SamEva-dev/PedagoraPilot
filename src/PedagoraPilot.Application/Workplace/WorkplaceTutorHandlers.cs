using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Workplace;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workplace;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Workplace;

public sealed record GetAssignedTutorPeriodsQuery : IQuery<IReadOnlyCollection<WorkplacePeriodDto>>;
public sealed record UpdateTutorHoursCommand(Guid PeriodId, decimal CompletedHours, string? Observation) : ICommand<WorkplacePeriodDto>;
public sealed record UpdateTutorActivityCommand(Guid PeriodId, Guid ActivityId, string Status, string? Comment) : ICommand<WorkplacePeriodDto>;
public sealed record SubmitTutorEvaluationCommand(Guid PeriodId, string Summary, string? Strengths, string? ImprovementAreas, bool? Validated) : ICommand<WorkplacePeriodDto>;

internal static class WorkplaceTutorAuthorization
{
    public static (Guid OrganizationId, Guid UserId) Require(ICurrentUser current, bool evaluate)
    {
        var permission = evaluate ? PedagoraPilotPermissionCodes.Internships.TutorEvaluateAssigned : PedagoraPilotPermissionCodes.Internships.TutorViewAssigned;
        var isDedicatedTutor = current.Roles.Contains(PedagoraPilotRoleCodes.InternshipTutor) && current.HasPermission(permission);
        var isEligibleStaff = current.Roles.Any(r => r is
            PedagoraPilotRoleCodes.Trainer or PedagoraPilotRoleCodes.PedagogicalManager or
            PedagoraPilotRoleCodes.OrganizationAdministrator or PedagoraPilotRoleCodes.OrganizationDirection or
            PedagoraPilotRoleCodes.SiteDirection);
        if (!current.IsAuthenticated || !(isDedicatedTutor || isEligibleStaff)
            || current.OrganizationId is not Guid organizationId || organizationId == Guid.Empty
            || current.UserId is not Guid userId || userId == Guid.Empty)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        return (organizationId, userId);
    }
    // Token roles can outlive a membership suspension or role revocation.
    // Re-read the organization membership for every tutor operation.
    public static async Task<(Guid OrganizationId, Guid UserId)> RequireActiveMemberAsync(
        ICurrentUser current, IOrganizationMemberRepository members, bool evaluate, CancellationToken ct)
    {
        var (organizationId, userId) = Require(current, evaluate);
        var member = await members.GetByAuthGateUserAsync(organizationId, userId, false, ct);
        if (member is null || !member.IsActive || member.OrganizationId != organizationId)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        // AuthGate tokens may outlive revocation of a staff role. Verify that
        // a staff-tutor still holds an eligible role in the local organization.
        if (!current.Roles.Contains(PedagoraPilotRoleCodes.InternshipTutor))
        {
            var roleCodes = await members.GetRoleCodesAsync(organizationId, userId, ct);
            if (!roleCodes.Any(r => r is PedagoraPilotRoleCodes.Trainer or
                PedagoraPilotRoleCodes.PedagogicalManager or PedagoraPilotRoleCodes.OrganizationAdministrator or
                PedagoraPilotRoleCodes.OrganizationDirection or PedagoraPilotRoleCodes.SiteDirection))
                throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        }
        return (organizationId, userId);
    }
    // External tutors must not receive internal notes, external student keys or document references.
    public static WorkplacePeriodDto Redact(WorkplacePeriodDto dto) => dto with
    {
        Notes = null,
        LearnerExternalKey = null,
        Documents = []
    };

    public static async Task<WorkplacePeriod> PeriodAsync(Guid id, ICurrentUser current, IWorkplaceTutorAssignmentRepository assignments,
        IOrganizationMemberRepository members, IWorkplacePeriodRepository periods, CancellationToken ct)
    {
        var (orgId, userId) = await RequireActiveMemberAsync(current, members, true, ct);
        // Never trust the email, period ID or client-provided evaluator identity on their own.
        if (!await assignments.HasActiveAssignmentAsync(orgId, id, userId, ct))
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        var period = await periods.Query(true).Include(p => p.Activities).Include(p => p.Documents).Include(p => p.Evaluations)
            .SingleOrDefaultAsync(p => p.Id == new WorkplacePeriodId(id) && p.OrganizationId == orgId, ct);
        if (period is not null && period.Status is WorkplacePeriodStatus.Cancelled or WorkplacePeriodStatus.Archived or WorkplacePeriodStatus.Deleted)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        return period ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
    }
}

public sealed class GetAssignedTutorPeriodsQueryHandler(
    IWorkplacePeriodRepository periods, IWorkplaceTutorAssignmentRepository assignments, IOrganizationMemberRepository members, ICurrentUser current,
    IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper)
    : IRequestHandler<GetAssignedTutorPeriodsQuery, IReadOnlyCollection<WorkplacePeriodDto>>
{
    public async Task<IReadOnlyCollection<WorkplacePeriodDto>> Handle(GetAssignedTutorPeriodsQuery request, CancellationToken ct)
    {
        var (organizationId, userId) = await WorkplaceTutorAuthorization.RequireActiveMemberAsync(current, members, false, ct);
        var ids = await assignments.GetActivePeriodIdsAsync(organizationId, userId, ct);
        if (ids.Count == 0) return [];
        // WorkplacePeriodId is an EF-converted value object. A predicate on
        // p.Id.Value cannot always be translated by Npgsql (HTTP 500).
        // Use an equality on the mapped identifier for each assigned period.
        // Never load unassigned periods and filter them in memory.
        var rows = new List<WorkplacePeriod>();
        foreach (var assignedId in ids.Distinct())
        {
            var periodId = new WorkplacePeriodId(assignedId);
            var period = await periods.Query(false)
                .Where(p => p.OrganizationId == organizationId && p.Id == periodId
                    && p.Status != WorkplacePeriodStatus.Cancelled
                    && p.Status != WorkplacePeriodStatus.Archived
                    && p.Status != WorkplacePeriodStatus.Deleted)
                .Include(p => p.Activities).Include(p => p.Documents).Include(p => p.Evaluations)
                .AsSplitQuery().SingleOrDefaultAsync(ct);
            if (period is not null) rows.Add(period);
        }
        rows = rows.OrderByDescending(p => p.StartDate).ToList();
        var result = new List<WorkplacePeriodDto>(rows.Count);
        foreach (var period in rows)
            result.Add(WorkplaceTutorAuthorization.Redact(await WorkplaceDtoFactory.Period(period, people, profiles, enrollments, mapper, ct)));
        return result;
    }
}

public sealed class UpdateTutorHoursCommandHandler(
    IWorkplacePeriodRepository periods, IWorkplaceTutorAssignmentRepository assignments, IOrganizationMemberRepository members, ICurrentUser current,
    IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper)
    : IRequestHandler<UpdateTutorHoursCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateTutorHoursCommand request, CancellationToken ct)
    {
        var period = await WorkplaceTutorAuthorization.PeriodAsync(request.PeriodId, current, assignments, members, periods, ct);
        if (request.CompletedHours < 0 || request.CompletedHours > period.PlannedMinutes / 60m)
            throw new ValidationApplicationException("WORKPLACE_COMPLETED_DURATION_INVALID");
        period.RecordCompletedMinutes(WorkplaceDtoFactory.Minutes(request.CompletedHours), request.Observation);
        return WorkplaceTutorAuthorization.Redact(await WorkplaceDtoFactory.Period(period, people, profiles, enrollments, mapper, ct));
    }
}

public sealed class UpdateTutorActivityCommandHandler(
    IWorkplacePeriodRepository periods, IWorkplaceTutorAssignmentRepository assignments, IOrganizationMemberRepository members, ICurrentUser current,
    IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper)
    : IRequestHandler<UpdateTutorActivityCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateTutorActivityCommand request, CancellationToken ct)
    {
        var period = await WorkplaceTutorAuthorization.PeriodAsync(request.PeriodId, current, assignments, members, periods, ct);
        if (!WorkplaceDtoFactory.TryActivity(request.Status, out var status) || !Enum.IsDefined(status))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceActivityStatusInvalid);
        if (!period.Activities.Any(a => a.Id == new WorkplaceActivityId(request.ActivityId)))
            throw new NotFoundApplicationException(ErrorKeys.WorkplaceActivityNotFound);
        period.SetActivity(new WorkplaceActivityId(request.ActivityId), status, request.Comment);
        return WorkplaceTutorAuthorization.Redact(await WorkplaceDtoFactory.Period(period, people, profiles, enrollments, mapper, ct));
    }
}

public sealed class SubmitTutorEvaluationCommandHandler(
    IWorkplacePeriodRepository periods, IWorkplaceTutorAssignmentRepository assignments, IOrganizationMemberRepository members, ICurrentUser current,
    IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper)
    : IRequestHandler<SubmitTutorEvaluationCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(SubmitTutorEvaluationCommand request, CancellationToken ct)
    {
        var period = await WorkplaceTutorAuthorization.PeriodAsync(request.PeriodId, current, assignments, members, periods, ct);
        if (string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Length > 4000)
            throw new ValidationApplicationException(ErrorKeys.WorkplaceEvaluatorRequired);
        var evaluator = current.DisplayName ?? current.Email ?? current.UserId?.ToString("D");
        if (string.IsNullOrWhiteSpace(evaluator))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceEvaluatorRequired);
        period.AddEvaluation(WorkplaceEvaluationKind.Tutor, evaluator, DateTimeOffset.UtcNow,
            request.Summary, request.Strengths, request.ImprovementAreas, request.Validated);
        return WorkplaceTutorAuthorization.Redact(await WorkplaceDtoFactory.Period(period, people, profiles, enrollments, mapper, ct));
    }
}
