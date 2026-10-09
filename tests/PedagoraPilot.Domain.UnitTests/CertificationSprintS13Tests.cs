using FluentAssertions;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class CertificationSprintS13Tests
{
    private static CertificationCandidate Candidate()
    {
        var candidate = CertificationCandidate.Register(Guid.NewGuid(), CertificationExamSessionId.New(), EnrollmentId.New());
        candidate.SetEligibility(true, "{}");
        return candidate;
    }

    private static CertificationAssessmentInput Input(CertificationStepDefinitionId id, CertificationAssessmentOutcome outcome) =>
        new(id, "Jury habilité", outcome, outcome == CertificationAssessmentOutcome.Pending ? 50m : 100m, "Observations");

    [Fact]
    public void Batch_draft_persists_all_criteria_without_finalizing()
    {
        var candidate = Candidate();
        var steps = new[] { CertificationStepDefinitionId.New(), CertificationStepDefinitionId.New() };
        candidate.RecordAssessmentBatch(new[] { Input(steps[0], CertificationAssessmentOutcome.Passed), Input(steps[1], CertificationAssessmentOutcome.Pending) }, steps, false);
        candidate.Assessments.Should().HaveCount(2);
        candidate.Status.Should().Be(CertificationCandidateStatus.InProgress);
    }

    [Fact]
    public void Batch_rejects_duplicate_before_modifying_any_assessment()
    {
        var candidate = Candidate();
        var step = CertificationStepDefinitionId.New();
        var write = () => candidate.RecordAssessmentBatch(new[] { Input(step, CertificationAssessmentOutcome.Passed), Input(step, CertificationAssessmentOutcome.Failed) }, new[] { step }, false);
        write.Should().Throw<DomainException>().WithMessage("CERTIFICATION_ASSESSMENT_DUPLICATE_STEP");
        candidate.Assessments.Should().BeEmpty();
    }

    [Fact]
    public void Batch_rejects_foreign_step_before_modifying_candidate()
    {
        var candidate = Candidate();
        var allowed = CertificationStepDefinitionId.New();
        var foreign = CertificationStepDefinitionId.New();
        var write = () => candidate.RecordAssessmentBatch(new[] { Input(allowed, CertificationAssessmentOutcome.Passed), Input(foreign, CertificationAssessmentOutcome.Passed) }, new[] { allowed }, false);
        write.Should().Throw<DomainException>().WithMessage("CERTIFICATION_STEP_NOT_FOUND");
        candidate.Assessments.Should().BeEmpty();
    }

    [Fact]
    public void Pending_partial_cannot_be_finalized()
    {
        var candidate = Candidate();
        var step = CertificationStepDefinitionId.New();
        var write = () => candidate.RecordAssessmentBatch(new[] { Input(step, CertificationAssessmentOutcome.Pending) }, new[] { step }, true);
        write.Should().Throw<DomainException>().WithMessage("CERTIFICATION_ASSESSMENTS_INCOMPLETE");
        candidate.Assessments.Should().BeEmpty();
    }

    [Fact]
    public void Finalize_is_persistent_and_prevents_future_edits()
    {
        var candidate = Candidate();
        var step = CertificationStepDefinitionId.New();
        candidate.RecordAssessmentBatch(new[] { Input(step, CertificationAssessmentOutcome.Passed) }, new[] { step }, true);
        candidate.Status.Should().Be(CertificationCandidateStatus.AssessmentsValidated);
        var write = () => candidate.RecordAssessment(step, "Jury", CertificationAssessmentOutcome.Failed, 0m, "Correction");
        write.Should().Throw<DomainException>().WithMessage("CERTIFICATION_CANDIDATE_LOCKED");
        candidate.Decide(CertificationDecision.Obtained, "Admis", new[] { step });
        candidate.Status.Should().Be(CertificationCandidateStatus.Completed);
    }

    [Fact]
    public void Draft_can_complete_previously_recorded_steps_before_validation()
    {
        var candidate = Candidate();
        var steps = new[] { CertificationStepDefinitionId.New(), CertificationStepDefinitionId.New() };
        candidate.RecordAssessment(steps[0], "Jury", CertificationAssessmentOutcome.Passed, 100, null);
        candidate.RecordAssessmentBatch(new[] { Input(steps[1], CertificationAssessmentOutcome.Failed) }, steps, true);
        candidate.Status.Should().Be(CertificationCandidateStatus.AssessmentsValidated);
        candidate.Assessments.Should().HaveCount(2);
    }
}
