using PedagoraPilot.Application.Abstractions.Messaging;

namespace PedagoraPilot.Application.Organizations.Sites;

public sealed record DeleteTrainingSiteCommand(Guid OrganizationId, Guid SiteId) : ICommand<bool>;
