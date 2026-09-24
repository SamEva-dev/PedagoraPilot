using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.Workplace;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Application.Workplace;
internal static class WorkplaceDtoFactory
{
    public static async Task<WorkplacePeriodDto> Period(WorkplacePeriod x, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(x.EnrollmentId, false, ct);
        LearnerProfile? profile = null;
        Person? person = null;
        if (enrollment is not null)
        {
            profile = await profiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct);
            if (profile is not null)
                person = await people.GetByIdAsync(profile.PersonId, false, ct);
        }

        var m = mapper.Map<WorkplacePeriod, WorkplacePeriodReadModel>(x);
        return new(m.Id.Value, m.EnrollmentId.Value, m.CohortId.Value, m.ReferentialVersionId, m.PeriodTypeCode, person?.DisplayName ?? "", enrollment?.ExternalKey, m.Company, m.City, m.TutorName, m.TutorEmail, m.TutorPhone, m.StartDate, m.EndDate, Hours(m.PlannedMinutes), Hours(m.CompletedMinutes), Status(m.Status), m.AgreementReceived, m.TrainerVisible, m.Notes, m.TutorObservation, x.Activities.Select(a =>
        {
            var q = mapper.Map<WorkplaceActivity, WorkplaceActivityReadModel>(a);
            return new WorkplaceActivityDto(q.Id.Value, q.DefinitionId.Value, q.Code, q.Title, q.LabelKey, q.Mandatory, ActivityStatus(q.Status), q.Comment);
        }).ToArray(), x.Documents.Select(d =>
        {
            var q = mapper.Map<WorkplaceDocumentChecklistItem, WorkplaceDocumentReadModel>(d);
            return new WorkplaceDocumentDto(q.Id.Value, q.RequirementId.Value, q.Code, q.Title, q.LabelKey, q.Mandatory, DocumentStatus(q.Status), q.DocumentId);
        }).ToArray(), x.Evaluations.Select(e =>
        {
            var q = mapper.Map<WorkplaceEvaluation, WorkplaceEvaluationReadModel>(e);
            return new WorkplaceEvaluationDto(q.Id.Value, q.Kind.ToString().ToLowerInvariant(), q.EvaluatorDisplayName, q.EvaluatedAtUtc, q.Summary, q.Strengths, q.ImprovementAreas, q.Validated);
        }).ToArray());
    }

    public static int Minutes(decimal h) => checked((int)Math.Round(h * 60m, MidpointRounding.AwayFromZero));
    public static decimal Hours(int m) => Math.Round(m / 60m, 2);
    public static string Status(WorkplacePeriodStatus x) => x switch
    {
        WorkplacePeriodStatus.InProgress => "inProgress",
        WorkplacePeriodStatus.Completed => "completed",
        WorkplacePeriodStatus.Incomplete => "incomplete",
        WorkplacePeriodStatus.Cancelled => "cancelled",
        _ => "planned"
    };
    public static string ActivityStatus(WorkplaceActivityStatus x) => x == WorkplaceActivityStatus.Done ? "done" : x == WorkplaceActivityStatus.NotApplicable ? "notApplicable" : "pending";
    public static string DocumentStatus(WorkplaceDocumentStatus x) => x == WorkplaceDocumentStatus.Validated ? "validated" : x == WorkplaceDocumentStatus.Available ? "available" : "missing";
    public static bool TryActivity(string v, out WorkplaceActivityStatus x) => Enum.TryParse((v ?? "").Replace("_", "").Replace("-", ""), true, out x);
    public static bool TryDocument(string v, out WorkplaceDocumentStatus x) => Enum.TryParse((v ?? "").Replace("_", "").Replace("-", ""), true, out x);
    public static bool TryEvaluation(string v, out WorkplaceEvaluationKind x) => Enum.TryParse((v ?? "").Replace("_", "").Replace("-", ""), true, out x);
}

public sealed class GetWorkplacePeriodsQueryHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetWorkplacePeriodsQuery, IReadOnlyCollection<WorkplacePeriodDto>>
{
    public async Task<IReadOnlyCollection<WorkplacePeriodDto>> Handle(GetWorkplacePeriodsQuery r, CancellationToken ct)
    {
        IQueryable<WorkplacePeriod> q = periods.Query(false).Include(x => x.Activities).Include(x => x.Documents).Include(x => x.Evaluations);
        if (current.OrganizationId.HasValue)
            q = q.Where(x => x.OrganizationId == current.OrganizationId.Value);
        if (r.CohortId.HasValue)
            q = q.Where(x => x.CohortId == r.CohortId.Value);
        if (r.EnrollmentId.HasValue)
            q = q.Where(x => x.EnrollmentId == r.EnrollmentId.Value);
        var rows = await q.OrderByDescending(x => x.StartDate).ToListAsync(ct);
        var result = new List<WorkplacePeriodDto>();
        foreach (var x in rows)
            result.Add(await WorkplaceDtoFactory.Period(x, people, profiles, enrollments, mapper, ct));
        return result;
    }
}

public sealed class GetWorkplacePeriodQueryHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetWorkplacePeriodQuery, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(GetWorkplacePeriodQuery r, CancellationToken ct)
    {
        var x = await periods.Query(false).Include(a => a.Activities).Include(a => a.Documents).Include(a => a.Evaluations).SingleOrDefaultAsync(a => a.Id == r.Id, ct) ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
        if (current.OrganizationId.HasValue && x.OrganizationId != current.OrganizationId)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        return await WorkplaceDtoFactory.Period(x, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class GetWorkplaceRequirementsQueryHandler(IWorkplaceActivityDefinitionRepository a, IWorkplaceDocumentRequirementRepository d) : IRequestHandler<GetWorkplaceRequirementsQuery, IReadOnlyCollection<WorkplaceRequirementDto>>
{
    public async Task<IReadOnlyCollection<WorkplaceRequirementDto>> Handle(GetWorkplaceRequirementsQuery r, CancellationToken ct)
    {
        var p = r.PeriodTypeCode.Trim().ToUpperInvariant();
        var activities = await a.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.PeriodTypeCode == p && x.Active).OrderBy(x => x.SortOrder).Select(x => new WorkplaceRequirementDto(x.Id.Value, x.Code, x.Title, x.LabelKey, x.Mandatory, x.SortOrder, "activity")).ToListAsync(ct);
        var docs = await d.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.PeriodTypeCode == p && x.Active).OrderBy(x => x.SortOrder).Select(x => new WorkplaceRequirementDto(x.Id.Value, x.Code, x.Title, x.LabelKey, x.Mandatory, x.SortOrder, "document")).ToListAsync(ct);
        return activities.Concat(docs).ToArray();
    }
}

public sealed class CreateWorkplacePeriodCommandHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IWorkplacePeriodRepository periods, IWorkplaceActivityDefinitionRepository activities, IWorkplaceDocumentRequirementRepository documents, IPersonRepository people, ILearnerProfileRepository profiles, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<CreateWorkplacePeriodCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(CreateWorkplacePeriodCommand r, CancellationToken ct)
    {
        var e = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        var c = await cohorts.GetByIdAsync(e.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (current.OrganizationId.HasValue && e.OrganizationId != current.OrganizationId)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        if (await periods.OverlapsAsync(e.Id, r.StartDate, r.EndDate, null, ct))
            throw new ConflictApplicationException(ErrorKeys.WorkplacePeriodOverlap);
        var p = WorkplacePeriod.Create(e.OrganizationId, e.Id, c.Id, c.ReferentialVersionId, r.PeriodTypeCode, r.Company, r.City, r.TutorName, r.TutorEmail, r.TutorPhone, r.StartDate, r.EndDate, WorkplaceDtoFactory.Minutes(r.PlannedHours), r.AgreementReceived, r.Notes);
        var code = r.PeriodTypeCode.Trim().ToUpperInvariant();
        var defs = await activities.Query(false).Where(x => x.ReferentialVersionId == c.ReferentialVersionId && x.PeriodTypeCode == code && x.Active).ToListAsync(ct);
        var docs = await documents.Query(false).Where(x => x.ReferentialVersionId == c.ReferentialVersionId && x.PeriodTypeCode == code && x.Active).ToListAsync(ct);
        p.InitializeChecklist(defs, docs);
        await periods.AddAsync(p, ct);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class UpdateWorkplacePeriodCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<UpdateWorkplacePeriodCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplacePeriodCommand r, CancellationToken ct)
    {
        var p = await periods.Query(true).Include(x => x.Activities).Include(x => x.Documents).Include(x => x.Evaluations).SingleOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
        if (await periods.OverlapsAsync(p.EnrollmentId, r.StartDate, r.EndDate, p.Id, ct))
            throw new ConflictApplicationException(ErrorKeys.WorkplacePeriodOverlap);
        p.UpdateDetails(r.Company, r.City, r.TutorName, r.TutorEmail, r.TutorPhone, r.StartDate, r.EndDate, WorkplaceDtoFactory.Minutes(r.PlannedHours), r.TrainerVisible, r.Notes);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class UpdateWorkplaceHoursCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<UpdateWorkplaceHoursCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplaceHoursCommand r, CancellationToken ct)
    {
        var p = await Load(periods, r.Id, ct);
        p.RecordCompletedMinutes(WorkplaceDtoFactory.Minutes(r.CompletedHours), r.TutorObservation);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }

    internal static Task<WorkplacePeriod?> Q(IWorkplacePeriodRepository p, WorkplacePeriodId id, CancellationToken ct) => p.Query(true).Include(x => x.Activities).Include(x => x.Documents).Include(x => x.Evaluations).SingleOrDefaultAsync(x => x.Id == id, ct);
    internal static async Task<WorkplacePeriod> Load(IWorkplacePeriodRepository p, WorkplacePeriodId id, CancellationToken ct) => await Q(p, id, ct) ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
}

public sealed class UpdateWorkplaceActivityCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<UpdateWorkplaceActivityCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplaceActivityCommand r, CancellationToken ct)
    {
        var p = await UpdateWorkplaceHoursCommandHandler.Load(periods, r.PeriodId, ct);
        if (!WorkplaceDtoFactory.TryActivity(r.Status, out var s))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceActivityStatusInvalid);
        try
        {
            p.SetActivity(r.ActivityId, s, r.Comment);
        }
        catch (DomainException e)when (e.Message == "WORKPLACE_ACTIVITY_NOT_FOUND")
        {
            throw new NotFoundApplicationException(ErrorKeys.WorkplaceActivityNotFound);
        }

        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class UpdateWorkplaceDocumentCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<UpdateWorkplaceDocumentCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplaceDocumentCommand r, CancellationToken ct)
    {
        var p = await UpdateWorkplaceHoursCommandHandler.Load(periods, r.PeriodId, ct);
        if (!WorkplaceDtoFactory.TryDocument(r.Status, out var s))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceDocumentStatusInvalid);
        try
        {
            p.SetDocument(r.ItemId, s, r.DocumentId);
        }
        catch (DomainException e)when (e.Message == "WORKPLACE_DOCUMENT_ITEM_NOT_FOUND")
        {
            throw new NotFoundApplicationException(ErrorKeys.WorkplaceDocumentItemNotFound);
        }

        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class RecordWorkplaceEvaluationCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<RecordWorkplaceEvaluationCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(RecordWorkplaceEvaluationCommand r, CancellationToken ct)
    {
        var p = await UpdateWorkplaceHoursCommandHandler.Load(periods, r.PeriodId, ct);
        if (!WorkplaceDtoFactory.TryEvaluation(r.Kind, out var k))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceEvaluationKindInvalid);
        p.AddEvaluation(k, r.EvaluatorDisplayName, r.EvaluatedAtUtc ?? DateTimeOffset.UtcNow, r.Summary, r.Strengths, r.ImprovementAreas, r.Validated);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}
