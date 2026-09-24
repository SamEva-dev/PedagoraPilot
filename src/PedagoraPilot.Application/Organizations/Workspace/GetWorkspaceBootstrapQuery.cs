using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Workspace;

namespace PedagoraPilot.Application.Organizations.Workspace;
public sealed record GetWorkspaceBootstrapQuery : IQuery<WorkspaceBootstrapDto>;
