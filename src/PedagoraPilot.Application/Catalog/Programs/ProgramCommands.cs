using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using FluentValidation;
using PedagoraPilot.Contracts.Catalog;

namespace PedagoraPilot.Application.Catalog.Programs;
public sealed record CreateProgramCommand(string FamilyCode, string Code, string Name, string DescriptionKey, string Icon, int DurationHours, string Status, IReadOnlyCollection<string> EnabledModules, string? ExternalKey) : ICommand<TrainingProgramDto>;
public sealed record UpdateProgramCommand(Guid Id, string FamilyCode, string Name, string DescriptionKey, string Icon, int DurationHours, string Status, IReadOnlyCollection<string> EnabledModules) : ICommand<TrainingProgramDto>;
public sealed record SetProgramOfferingCommand(Guid ProgramId, Guid SiteId, bool Active) : ICommand<ProgramOfferingDto>;
public sealed class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.FamilyCode).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(220);
        RuleFor(x => x.DurationHours).GreaterThan(0);
        RuleFor(x => x.EnabledModules).NotEmpty();
    }
}

public sealed class UpdateProgramCommandValidator : AbstractValidator<UpdateProgramCommand>
{
    public UpdateProgramCommandValidator()
    {
        RuleFor(x => x.FamilyCode).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.DurationHours).GreaterThan(0);
        RuleFor(x => x.EnabledModules).NotEmpty();
    }
}
