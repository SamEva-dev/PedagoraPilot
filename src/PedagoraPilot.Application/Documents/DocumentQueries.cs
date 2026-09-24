using DomainRelay.Abstractions;
using PedagoraPilot.Contracts.Documents;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Documents;
public sealed record GetDocumentsQuery(Guid? CohortId, string? Category, string? OwnerType, Guid? OwnerId) : IRequest<IReadOnlyCollection<DocumentDto>>;
public sealed record GetDocumentQuery(DocumentId DocumentId) : IRequest<DocumentDto>;
public sealed record DownloadDocumentQuery(DocumentId DocumentId, DocumentVersionId? VersionId = null) : IRequest<DocumentDownloadDto>;
