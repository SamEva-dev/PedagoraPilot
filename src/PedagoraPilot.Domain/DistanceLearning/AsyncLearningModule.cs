using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.DistanceLearning.Events;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.DistanceLearning;
public sealed class AsyncLearningModule : AggregateRoot<AsyncLearningModuleId>
{
    private readonly List<AsyncModuleStep> _steps = [];
    private AsyncLearningModule()
    {
    }

    private AsyncLearningModule(AsyncLearningModuleId id, Guid organizationId, Guid siteId, Guid programId, CohortId cohortId, string title, string? description, int estimatedMinutes, DateOnly dueDate, string trainerDisplayName, int expectedStudents) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("DISTANCE_ORGANIZATION_REQUIRED");
        if (siteId == Guid.Empty)
            throw new DomainException("DISTANCE_SITE_REQUIRED");
        if (programId == Guid.Empty)
            throw new DomainException("DISTANCE_PROGRAM_REQUIRED");
        if (cohortId.IsEmpty)
            throw new DomainException("DISTANCE_COHORT_REQUIRED");
        if (estimatedMinutes <= 0)
            throw new DomainException("DISTANCE_ESTIMATED_DURATION_INVALID");
        if (expectedStudents < 0)
            throw new DomainException("DISTANCE_EXPECTED_STUDENTS_INVALID");
        OrganizationId = organizationId;
        SiteId = siteId;
        ProgramId = programId;
        CohortId = cohortId;
        Title = Req(title, "DISTANCE_MODULE_TITLE_REQUIRED", 240);
        Description = Opt(description, 4000);
        EstimatedMinutes = estimatedMinutes;
        DueDate = dueDate;
        TrainerDisplayName = Req(trainerDisplayName, "DISTANCE_TRAINER_REQUIRED", 200);
        ExpectedStudents = expectedStudents;
        Status = AsyncLearningModuleStatus.NotStarted;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid ProgramId { get; private set; }
    public CohortId CohortId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string TrainerDisplayName { get; private set; } = string.Empty;
    public AsyncLearningModuleStatus Status { get; private set; }
    public int ProgressPercent { get; private set; }
    public int CompletedStudents { get; private set; }
    public int ExpectedStudents { get; private set; }
    public decimal? AverageScore { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<AsyncModuleStep> Steps => _steps.AsReadOnly();

    public static AsyncLearningModule Create(Guid organizationId, Guid siteId, Guid programId, CohortId cohortId, string title, string? description, int estimatedMinutes, DateOnly dueDate, string trainerDisplayName, int expectedStudents)
    {
        var x = new AsyncLearningModule(AsyncLearningModuleId.New(), organizationId, siteId, programId, cohortId, title, description, estimatedMinutes, dueDate, trainerDisplayName, expectedStudents);
        x.RaiseDomainEvent(new AsyncLearningModuleCreatedDomainEvent(x.Id, organizationId, cohortId));
        return x;
    }

    public void AddStep(string code, string label, int sortOrder)
    {
        if (_steps.Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("DISTANCE_MODULE_STEP_DUPLICATE");
        _steps.Add(new AsyncModuleStep(AsyncModuleStepId.New(), Id, code, label, sortOrder));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProgress(int progressPercent, int completedStudents, decimal? averageScore)
    {
        if (progressPercent is < 0 or > 100)
            throw new DomainException("DISTANCE_PROGRESS_INVALID");
        if (completedStudents < 0 || completedStudents > ExpectedStudents)
            throw new DomainException("DISTANCE_COMPLETED_STUDENTS_INVALID");
        if (averageScore is < 0 or > 100)
            throw new DomainException("DISTANCE_SCORE_INVALID");
        ProgressPercent = progressPercent;
        CompletedStudents = completedStudents;
        AverageScore = averageScore;
        Status = progressPercent switch
        {
            0 => DateOnly.FromDateTime(DateTime.UtcNow) > DueDate ? AsyncLearningModuleStatus.Late : AsyncLearningModuleStatus.NotStarted,
            100 => AsyncLearningModuleStatus.Completed,
            _ => DateOnly.FromDateTime(DateTime.UtcNow) > DueDate ? AsyncLearningModuleStatus.Late : AsyncLearningModuleStatus.InProgress
        };
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new AsyncLearningModuleProgressChangedDomainEvent(Id, OrganizationId, progressPercent));
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
