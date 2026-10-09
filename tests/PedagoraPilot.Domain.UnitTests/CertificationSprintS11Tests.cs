using FluentAssertions;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class CertificationSprintS11Tests
{
    private static CertificationExamSession NewSession() =>
        CertificationExamSession.Create(
            Guid.NewGuid(), Guid.NewGuid(), CohortId.New(), CertificationSchemeId.New(),
            "Session d'examen", DateTimeOffset.UtcNow.AddDays(2),
            DateTimeOffset.UtcNow.AddDays(3), "Centre");

    [Fact]
    public void Only_a_planned_exam_session_can_be_started()
    {
        var session = NewSession();
        var action = () => session.Start();
        action.Should().Throw<DomainException>().WithMessage("CERTIFICATION_SESSION_CANNOT_START");
        session.Status.Should().Be(CertificationExamSessionStatus.Draft);
    }

    [Fact]
    public void Starting_an_exam_is_one_way_and_cannot_be_repeated()
    {
        var session = NewSession();
        session.Plan();
        session.Start();
        session.Status.Should().Be(CertificationExamSessionStatus.InProgress);

        var secondStart = () => session.Start();
        secondStart.Should().Throw<DomainException>().WithMessage("CERTIFICATION_SESSION_CANNOT_START");
        session.Status.Should().Be(CertificationExamSessionStatus.InProgress);
    }

    [Fact]
    public void An_exam_cannot_be_published_just_after_starting()
    {
        var session = NewSession();
        session.Plan();
        session.Start();

        var publish = () => session.Publish();
        publish.Should().Throw<DomainException>().WithMessage("CERTIFICATION_SESSION_CANNOT_PUBLISH");
    }
}
