using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Learning;

public sealed class LearnerTopicEvaluationCriterion : Entity<LearnerTopicEvaluationCriterionId>
{
    private LearnerTopicEvaluationCriterion()
    {
    }

    internal LearnerTopicEvaluationCriterion(
        LearnerTopicEvaluationCriterionId id,
        LearnerTopicProgressId learnerTopicProgressId,
        string code,
        TopicEvaluationLevel level) : base(id)
    {
        if (learnerTopicProgressId.IsEmpty)
            throw new DomainException("TOPIC_EVALUATION_PROGRESS_REQUIRED");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("TOPIC_EVALUATION_CRITERION_CODE_REQUIRED");

        var normalizedCode = code.Trim();
        if (normalizedCode.Length > 80)
            throw new DomainException("TOPIC_EVALUATION_CRITERION_CODE_INVALID");

        LearnerTopicProgressId = learnerTopicProgressId;
        Code = normalizedCode;
        Level = level;
    }

    public LearnerTopicProgressId LearnerTopicProgressId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public TopicEvaluationLevel Level { get; private set; }
}
