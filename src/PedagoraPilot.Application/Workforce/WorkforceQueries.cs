using DomainRelay.Abstractions;
using PedagoraPilot.Contracts.Workforce;

namespace PedagoraPilot.Application.Workforce;
public sealed record GetRemoteWorkRequestsQuery(Guid? SiteId, bool MineOnly) : IRequest<IReadOnlyCollection<RemoteWorkRequestDto>>;
