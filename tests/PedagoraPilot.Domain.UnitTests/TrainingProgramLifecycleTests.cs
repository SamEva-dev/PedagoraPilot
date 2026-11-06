using FluentAssertions;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Common;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class TrainingProgramLifecycleTests
{
    private static TrainingProgram NewProgram() => TrainingProgram.Create(
        Guid.NewGuid(), "ECSR", "Training", "Description", "ph-books", 910, ["planning"]);

    [Fact]
    public void Creation_starts_in_draft_and_edition_preserves_status()
    {
        var p = NewProgram();
        p.Status.Should().Be(ProgramStatus.Draft);
        p.Update(p.FamilyId, "Changed", "Description", "ph-books", 910,
            ProgramStatus.Active, ["planning"]);
        p.Status.Should().Be(ProgramStatus.Draft);
    }

    [Fact]
    public void Activation_without_manager_is_rejected()
    {
        var p = NewProgram();
        var action = () => p.ChangeStatus(ProgramStatus.Active);
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Active_to_draft_is_forbidden_even_for_administrators()
    {
        var p = NewProgram();
        p.AssignResponsibleTrainer(Guid.NewGuid(), "Responsible");
        p.ChangeStatus(ProgramStatus.Active);
        var action = () => p.ChangeStatus(ProgramStatus.Draft);
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Active_can_be_disabled_then_reactivated()
    {
        var p = NewProgram();
        p.AssignResponsibleTrainer(Guid.NewGuid(), "Responsible");
        p.ChangeStatus(ProgramStatus.Active);
        p.ChangeStatus(ProgramStatus.Inactive);
        p.Status.Should().Be(ProgramStatus.Inactive);
        p.ChangeStatus(ProgramStatus.Active);
        p.Status.Should().Be(ProgramStatus.Active);
    }
}
