using FluentAssertions;
using PedagoraPilot.Application.Access.Invitations;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class AccessInvitationValidatorTests
{
    private static CreateAccessInvitationCommand Valid() =>
        new("Ada", "Martin", "ada@example.com", "student", CohortId: Guid.NewGuid(),
            Phone: "+33601020304", BirthDate: new DateOnly(2002, 1, 2), EnrolledOn: new DateOnly(2026, 10, 9));

    [Fact]
    public async Task Valid_student_invitation_is_accepted()
    {
        var result = await new CreateAccessInvitationCommandValidator().ValidateAsync(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Martin", "ada@example.com")]
    [InlineData("Ada", "", "ada@example.com")]
    [InlineData("Ada", "Martin", "invalid-email")]
    public async Task Missing_or_invalid_identity_is_rejected(string firstName, string lastName, string email)
    {
        var command = Valid() with { FirstName = firstName, LastName = lastName, Email = email };
        var result = await new CreateAccessInvitationCommandValidator().ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Future_birth_date_is_rejected()
    {
        var command = Valid() with { BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)) };
        var result = await new CreateAccessInvitationCommandValidator().ValidateAsync(command);
        result.Errors.Should().Contain(x => x.ErrorMessage == "ACCESS_INVITATION_BIRTH_DATE_INVALID");
    }

    [Fact]
    public async Task Oversized_phone_is_rejected()
    {
        var command = Valid() with { Phone = new string('1', 41) };
        var result = await new CreateAccessInvitationCommandValidator().ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }
}
