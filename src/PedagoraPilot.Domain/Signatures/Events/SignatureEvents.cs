using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Signatures.Events;

public sealed record SignatureDraftCreatedDomainEvent(SignatureEnvelopeId EnvelopeId, Guid OrganizationId) : DomainEvent;
public sealed record SignatureRequestedDomainEvent(SignatureEnvelopeId EnvelopeId, Guid OrganizationId) : DomainEvent;
public sealed record SignatureCompletedDomainEvent(SignatureEnvelopeId EnvelopeId, Guid OrganizationId) : DomainEvent;
public sealed record SignatureDeclinedDomainEvent(SignatureEnvelopeId EnvelopeId, Guid OrganizationId) : DomainEvent;

public sealed record SignatureReminderRequestedDomainEvent(SignatureEnvelopeId EnvelopeId, Guid OrganizationId) : DomainEvent;
