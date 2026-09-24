using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Documents.Events;
public sealed record DocumentCreatedDomainEvent(DocumentId DocumentId, Guid OrganizationId) : DomainEvent;
public sealed record DocumentVersionAddedDomainEvent(DocumentId DocumentId, DocumentVersionId VersionId, Guid OrganizationId) : DomainEvent;
public sealed record DocumentMetadataUpdatedDomainEvent(DocumentId DocumentId, Guid OrganizationId) : DomainEvent;
public sealed record DocumentDeletedDomainEvent(DocumentId DocumentId, Guid OrganizationId) : DomainEvent;
