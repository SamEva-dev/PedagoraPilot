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
    public static bool IsTrainer(ICurrentUser current) =>
        current.Roles.Contains("PedagoraPilot.Trainer") || current.Roles.Contains("PedagoraPilot.PedagogicalManager");
    public static void EnsureTrainerVisibility(WorkplacePeriod period, ICurrentUser current)
    {
        if (IsTrainer(current) && !period.TrainerVisible)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
    }
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

        return Period(x, person?.DisplayName ?? "", enrollment?.ExternalKey, mapper);
    }
    public static WorkplacePeriodDto Period(WorkplacePeriod x, string learnerDisplayName, string? learnerExternalKey, IObjectMapper mapper)
    {
        var m = mapper.Map<WorkplacePeriod, WorkplacePeriodReadModel>(x);
        return new(m.Id.Value, m.EnrollmentId.Value, m.CohortId.Value, m.ReferentialVersionId, m.PeriodTypeCode, learnerDisplayName, learnerExternalKey, m.Company, m.City, m.TutorName, m.TutorEmail, m.TutorPhone, m.StartDate, m.EndDate, Hours(m.PlannedMinutes), Hours(m.CompletedMinutes), Status(m.Status), m.AgreementReceived, m.TrainerVisible, m.Notes, m.TutorObservation, x.Activities.Select(a =>
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

public sealed class GetWorkplacePeriodsQueryHandler(IWorkplacePeriodRepository periods, WorkplaceContextualAccess contextualAccess, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetWorkplacePeriodsQuery, IReadOnlyCollection<WorkplacePeriodDto>>
{
    public async Task<IReadOnlyCollection<WorkplacePeriodDto>> Handle(GetWorkplacePeriodsQuery r, CancellationToken ct)
    {
        PedagoraPilot.Domain.Identifiers.EnrollmentId[]? selfEnrollmentIds = null;
        if (PedagoraPilot.Application.Training.Learners.LearnerSelfAccess.Applies(current))
        {
            var profile = await PedagoraPilot.Application.Training.Learners.LearnerSelfAccess
                .ResolveProfileAsync(profiles, people, current, ct);
            if (profile is null) return [];

            var selfEnrollmentQuery = enrollments.Query(false)
                .Where(x => x.LearnerProfileId == profile.Id);
            var selfOrganizationId = TenantScope.Organization(current);
            if (selfOrganizationId.HasValue)
                selfEnrollmentQuery = selfEnrollmentQuery.Where(x => x.OrganizationId == selfOrganizationId.Value);
            if (r.CohortId.HasValue)
                selfEnrollmentQuery = selfEnrollmentQuery.Where(x => x.CohortId == r.CohortId.Value);

            selfEnrollmentIds = await selfEnrollmentQuery
                .Select(x => x.Id)
                .ToArrayAsync(ct);

            if (r.EnrollmentId.HasValue && !selfEnrollmentIds.Contains(r.EnrollmentId.Value))
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        }
        IQueryable<WorkplacePeriod> q = periods.Query(false).Include(x => x.Activities).Include(x => x.Documents).Include(x => x.Evaluations).AsSplitQuery();
        var organizationId = TenantScope.Organization(current);
        if (organizationId.HasValue)
            q = q.Where(x => x.OrganizationId == organizationId.Value);
        if (r.CohortId.HasValue)
            q = q.Where(x => x.CohortId == r.CohortId.Value);
        if (r.EnrollmentId.HasValue)
            q = q.Where(x => x.EnrollmentId == r.EnrollmentId.Value);
        if (selfEnrollmentIds is not null)
            q = q.Where(x => selfEnrollmentIds.Contains(x.EnrollmentId));
        if (WorkplaceDtoFactory.IsTrainer(current))
            q = q.Where(x => x.TrainerVisible);
        var rows = await q.OrderByDescending(x => x.StartDate).ToListAsync(ct);
        if (current.HasContextualScopeRestrictions)
        {
            var visible = new List<WorkplacePeriod>(rows.Count);
            foreach (var row in rows)
            {
                if (await contextualAccess.CanViewAsync(row, ct)) visible.Add(row);
            }
            rows = visible;
        }
        if (rows.Count == 0) return [];
        var enrollmentIds = rows.Select(x => x.EnrollmentId).Distinct().ToArray();
        var enrollmentsById = (await enrollments.Query(false).Where(x => enrollmentIds.Contains(x.Id)).ToListAsync(ct)).ToDictionary(x => x.Id);
        var profileIds = enrollmentsById.Values.Select(x => x.LearnerProfileId).Distinct().ToArray();
        var profilesById = (await profiles.Query(false).Where(x => profileIds.Contains(x.Id)).ToListAsync(ct)).ToDictionary(x => x.Id);
        var personIds = profilesById.Values.Select(x => x.PersonId).Distinct().ToArray();
        var peopleById = (await people.Query(false).Where(x => personIds.Contains(x.Id)).ToListAsync(ct)).ToDictionary(x => x.Id);
        var result = new List<WorkplacePeriodDto>();
        foreach (var x in rows)
        {
            enrollmentsById.TryGetValue(x.EnrollmentId, out var enrollment);
            var profile = enrollment is not null && profilesById.TryGetValue(enrollment.LearnerProfileId, out var foundProfile) ? foundProfile : null;
            var person = profile is not null && peopleById.TryGetValue(profile.PersonId, out var foundPerson) ? foundPerson : null;
            result.Add(WorkplaceDtoFactory.Period(x, person?.DisplayName ?? "", enrollment?.ExternalKey, mapper));
        }
        return result;
    }
}

public sealed class GetWorkplacePeriodQueryHandler(IWorkplacePeriodRepository periods, WorkplaceContextualAccess contextualAccess, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetWorkplacePeriodQuery, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(GetWorkplacePeriodQuery r, CancellationToken ct)
    {
        var x = await periods.Query(false).Include(a => a.Activities).Include(a => a.Documents).Include(a => a.Evaluations).SingleOrDefaultAsync(a => a.Id == r.Id, ct) ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
        if (TenantScope.Organization(current) is Guid scopeId && x.OrganizationId != scopeId)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        await contextualAccess.EnsureAsync(x, manage: false, ct);
        WorkplaceDtoFactory.EnsureTrainerVisibility(x, current);
        var enrollment = await enrollments.GetByIdAsync(x.EnrollmentId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        await PedagoraPilot.Application.Training.Learners.LearnerSelfAccess.EnsureAsync(enrollment, profiles, people, current, ct);
        return await WorkplaceDtoFactory.Period(x, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class GetWorkplacePeriodTypesQueryHandler(IWorkplaceActivityDefinitionRepository activities, IWorkplaceDocumentRequirementRepository documents, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<GetWorkplacePeriodTypesQuery, IReadOnlyCollection<string>>
{
    public async Task<IReadOnlyCollection<string>> Handle(GetWorkplacePeriodTypesQuery r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct);
        var fromActivities = await activities.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Active).Select(x => x.PeriodTypeCode).Distinct().ToListAsync(ct);
        var fromDocuments = await documents.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Active).Select(x => x.PeriodTypeCode).Distinct().ToListAsync(ct);
        return fromActivities.Concat(fromDocuments).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
    }
}

public sealed class GetWorkplaceRequirementsQueryHandler(IWorkplaceActivityDefinitionRepository a, IWorkplaceDocumentRequirementRepository d, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<GetWorkplaceRequirementsQuery, IReadOnlyCollection<WorkplaceRequirementDto>>
{
    public async Task<IReadOnlyCollection<WorkplaceRequirementDto>> Handle(GetWorkplaceRequirementsQuery r, CancellationToken ct)
    {
        var p = r.PeriodTypeCode.Trim().ToUpperInvariant();
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct);
        var activities = await a.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.PeriodTypeCode == p && x.Active).OrderBy(x => x.SortOrder).Select(x => new WorkplaceRequirementDto(x.Id.Value, x.Code, x.Title, x.LabelKey, x.Mandatory, x.SortOrder, "activity")).ToListAsync(ct);
        var docs = await d.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.PeriodTypeCode == p && x.Active).OrderBy(x => x.SortOrder).Select(x => new WorkplaceRequirementDto(x.Id.Value, x.Code, x.Title, x.LabelKey, x.Mandatory, x.SortOrder, "document")).ToListAsync(ct);
        return activities.Concat(docs).ToArray();
    }
}

public sealed class CreateWorkplacePeriodCommandHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, IWorkplacePeriodRepository periods, IWorkplaceActivityDefinitionRepository activities, IWorkplaceDocumentRequirementRepository documents, IPersonRepository people, ILearnerProfileRepository profiles, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<CreateWorkplacePeriodCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(CreateWorkplacePeriodCommand r, CancellationToken ct)
    {
        var e = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        var c = await cohorts.GetByIdAsync(e.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (TenantScope.Organization(current) is Guid scopeId && e.OrganizationId != scopeId)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        await ContextualScope.EnsureCanManageCohortAsync(current, c, offerings, ct);
        var code = r.PeriodTypeCode.Trim().ToUpperInvariant();
        if (!await activities.Query(false).AnyAsync(x => x.ReferentialVersionId == c.ReferentialVersionId && x.PeriodTypeCode == code && x.Active, ct)
            && !await documents.Query(false).AnyAsync(x => x.ReferentialVersionId == c.ReferentialVersionId && x.PeriodTypeCode == code && x.Active, ct))
            throw new ValidationApplicationException(ErrorKeys.WorkplacePeriodTypeInvalid);
        if (await periods.OverlapsAsync(e.Id, r.StartDate, r.EndDate, null, ct))
            throw new ConflictApplicationException(ErrorKeys.WorkplacePeriodOverlap);
        var p = WorkplacePeriod.Create(e.OrganizationId, e.Id, c.Id, c.ReferentialVersionId, r.PeriodTypeCode, r.Company, r.City, r.TutorName, r.TutorEmail, r.TutorPhone, r.StartDate, r.EndDate, WorkplaceDtoFactory.Minutes(r.PlannedHours), r.AgreementReceived, r.Notes);
        var defs = await activities.Query(false).Where(x => x.ReferentialVersionId == c.ReferentialVersionId && x.PeriodTypeCode == code && x.Active).ToListAsync(ct);
        var docs = await documents.Query(false).Where(x => x.ReferentialVersionId == c.ReferentialVersionId && x.PeriodTypeCode == code && x.Active).ToListAsync(ct);
        p.InitializeChecklist(defs, docs);
        await periods.AddAsync(p, ct);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class UpdateWorkplacePeriodCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current, WorkplaceContextualAccess contextualAccess) : IRequestHandler<UpdateWorkplacePeriodCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplacePeriodCommand r, CancellationToken ct)
    {
        var p = await periods.Query(true).Include(x => x.Activities).Include(x => x.Documents).Include(x => x.Evaluations).SingleOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
        TenantScope.Ensure(current, p.OrganizationId);
        await contextualAccess.EnsureAsync(p, manage: true, ct);
        WorkplaceDtoFactory.EnsureTrainerVisibility(p, current);
        if (await periods.OverlapsAsync(p.EnrollmentId, r.StartDate, r.EndDate, p.Id, ct))
            throw new ConflictApplicationException(ErrorKeys.WorkplacePeriodOverlap);
        p.UpdateDetails(r.Company, r.City, r.TutorName, r.TutorEmail, r.TutorPhone, r.StartDate, r.EndDate, WorkplaceDtoFactory.Minutes(r.PlannedHours), r.TrainerVisible, r.Notes);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class UpdateWorkplaceHoursCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current, WorkplaceContextualAccess contextualAccess) : IRequestHandler<UpdateWorkplaceHoursCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplaceHoursCommand r, CancellationToken ct)
    {
        var p = await Load(periods, r.Id, ct);
        TenantScope.Ensure(current, p.OrganizationId);
        await contextualAccess.EnsureAsync(p, manage: true, ct);
        WorkplaceDtoFactory.EnsureTrainerVisibility(p, current);
        p.RecordCompletedMinutes(WorkplaceDtoFactory.Minutes(r.CompletedHours), r.TutorObservation);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }

    internal static Task<WorkplacePeriod?> Q(IWorkplacePeriodRepository p, WorkplacePeriodId id, CancellationToken ct) => p.Query(true).Include(x => x.Activities).Include(x => x.Documents).Include(x => x.Evaluations).SingleOrDefaultAsync(x => x.Id == id, ct);
    internal static async Task<WorkplacePeriod> Load(IWorkplacePeriodRepository p, WorkplacePeriodId id, CancellationToken ct) => await Q(p, id, ct) ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
}

public sealed class UpdateWorkplaceActivityCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current, WorkplaceContextualAccess contextualAccess) : IRequestHandler<UpdateWorkplaceActivityCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplaceActivityCommand r, CancellationToken ct)
    {
        var p = await UpdateWorkplaceHoursCommandHandler.Load(periods, r.PeriodId, ct);
        TenantScope.Ensure(current, p.OrganizationId);
        await contextualAccess.EnsureAsync(p, manage: true, ct);
        WorkplaceDtoFactory.EnsureTrainerVisibility(p, current);
        if (!WorkplaceDtoFactory.TryActivity(r.Status, out var s) || !Enum.IsDefined(s))
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

public sealed class UpdateWorkplaceDocumentCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IDocumentRepository documents, IObjectMapper mapper, ICurrentUser current, WorkplaceContextualAccess contextualAccess) : IRequestHandler<UpdateWorkplaceDocumentCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(UpdateWorkplaceDocumentCommand r, CancellationToken ct)
    {
        var p = await UpdateWorkplaceHoursCommandHandler.Load(periods, r.PeriodId, ct);
        TenantScope.Ensure(current, p.OrganizationId);
        await contextualAccess.EnsureAsync(p, manage: true, ct);
        WorkplaceDtoFactory.EnsureTrainerVisibility(p, current);
        if (!WorkplaceDtoFactory.TryDocument(r.Status, out var s) || !Enum.IsDefined(s))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceDocumentStatusInvalid);
        if (s != WorkplaceDocumentStatus.Missing && !r.DocumentId.HasValue
            && !(s == WorkplaceDocumentStatus.Available && p.Documents.Any(x => x.Id == r.ItemId && x.Code == "AGREEMENT")))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceDocumentRequired);
        if (r.DocumentId.HasValue && !await documents.Query(false).AnyAsync(x => x.Id.Value == r.DocumentId.Value
            && x.OrganizationId == p.OrganizationId && x.Status == PedagoraPilot.Domain.Documents.DocumentStatus.Active
            && x.Versions.OrderByDescending(v => v.VersionNumber).Take(1)
                .Any(v => v.BlobAvailable && v.SecurityStatus == PedagoraPilot.Domain.Documents.DocumentSecurityStatus.Clean)
            && (x.Category == PedagoraPilot.Domain.Documents.DocumentCategory.Internship
                || x.Category == PedagoraPilot.Domain.Documents.DocumentCategory.Administrative)
            && (!x.CohortId.HasValue || x.CohortId == p.CohortId.Value)
            && ((x.OwnerType == PedagoraPilot.Domain.Documents.DocumentOwnerType.Enrollment && x.OwnerId == p.EnrollmentId.Value)
                || (x.OwnerType == PedagoraPilot.Domain.Documents.DocumentOwnerType.WorkplacePeriod && x.OwnerId == p.Id.Value)
                || (x.OwnerType == PedagoraPilot.Domain.Documents.DocumentOwnerType.Cohort && x.OwnerId == p.CohortId.Value)), ct))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        try
        {
            p.SetDocument(r.ItemId, s, s == WorkplaceDocumentStatus.Missing ? null : r.DocumentId);
        }
        catch (DomainException e)when (e.Message == "WORKPLACE_DOCUMENT_ITEM_NOT_FOUND")
        {
            throw new NotFoundApplicationException(ErrorKeys.WorkplaceDocumentItemNotFound);
        }

        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}

