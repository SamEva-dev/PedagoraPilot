using FluentAssertions;
using PedagoraPilot.Application.Training.Cohorts;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class CohortSprintS03ValidatorTests
{
    private static CreateCohortCommand Command(string? status, Guid? managerId = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), "ECSR-2026", "Formation",
        new DateOnly(2026, 10, 1), new DateOnly(2027, 2, 28), 12,
        managerId ?? Guid.NewGuid(), "Formateur actif", null, status);

    [Theory]
    [InlineData(null)]
    [InlineData("planned")]
    [InlineData("active")]
    [InlineData("draft")]
    [InlineData("ACTIVE")]
    public async Task Create_validator_accepts_open_initial_status(string? status)
    {
        var result = await new CreateCohortCommandValidator().ValidateAsync(Command(status));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("cancelled")]
    [InlineData("not-a-status")]
    public async Task Create_validator_rejects_terminal_and_unknown_status(string status)
    {
        var result = await new CreateCohortCommandValidator().ValidateAsync(Command(status));
        result.Errors.Should().Contain(x => x.ErrorMessage == "COHORT_STATUS_INVALID");
    }

    [Fact]
    public async Task Create_validator_rejects_missing_pedagogical_manager()
    {
        var command = Command("planned") with { PedagogicalManagerAuthGateUserId = null };
        var result = await new CreateCohortCommandValidator().ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Reassignment_validator_rejects_empty_user()
    {
        var command = new ReassignCohortManagerCommand(new CohortId(Guid.NewGuid()), Guid.Empty);
        var result = await new ReassignCohortManagerCommandValidator().ValidateAsync(command);
        result.Errors.Should().Contain(x => x.ErrorMessage == "COHORT_PEDAGOGICAL_MANAGER_REQUIRED");
    }
}
