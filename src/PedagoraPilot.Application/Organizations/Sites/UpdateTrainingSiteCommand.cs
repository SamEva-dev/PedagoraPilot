using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Workspace;

namespace PedagoraPilot.Application.Organizations.Sites;
public sealed record UpdateTrainingSiteCommand(Guid OrganizationId, Guid SiteId, string Code, string Name,
    string City, string Address, string PostalCode, string Phone, string Email, string Manager,
    string Status) : ICommand<WorkspaceSiteDto>;
