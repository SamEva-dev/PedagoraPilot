using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workforce;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class RemoteWorkRequestRepository(PedagoraPilotDbContext db) : Repository<RemoteWorkRequest, RemoteWorkRequestId>(db), IRemoteWorkRequestRepository
{
    public Task<bool> HasOverlappingRequestAsync(Guid authGateUserId, DateOnly date, RemoteWorkRequestId? exceptId = null, CancellationToken cancellationToken = default) => Query(false).AnyAsync(x => x.AuthGateUserId == authGateUserId && x.Date == date && x.Status != RemoteWorkRequestStatus.Rejected && x.Status != RemoteWorkRequestStatus.Cancelled && (!exceptId.HasValue || x.Id != exceptId.Value), cancellationToken);
}
