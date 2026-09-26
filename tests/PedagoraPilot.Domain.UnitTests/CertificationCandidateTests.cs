using FluentAssertions;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class CertificationCandidateTests
{
    [Fact]
    public void Recording_same_step_twice_updates_existing_draft_assessment()
    {
        var candidate = CertificationCandidate.Register(
            Guid.NewGuid(),
            CertificationExamSessionId.New(),
            EnrollmentId.New());
        candidate.SetEligibility(true, "{}");
        var stepId = CertificationStepDefinitionId.New();

        candidate.RecordAssessment(
            stepId,
            "Jury One",
            CertificationAssessmentOutcome.Passed,
            100m,
            "First draft");

        candidate.RecordAssessment(
            stepId,
            "Jury Two",
            CertificationAssessmentOutcome.Failed,
            0m,
            "Updated draft");

        candidate.Assessments.Should().ContainSingle();
        var assessment = candidate.Assessments.Single();
        assessment.StepDefinitionId.Should().Be(stepId);
        assessment.JuryDisplayName.Should().Be("Jury Two");
        assessment.Outcome.Should().Be(CertificationAssessmentOutcome.Failed);
        assessment.Score.Should().Be(0m);
        assessment.Comment.Should().Be("Updated draft");
    }
}
