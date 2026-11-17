using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Application.Training.Delivery;

/// <summary>
/// Prevents an identified instructor from being assigned to overlapping sessions,
/// even when the sessions belong to different sites or cohorts of one organization.
/// Adjacent intervals [start, end) do not overlap. Cancelled sessions release the slot.
/// </summary>
public static class TrainingSessionScheduleGuard
{
    public static bool Overlaps(DateTimeOffset existingStart, DateTimeOffset existingEnd,
        DateTimeOffset proposedStart, DateTimeOffset proposedEnd)
        => existingStart < proposedEnd && proposedStart < existingEnd;

    public static async Task EnsureTrainerAvailableAsync(
        ITrainingSessionRepository sessions, Guid organizationId, string? trainerId,
        DateTimeOffset start, DateTimeOffset end, TrainingSessionId? excludedSessionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trainerId) || end <= start)
            return; // Other validators reject invalid date ranges; no identified trainer to reserve.

        var startUtc = start.ToUniversalTime();
        var endUtc = end.ToUniversalTime();
        var query = sessions.Query(false).Where(session =>
            session.OrganizationId == organizationId &&
            session.TrainerAuthGateUserId == trainerId &&
            session.Status != TrainingSessionStatus.Cancelled &&
            session.StartsAtUtc < endUtc && startUtc < session.EndsAtUtc);
        if (excludedSessionId.HasValue)
            query = query.Where(session => session.Id != excludedSessionId.Value);
        if (await query.AnyAsync(cancellationToken))
            throw new ConflictApplicationException(ErrorKeys.SessionTrainerScheduleConflict);
    }
}
