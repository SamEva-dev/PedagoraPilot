using FluentAssertions;
using PedagoraPilot.Application.Training.Cohorts;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;
public sealed class CohortValidatorTests
{
    [Fact]
    public async Task Create_validator_accepts_valid_command()
    {
        var command = new CreateCohortCommand(Guid.NewGuid(), Guid.NewGuid(), "ECSR-2026", "TP ECSR", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), 20, null);
        var result = await new CreateCohortCommandValidator().ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Create_validator_rejects_invalid_range_and_capacity()
    {
        var command = new CreateCohortCommand(Guid.NewGuid(), Guid.NewGuid(), "ECSR", "TP ECSR", new DateOnly(2027, 1, 2), new DateOnly(2027, 1, 1), 0, null);
        var result = await new CreateCohortCommandValidator().ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage == "COHORT_DATE_RANGE_INVALID");
        result.Errors.Should().Contain(x => x.PropertyName == "Capacity");
    }
}