public sealed class RecordWorkplaceEvaluationCommandHandler(IWorkplacePeriodRepository periods, IPersonRepository people, ILearnerProfileRepository profiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current, WorkplaceContextualAccess contextualAccess) : IRequestHandler<RecordWorkplaceEvaluationCommand, WorkplacePeriodDto>
{
    public async Task<WorkplacePeriodDto> Handle(RecordWorkplaceEvaluationCommand r, CancellationToken ct)
    {
        var p = await UpdateWorkplaceHoursCommandHandler.Load(periods, r.PeriodId, ct);
        TenantScope.Ensure(current, p.OrganizationId);
        await contextualAccess.EnsureAsync(p, manage: true, ct);
        WorkplaceDtoFactory.EnsureTrainerVisibility(p, current);
        if (!WorkplaceDtoFactory.TryEvaluation(r.Kind, out var k) || !Enum.IsDefined(k))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceEvaluationKindInvalid);
        var evaluator = current.DisplayName ?? current.Email ?? current.UserId?.ToString("D");
        if (string.IsNullOrWhiteSpace(evaluator))
            throw new ValidationApplicationException(ErrorKeys.WorkplaceEvaluatorRequired);
        p.AddEvaluation(k, evaluator, DateTimeOffset.UtcNow, r.Summary, r.Strengths, r.ImprovementAreas, r.Validated);
        return await WorkplaceDtoFactory.Period(p, people, profiles, enrollments, mapper, ct);
    }
}
