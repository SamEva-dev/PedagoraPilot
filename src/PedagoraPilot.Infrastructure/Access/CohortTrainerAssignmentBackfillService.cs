using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Infrastructure.Persistence;

namespace PedagoraPilot.Infrastructure.Access;

/// <summary>
/// Idempotent reconciliation for promotions that already had a pedagogical manager before
/// CohortTrainerAssignment became a first-class business relation.
/// </summary>
public sealed class CohortTrainerAssignmentBackfillService(
    IServiceScopeFactory scopeFactory,
    ILogger<CohortTrainerAssignmentBackfillService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PedagoraPilotDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var cohorts = await db.Cohorts.AsNoTracking()
                .Where(x => x.PedagogicalManagerAuthGateUserId.HasValue)
                .Select(x => new { x.Id, x.OrganizationId, ManagerId = x.PedagogicalManagerAuthGateUserId!.Value })
                .ToListAsync(stoppingToken);

            if (cohorts.Count == 0) return;

            var managerIds = cohorts.Select(x => x.ManagerId).Distinct().ToArray();
            var members = await db.OrganizationMembers.AsNoTracking()
                .Where(x => x.IsActive && managerIds.Contains(x.AuthGateUserId))
                .Select(x => new { x.Id, x.OrganizationId, x.AuthGateUserId })
                .ToListAsync(stoppingToken);
            var memberByKey = members.ToDictionary(x => (x.OrganizationId, x.AuthGateUserId));

            var existing = await db.CohortTrainerAssignments.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new { x.OrganizationId, x.CohortId, x.AuthGateUserId })
                .ToListAsync(stoppingToken);
            var existingKeys = existing.Select(x => (x.OrganizationId, x.CohortId, x.AuthGateUserId)).ToHashSet();

            var added = 0;
            foreach (var cohort in cohorts)
            {
                var key = (cohort.OrganizationId, cohort.Id, cohort.ManagerId);
                if (existingKeys.Contains(key)) continue;
                if (!memberByKey.TryGetValue((cohort.OrganizationId, cohort.ManagerId), out var member)) continue;

                await db.CohortTrainerAssignments.AddAsync(
                    CohortTrainerAssignment.Create(cohort.OrganizationId, cohort.Id, member.Id, cohort.ManagerId),
                    stoppingToken);
                existingKeys.Add(key);
                added++;
            }

            if (added > 0)
                await unitOfWork.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to backfill trainer assignments for existing cohort pedagogical managers.");
        }
    }
}
