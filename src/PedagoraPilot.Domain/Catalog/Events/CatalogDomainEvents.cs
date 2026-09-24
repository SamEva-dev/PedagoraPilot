using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Catalog.Events;
public sealed record TrainingProgramCreatedDomainEvent(Guid ProgramId) : DomainEvent;
public sealed record TrainingProgramUpdatedDomainEvent(Guid ProgramId) : DomainEvent;
public sealed record ProgramOfferingChangedDomainEvent(Guid OfferingId, Guid SiteId, Guid ProgramId, bool Active) : DomainEvent;
public sealed record ReferentialVersionCreatedDomainEvent(Guid ReferentialVersionId, Guid ReferentialId) : DomainEvent;
public sealed record ReferentialVersionPublishedDomainEvent(Guid ReferentialVersionId, Guid ReferentialId) : DomainEvent;
