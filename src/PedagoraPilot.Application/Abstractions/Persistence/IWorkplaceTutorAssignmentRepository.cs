namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IWorkplaceTutorAssignmentRepository
{
    Task<IReadOnlyList<PedagoraPilot.Domain.Workplace.WorkplaceTutorAssignment>> GetByUserAsync(Guid organizationId, Guid authGateUserId, CancellationToken ct = default);
    Task AddAsync(PedagoraPilot.Domain.Workplace.WorkplaceTutorAssignment assignment, CancellationToken ct = default);
    Task<IReadOnlyList<PedagoraPilot.Domain.Workplace.WorkplaceTutorAssignment>> GetByInvitationAsync(Guid organizationId, Guid invitationId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetActivePeriodIdsAsync(Guid organizationId, Guid authGateUserId, CancellationToken ct = default);
    Task<IReadOnlyList<PedagoraPilot.Domain.Workplace.WorkplaceTutorAssignment>> GetByPeriodAsync(Guid organizationId, Guid periodId, CancellationToken ct = default);
    Task<bool> HasAnyAssignmentAsync(Guid organizationId, Guid periodId, CancellationToken ct = default);
    Task<bool> HasActiveAssignmentAsync(Guid organizationId, Guid periodId, Guid authGateUserId, CancellationToken ct = default);
}
