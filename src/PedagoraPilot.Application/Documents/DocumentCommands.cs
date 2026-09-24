using DomainRelay.Abstractions;
using FluentValidation;
using PedagoraPilot.Contracts.Documents;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Documents;
public sealed record UploadDocumentCommand(string Title, string? Description, string Category, string Visibility, string OwnerType, Guid? OwnerId, Guid? SiteId, Guid? ProgramId, Guid? CohortId, string AuthorDisplayName, string FileName, string ContentType, long SizeBytes, Stream Content) : IRequest<DocumentDto>;
public sealed record ReplaceDocumentVersionCommand(DocumentId DocumentId, string AuthorDisplayName, string FileName, string ContentType, long SizeBytes, Stream Content) : IRequest<DocumentDto>;
public sealed record UpdateDocumentMetadataCommand(DocumentId DocumentId, string Title, string? Description, string Category, string Visibility) : IRequest<DocumentDto>;
public sealed record DeleteDocumentCommand(DocumentId DocumentId) : IRequest<bool>;
public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AuthorDisplayName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.Content).NotNull();
    }
}

public sealed class ReplaceDocumentVersionCommandValidator : AbstractValidator<ReplaceDocumentVersionCommand>
{
    public ReplaceDocumentVersionCommandValidator()
    {
        RuleFor(x => x.AuthorDisplayName).NotEmpty().MaximumLength(250);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.Content).NotNull();
    }
}

public sealed class UpdateDocumentMetadataCommandValidator : AbstractValidator<UpdateDocumentMetadataCommand>
{
    public UpdateDocumentMetadataCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}
