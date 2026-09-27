using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Access.Events;

public sealed record AccessInvitationCreatedDomainEvent(AccessInvitationId InvitationId, Guid OrganizationId, string Token) : DomainEvent;
public sealed record AccessInvitationAcceptedDomainEvent(AccessInvitationId InvitationId, Guid OrganizationId, Guid AuthGateUserId) : DomainEvent;

public sealed record AccessMemberRoleChangedDomainEvent(Guid OrganizationId, Guid AuthGateUserId, string RoleCode) : DomainEvent;
