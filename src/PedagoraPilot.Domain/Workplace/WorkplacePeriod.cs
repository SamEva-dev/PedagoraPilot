using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workplace.Events;

namespace PedagoraPilot.Domain.Workplace;
public sealed class WorkplacePeriod : AggregateRoot<WorkplacePeriodId>
{
    private readonly List<WorkplaceActivity> _activities = [];
    private readonly List<WorkplaceDocumentChecklistItem> _documents = [];
    private readonly List<WorkplaceEvaluation> _evaluations = [];
    private WorkplacePeriod()
    {
    }

    private WorkplacePeriod(WorkplacePeriodId id, Guid organizationId, EnrollmentId enrollmentId, CohortId cohortId, Guid referentialVersionId, string periodTypeCode, string company, string city, string tutorName, string? tutorEmail, string? tutorPhone, DateOnly startDate, DateOnly endDate, int plannedMinutes, bool agreementReceived, string? notes) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("WORKPLACE_ORGANIZATION_REQUIRED");
        if (enrollmentId.IsEmpty)
            throw new DomainException("WORKPLACE_ENROLLMENT_REQUIRED");
        if (cohortId.IsEmpty)
            throw new DomainException("WORKPLACE_COHORT_REQUIRED");
        if (referentialVersionId == Guid.Empty)
            throw new DomainException("WORKPLACE_REFERENTIAL_REQUIRED");
        if (endDate < startDate)
            throw new DomainException("WORKPLACE_DATE_RANGE_INVALID");
        if (plannedMinutes <= 0)
            throw new DomainException("WORKPLACE_PLANNED_DURATION_INVALID");
        OrganizationId = organizationId;
        EnrollmentId = enrollmentId;
        CohortId = cohortId;
        ReferentialVersionId = referentialVersionId;
        PeriodTypeCode = Req(periodTypeCode, "WORKPLACE_PERIOD_TYPE_REQUIRED", 64).ToUpperInvariant();
        Company = Req(company, "WORKPLACE_COMPANY_REQUIRED", 200);
        City = Req(city, "WORKPLACE_CITY_REQUIRED", 120);
        TutorName = Req(tutorName, "WORKPLACE_TUTOR_REQUIRED", 200);
        TutorEmail = Opt(tutorEmail, 250);
        TutorPhone = Opt(tutorPhone, 60);
        StartDate = startDate;
        EndDate = endDate;
        PlannedMinutes = plannedMinutes;
        AgreementReceived = agreementReceived;
        Notes = Opt(notes, 4000);
        Status = WorkplacePeriodStatus.Planned;
        TrainerVisible = true;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public CohortId CohortId { get; private set; }
    public Guid ReferentialVersionId { get; private set; }
    public string PeriodTypeCode { get; private set; } = string.Empty;
    public string Company { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string TutorName { get; private set; } = string.Empty;
    public string? TutorEmail { get; private set; }
    public string? TutorPhone { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int PlannedMinutes { get; private set; }
    public int CompletedMinutes { get; private set; }
    public WorkplacePeriodStatus Status { get; private set; }
    public bool AgreementReceived { get; private set; }
    public bool TrainerVisible { get; private set; }
    public string? Notes { get; private set; }
    public string? TutorObservation { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<WorkplaceActivity> Activities => _activities.AsReadOnly();
    public IReadOnlyCollection<WorkplaceDocumentChecklistItem> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<WorkplaceEvaluation> Evaluations => _evaluations.AsReadOnly();

    public static WorkplacePeriod Create(Guid organizationId, EnrollmentId enrollmentId, CohortId cohortId, Guid referentialVersionId, string periodTypeCode, string company, string city, string tutorName, string? tutorEmail, string? tutorPhone, DateOnly startDate, DateOnly endDate, int plannedMinutes, bool agreementReceived, string? notes)
    {
        var x = new WorkplacePeriod(WorkplacePeriodId.New(), organizationId, enrollmentId, cohortId, referentialVersionId, periodTypeCode, company, city, tutorName, tutorEmail, tutorPhone, startDate, endDate, plannedMinutes, agreementReceived, notes);
        x.RaiseDomainEvent(new WorkplacePeriodCreatedDomainEvent(x.Id, organizationId, enrollmentId, cohortId));
        return x;
    }

    public void InitializeChecklist(IEnumerable<WorkplaceActivityDefinition> activities, IEnumerable<WorkplaceDocumentRequirement> documents)
    {
        foreach (var d in activities.OrderBy(x => x.SortOrder))
            if (_activities.All(x => x.DefinitionId != d.Id))
                _activities.Add(new(WorkplaceActivityId.New(), Id, d.Id, d.Code, d.Title, d.LabelKey, d.Mandatory));
        foreach (var d in documents.OrderBy(x => x.SortOrder))
            if (_documents.All(x => x.RequirementId != d.Id))
                _documents.Add(new(WorkplaceDocumentChecklistItemId.New(), Id, d.Id, d.Code, d.Title, d.LabelKey, d.Mandatory, d.Code == "AGREEMENT" && AgreementReceived ? WorkplaceDocumentStatus.Available : WorkplaceDocumentStatus.Missing));
    }

    public void UpdateDetails(string company, string city, string tutorName, string? tutorEmail, string? tutorPhone, DateOnly startDate, DateOnly endDate, int plannedMinutes, bool trainerVisible, string? notes)
    {
        EnsureMutable();
        if (endDate < startDate)
            throw new DomainException("WORKPLACE_DATE_RANGE_INVALID");
        if (plannedMinutes <= 0)
            throw new DomainException("WORKPLACE_PLANNED_DURATION_INVALID");
        if (plannedMinutes < CompletedMinutes)
            throw new DomainException("WORKPLACE_PLANNED_BELOW_COMPLETED");
        Company = Req(company, "WORKPLACE_COMPANY_REQUIRED", 200);
        City = Req(city, "WORKPLACE_CITY_REQUIRED", 120);
        TutorName = Req(tutorName, "WORKPLACE_TUTOR_REQUIRED", 200);
        TutorEmail = Opt(tutorEmail, 250);
        TutorPhone = Opt(tutorPhone, 60);
        StartDate = startDate;
        EndDate = endDate;
        PlannedMinutes = plannedMinutes;
        TrainerVisible = trainerVisible;
        Notes = Opt(notes, 4000);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new WorkplacePeriodUpdatedDomainEvent(Id, OrganizationId));
    }

    public void RecordCompletedMinutes(int completedMinutes, string? tutorObservation = null)
    {
        EnsureMutable();
        if (completedMinutes < 0 || completedMinutes > PlannedMinutes)
            throw new DomainException("WORKPLACE_COMPLETED_DURATION_INVALID");
        CompletedMinutes = completedMinutes;
        TutorObservation = Opt(tutorObservation, 4000) ?? TutorObservation;
        Status = completedMinutes == 0 ? WorkplacePeriodStatus.Planned : completedMinutes < PlannedMinutes ? WorkplacePeriodStatus.InProgress : WorkplacePeriodStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new WorkplaceHoursUpdatedDomainEvent(Id, OrganizationId, completedMinutes));
    }

    public void SetActivity(WorkplaceActivityId activityId, WorkplaceActivityStatus status, string? comment)
    {
        EnsureMutable();
        var a = _activities.SingleOrDefault(x => x.Id == activityId) ?? throw new DomainException("WORKPLACE_ACTIVITY_NOT_FOUND");
        a.SetStatus(status, comment);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new WorkplaceActivityUpdatedDomainEvent(Id, OrganizationId, activityId, status));
    }

