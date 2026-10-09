using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Access.Members;
using PedagoraPilot.Infrastructure.Persistence;

namespace PedagoraPilot.Infrastructure.Access;

/// <summary>
/// One-time/idempotent startup reconciliation for Student members created before learner
/// projection provisioning was introduced. It never creates enrollments automatically: existing
/// members without a cohort appear as unassigned learners and can be assigned later.
/// </summary>
public sealed class StudentMemberProjectionBackfillService(
    IServiceScopeFactory scopeFactory,
    ILogger<StudentMemberProjectionBackfillService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PedagoraPilotDbContext>();
            var provisioner = scope.ServiceProvider.GetRequiredService<IStudentMemberProvisioner>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var studentMemberIds = db.StudentProfiles.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.MemberId);
            var students = await db.OrganizationMembers.AsNoTracking()
                .Where(x => x.IsActive && studentMemberIds.Contains(x.Id))
                .Select(x => new { x.OrganizationId, x.AuthGateUserId, x.Email, x.FirstName, x.LastName })
                .ToListAsync(stoppingToken);

            foreach (var student in students)
            {
                await provisioner.EnsureAsync(student.OrganizationId, student.AuthGateUserId, student.Email,
                    student.FirstName, student.LastName, null, ct: stoppingToken);
            }

            if (students.Count > 0)
                await unitOfWork.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to reconcile existing Student members with learner profiles at startup.");
        }
    }
}
