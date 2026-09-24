using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Learning.Events;
public sealed record PersonCreatedDomainEvent(PersonId PersonId) : DomainEvent;
public sealed record LearnerProfileCreatedDomainEvent(LearnerProfileId LearnerProfileId, PersonId PersonId) : DomainEvent;
