using FluentAssertions;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class CertificationSprintS07Tests
{
    private static CertificationCandidate EligibleCandidate()
    {
        var candidate = CertificationCandidate.Register(Guid.NewGuid(), CertificationExamSessionId.New(), EnrollmentId.New());
        candidate.SetEligibility(true, "{}");
        return candidate;
    }

    [Fact]
    public void Cannot_decide_before_required_assessments_are_recorded()
    {
        var candidate = EligibleCandidate();
        var steps = new[] { CertificationStepDefinitionId.New(), CertificationStepDefinitionId.New() };
        candidate.RecordAssessment(steps[0], "Jury", CertificationAssessmentOutcome.Passed, 70, null);

        var action = () => candidate.Decide(CertificationDecision.Obtained, "Validé", steps);
        action.Should().Throw<DomainException>().WithMessage("CERTIFICATION_ASSESSMENTS_INCOMPLETE");
        candidate.Decision.Should().Be(CertificationDecision.Pending);
    }

    [Fact]
    public void Pending_assessment_is_not_a_final_assessment()
    {
        var candidate = EligibleCandidate();
        var step = CertificationStepDefinitionId.New();
        candidate.RecordAssessment(step, "Jury", CertificationAssessmentOutcome.Pending, 50m, null);
        var action = () => candidate.Decide(CertificationDecision.Partial, null, new[] { step });
        action.Should().Throw<DomainException>().WithMessage("CERTIFICATION_ASSESSMENTS_INCOMPLETE");
    }

    [Fact]
    public void Completed_decision_freezes_assessments_and_decision()
    {
        var candidate = EligibleCandidate();
        var step = CertificationStepDefinitionId.New();
        candidate.RecordAssessment(step, "Jury", CertificationAssessmentOutcome.Passed, 100m, "Initial");
        candidate.Decide(CertificationDecision.Obtained, "Approuvé", new[] { step });

        var reassess = () => candidate.RecordAssessment(step, "Autre", CertificationAssessmentOutcome.Failed, 0m, "Altération");
        var redecide = () => candidate.Decide(CertificationDecision.Failed, "Changement", new[] { step });
        reassess.Should().Throw<DomainException>().WithMessage("CERTIFICATION_CANDIDATE_LOCKED");
        redecide.Should().Throw<DomainException>().WithMessage("CERTIFICATION_CANDIDATE_LOCKED");
        candidate.Decision.Should().Be(CertificationDecision.Obtained);
    }

    [Fact]
    public void Absent_decision_is_allowed_without_sitting_the_assessments()
    {
        var candidate = EligibleCandidate();
        candidate.Decide(CertificationDecision.Absent, "Absent", new[] { CertificationStepDefinitionId.New() });
        candidate.Decision.Should().Be(CertificationDecision.Absent);
        candidate.Status.Should().Be(CertificationCandidateStatus.Completed);
    }

    [Fact]
    public void Eligibility_is_frozen_after_first_assessment()
    {
        var candidate = EligibleCandidate();
        var step = CertificationStepDefinitionId.New();
        candidate.RecordAssessment(step, "Jury", CertificationAssessmentOutcome.Passed, 60m, null);
        var action = () => candidate.SetEligibility(false, "{}");
        action.Should().Throw<DomainException>().WithMessage("CERTIFICATION_CANDIDATE_LOCKED");
        candidate.Eligible.Should().BeTrue();
    }

    [Fact]
    public void Session_must_be_planned_to_start()
    {
        var session = CertificationExamSession.Create(Guid.NewGuid(), Guid.NewGuid(), CohortId.New(), CertificationSchemeId.New(),
            "Examen", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2), null);
        var invalidStart = () => session.Start();
        invalidStart.Should().Throw<DomainException>().WithMessage("CERTIFICATION_SESSION_CANNOT_START");
        session.Plan();
        session.Start();
        session.Status.Should().Be(CertificationExamSessionStatus.InProgress);
    }
}
