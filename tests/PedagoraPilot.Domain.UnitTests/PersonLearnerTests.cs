using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class PersonLearnerTests
{
    [Fact]
    public void Person_normalizes_names_and_email()
    {
        var p = Person.Create(" Sam ", " Fokam ", " SAM@EXAMPLE.COM ", null, null);
        p.FirstName.Should().Be("Sam");
        p.LastName.Should().Be("Fokam");
        p.Email.Should().Be("sam@example.com");
        p.DisplayName.Should().Be("Sam Fokam");
    }

    [Fact]
    public void Person_rejects_email_without_at_sign()
    {
        var act = () => Person.Create("Sam", "Fokam", "invalid", null, null);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("PERSON_EMAIL_INVALID");
    }

    [Fact]
    public void LearnerProfile_requires_person_identifier()
    {
        var act = () => LearnerProfile.Create(PersonId.Empty);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("LEARNER_PERSON_REQUIRED");
    }
}
