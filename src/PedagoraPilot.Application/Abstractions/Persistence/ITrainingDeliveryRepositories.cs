using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface ITrainingSessionRepository : IRepository<TrainingSession, TrainingSessionId>
{
    new Task<TrainingSession?> GetByIdAsync(TrainingSessionId id, bool isTracking = false, CancellationToken cancellationToken = default);
}

public interface IAttendanceSheetRepository : IRepository<AttendanceSheet, AttendanceSheetId>
{
    Task<AttendanceSheet?> GetBySessionIdAsync(TrainingSessionId sessionId, bool isTracking = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceSheet>> GetBySessionIdsAsync(IReadOnlyCollection<TrainingSessionId> sessionIds, bool isTracking = false, CancellationToken cancellationToken = default);
}