    public void SetDocument(WorkplaceDocumentChecklistItemId itemId, WorkplaceDocumentStatus status, Guid? documentId)
    {
        EnsureMutable();
        var d = _documents.SingleOrDefault(x => x.Id == itemId) ?? throw new DomainException("WORKPLACE_DOCUMENT_ITEM_NOT_FOUND");
        d.SetStatus(status, documentId);
        if (d.Code == "AGREEMENT")
            AgreementReceived = status != WorkplaceDocumentStatus.Missing;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new WorkplaceDocumentStatusUpdatedDomainEvent(Id, OrganizationId, itemId, status));
    }

    public WorkplaceEvaluation AddEvaluation(WorkplaceEvaluationKind kind, string evaluatorDisplayName, DateTimeOffset evaluatedAtUtc, string summary, string? strengths, string? improvementAreas, bool? validated)
    {
        EnsureMutable();
        var e = new WorkplaceEvaluation(WorkplaceEvaluationId.New(), Id, kind, evaluatorDisplayName, evaluatedAtUtc, summary, strengths, improvementAreas, validated);
        _evaluations.Add(e);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new WorkplaceEvaluationRecordedDomainEvent(Id, OrganizationId, e.Id, kind));
        return e;
    }

    public void MarkIncomplete(string? observation)
    {
        EnsureMutable();
        Status = WorkplacePeriodStatus.Incomplete;
        TutorObservation = Opt(observation, 4000) ?? TutorObservation;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new WorkplacePeriodUpdatedDomainEvent(Id, OrganizationId));
    }

    public void Cancel()
    {
        if (Status == WorkplacePeriodStatus.Completed)
            throw new DomainException("WORKPLACE_COMPLETED_LOCKED");
        Status = WorkplacePeriodStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new WorkplacePeriodUpdatedDomainEvent(Id, OrganizationId));
    }

    private void EnsureMutable()
    {
        if (Status is WorkplacePeriodStatus.Completed or WorkplacePeriodStatus.Cancelled)
            throw new DomainException("WORKPLACE_PERIOD_LOCKED");
    }

    private static string Req(string? v, string k, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(k);
        var x = v.Trim();
        if (x.Length > max)
            throw new DomainException(k);
        return x;
    }

    private static string? Opt(string? v, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            return null;
        var x = v.Trim();
        return x.Length <= max ? x : x[..max];
    }
}
