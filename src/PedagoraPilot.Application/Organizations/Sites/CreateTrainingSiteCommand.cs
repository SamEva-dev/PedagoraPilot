using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Workspace;

namespace PedagoraPilot.Application.Organizations.Sites;
public sealed record CreateTrainingSiteCommand(Guid OrganizationId, string Code, string Name, string City,
    string Address, string PostalCode, string Phone, string Email, string Manager, string Status,
    string? ExternalKey) : ICommand<WorkspaceSiteDto>;
