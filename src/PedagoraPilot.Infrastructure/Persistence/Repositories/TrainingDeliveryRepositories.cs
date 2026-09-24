using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class TrainingSessionRepository(PedagoraPilotDbContext db) : Repository<TrainingSession, TrainingSessionId>(db), ITrainingSessionRepository
{
    public override Task<TrainingSession?> GetByIdAsync(TrainingSessionId id, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).Include(x => x.Participants).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}

public sealed class AttendanceSheetRepository(PedagoraPilotDbContext db) : Repository<AttendanceSheet, AttendanceSheetId>(db), IAttendanceSheetRepository
{
    public Task<AttendanceSheet?> GetBySessionIdAsync(TrainingSessionId sessionId, bool isTracking = false, CancellationToken cancellationToken = default)
    {
        var query = Query(isTracking).Include(x => x.Entries);
        return query.SingleOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttendanceSheet>> GetBySessionIdsAsync(IReadOnlyCollection<TrainingSessionId> sessionIds, bool isTracking = false, CancellationToken cancellationToken = default)
    {
        if (sessionIds.Count == 0)
            return Array.Empty<AttendanceSheet>();
        return await Query(isTracking).Include(x => x.Entries).Where(x => sessionIds.Contains(x.SessionId)).ToListAsync(cancellationToken);
    }
}
