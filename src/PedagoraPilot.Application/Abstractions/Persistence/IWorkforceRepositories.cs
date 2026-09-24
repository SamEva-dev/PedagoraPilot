using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workforce;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IRemoteWorkRequestRepository : IRepository<RemoteWorkRequest, RemoteWorkRequestId>
{
    Task<bool> HasOverlappingRequestAsync(Guid authGateUserId, DateOnly date, RemoteWorkRequestId? exceptId = null, CancellationToken cancellationToken = default);
}
