using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning.Events;

namespace PedagoraPilot.Domain.Learning;

public sealed class LearnerTopicProgress : AggregateRoot<LearnerTopicProgressId>
{
    private readonly List<LearnerTopicEvaluationCriterion> _evaluationCriteria = [];

    private LearnerTopicProgress()
    {
    }

    private LearnerTopicProgress(
        LearnerTopicProgressId id,
        Guid organizationId,
        EnrollmentId enrollmentId,
        PedagogicalTopicId topicId) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("TOPIC_PROGRESS_ORGANIZATION_REQUIRED");
        if (enrollmentId.IsEmpty)
            throw new DomainException("TOPIC_PROGRESS_ENROLLMENT_REQUIRED");
        if (topicId.IsEmpty)
            throw new DomainException("TOPIC_PROGRESS_TOPIC_REQUIRED");

        OrganizationId = organizationId;
        EnrollmentId = enrollmentId;
        TopicId = topicId;
        Status = TopicProgressStatus.NotStarted;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public PedagogicalTopicId TopicId { get; private set; }
    public TopicProgressStatus Status { get; private set; }
    public DateOnly? PreparationDate { get; private set; }
    public DateOnly? PresentationDate { get; private set; }
    public int? PresentationDurationMinutes { get; private set; }
    public string? EvaluatorDisplayName { get; private set; }
    public string? PositivePoints { get; private set; }
    public string? Improvements { get; private set; }
    public string? Comment { get; private set; }
    public string? NextObjective { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<LearnerTopicEvaluationCriterion> EvaluationCriteria => _evaluationCriteria.AsReadOnly();

    public static LearnerTopicProgress Create(
        Guid organizationId,
        EnrollmentId enrollmentId,
        PedagogicalTopicId topicId) =>
        new(LearnerTopicProgressId.New(), organizationId, enrollmentId, topicId);

    public void Update(
        TopicProgressStatus status,
        DateOnly? preparationDate,
        DateOnly? presentationDate,
        int? presentationDurationMinutes,
        string? evaluatorDisplayName,
        string? positivePoints,
        string? improvements,
        string? comment,
        string? nextObjective,
        IEnumerable<(string Code, TopicEvaluationLevel Level)>? evaluationCriteria)
    {
        if (presentationDurationMinutes is <= 0 or > 1440)
            throw new DomainException("TOPIC_PRESENTATION_DURATION_INVALID");

        Status = status;
        PreparationDate = preparationDate;
        PresentationDate = presentationDate;
        PresentationDurationMinutes = presentationDurationMinutes;
        EvaluatorDisplayName = Normalize(evaluatorDisplayName, 200);
        PositivePoints = Normalize(positivePoints, 2000);
        Improvements = Normalize(improvements, 2000);
        Comment = Normalize(comment, 2000);
        NextObjective = Normalize(nextObjective, 2000);

        _evaluationCriteria.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var criterion in evaluationCriteria ?? [])
        {
            var code = criterion.Code?.Trim() ?? string.Empty;
            if (!seen.Add(code))
                throw new DomainException("TOPIC_EVALUATION_CRITERIA_INVALID");

            _evaluationCriteria.Add(new LearnerTopicEvaluationCriterion(
                LearnerTopicEvaluationCriterionId.New(),
                Id,
                code,
                criterion.Level));
        }

        UpdatedAtUtc = DateTime.UtcNow;
        var @event = new LearnerTopicProgressUpdatedDomainEvent(Id, EnrollmentId, TopicId, OrganizationId);
        RaiseDomainEvent(@event);
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}
