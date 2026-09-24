using DomainRelay.Abstractions;
using FluentValidation;
using PedagoraPilot.Contracts.Catalog;

namespace PedagoraPilot.Application.Catalog.Referentials;
public sealed record CreateReferentialVersionCommand(Guid ReferentialId, string Version, string? CertificationCode, DateOnly EffectiveFrom, int TotalHours, int SheetCount, int RequiredDocumentCount, IReadOnlyCollection<string> EnabledModules, string? NotesKey, string? ExternalKey) : IRequest<ReferentialVersionDto>;
public sealed record PublishReferentialVersionCommand(Guid VersionId) : IRequest<ReferentialVersionDto>;
public sealed class CreateReferentialVersionCommandValidator : AbstractValidator<CreateReferentialVersionCommand>
{
    public CreateReferentialVersionCommandValidator()
    {
        RuleFor(x => x.Version).NotEmpty();
        RuleFor(x => x.TotalHours).GreaterThan(0);
    }
}
