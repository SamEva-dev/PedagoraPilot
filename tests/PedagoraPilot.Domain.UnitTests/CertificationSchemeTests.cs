using FluentAssertions;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Common;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class CertificationSchemeTests
{
    [Fact]
    public void Published_scheme_requires_units_and_steps()
    {
        var scheme = CertificationScheme.Create(Guid.NewGuid(), "RNCP41862", "TP ECSR");
        var act = () => scheme.Publish(new DateOnly(2026, 4, 29), null);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("CERTIFICATION_SCHEME_INCOMPLETE");
    }

    [Fact]
    public void Published_scheme_is_immutable_for_structure()
    {
        var scheme = CertificationScheme.Create(Guid.NewGuid(), "RNCP41862", "TP ECSR");
        var unit = scheme.AddUnit("BC01", "Former des apprenants", 1);
        scheme.AddStep(unit.Id, "MSP", "Mise en situation professionnelle", CertificationStepKind.Practical, 120, 1);
        scheme.Publish(new DateOnly(2026, 4, 29), null);
        var act = () => scheme.AddUnit("BC02", "Sensibiliser", 2);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("CERTIFICATION_SCHEME_LOCKED");
    }
}
