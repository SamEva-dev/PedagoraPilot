using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Workplace;
using PedagoraPilot.Domain.Identifiers;
namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class WorkplaceTutorAssignmentRepository(PedagoraPilotDbContext db) : IWorkplaceTutorAssignmentRepository
{
    public async Task<IReadOnlyList<WorkplaceTutorAssignment>> GetByUserAsync(Guid organizationId, Guid authGateUserId, CancellationToken ct = default) =>
        await db.WorkplaceTutorAssignments.Where(x => x.OrganizationId == organizationId && x.AuthGateUserId == authGateUserId).ToListAsync(ct);
    public async Task AddAsync(WorkplaceTutorAssignment assignment, CancellationToken ct = default)
    {
        await db.WorkplaceTutorAssignments.AddAsync(assignment, ct);
    }
    public async Task<IReadOnlyList<WorkplaceTutorAssignment>> GetByInvitationAsync(Guid organizationId, Guid invitationId, CancellationToken ct = default) =>
        await db.WorkplaceTutorAssignments.Where(x => x.OrganizationId == organizationId && x.InvitationId == invitationId).ToListAsync(ct);
    public async Task<IReadOnlyList<Guid>> GetActivePeriodIdsAsync(Guid organizationId, Guid authGateUserId, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty || authGateUserId == Guid.Empty)
            return [];
        // Materialize value objects before converting them to Guid: EF translates
        // the value converter, not necessarily an inline .Value projection.
        var periodIds = await db.WorkplaceTutorAssignments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.AuthGateUserId == authGateUserId
                && x.Status == WorkplaceTutorAssignmentStatus.Active)
            .Select(x => x.WorkplacePeriodId).Distinct().ToListAsync(ct);
        return periodIds.Select(x => x.Value).ToArray();
    }
    public async Task<IReadOnlyList<WorkplaceTutorAssignment>> GetByPeriodAsync(Guid organizationId, Guid periodId, CancellationToken ct = default) =>
        await db.WorkplaceTutorAssignments.Where(x => x.OrganizationId == organizationId && x.WorkplacePeriodId == new WorkplacePeriodId(periodId)).ToListAsync(ct);
    public Task<bool> HasAnyAssignmentAsync(Guid organizationId, Guid periodId, CancellationToken ct = default) =>
        organizationId == Guid.Empty || periodId == Guid.Empty
            ? Task.FromResult(false)
            : db.WorkplaceTutorAssignments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId
                && x.WorkplacePeriodId == new WorkplacePeriodId(periodId), ct);
    public Task<bool> HasActiveAssignmentAsync(Guid organizationId, Guid periodId, Guid userId, CancellationToken ct = default) =>
        organizationId == Guid.Empty || periodId == Guid.Empty || userId == Guid.Empty
            ? Task.FromResult(false)
            : db.WorkplaceTutorAssignments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId
                && x.WorkplacePeriodId == new WorkplacePeriodId(periodId) && x.AuthGateUserId == userId
                && x.Status == WorkplaceTutorAssignmentStatus.Active, ct);
}
