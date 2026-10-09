using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class CohortRepository(PedagoraPilotDbContext db) : Repository<Cohort, CohortId>(db), ICohortRepository
{
    public async Task LockForEnrollmentCapacityAsync(CohortId cohortId, CancellationToken cancellationToken = default)
    {
        // All seat mutations (enroll, activate, cohort capacity change) lock the same cohort row.
        // PostgreSQL holds SELECT FOR UPDATE until the active transaction commits/rolls back.
        var transaction = DbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException("ENROLLMENT_CAPACITY_TRANSACTION_REQUIRED");
        var connection = DbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT 1 FROM training.cohorts WHERE \"Id\" = @cohort_id FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@cohort_id";
        parameter.Value = cohortId.Value;
        command.Parameters.Add(parameter);
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
            throw new InvalidOperationException("COHORT_NOT_FOUND");
    }

    public Task<bool> CodeExistsAsync(Guid organizationId, string code, CohortId? exceptId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return Query(false).AnyAsync(x => x.OrganizationId == organizationId && x.Code == normalized && (!exceptId.HasValue || x.Id != exceptId.Value), cancellationToken);
    }
}

public sealed class PersonRepository(PedagoraPilotDbContext db) : Repository<Person, PersonId>(db), IPersonRepository
{
    public Task<Person?> GetByEmailAsync(string email, bool isTracking = false, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Query(isTracking).SingleOrDefaultAsync(x => x.Email == normalized, cancellationToken);
    }
}

public sealed class LearnerProfileRepository(PedagoraPilotDbContext db) : Repository<LearnerProfile, LearnerProfileId>(db), ILearnerProfileRepository
{
    public Task<LearnerProfile?> GetByPersonIdAsync(PersonId personId, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).SingleOrDefaultAsync(x => x.PersonId == personId, cancellationToken);
}

public sealed class EnrollmentRepository(PedagoraPilotDbContext db) : Repository<Enrollment, EnrollmentId>(db), IEnrollmentRepository
{
    public Task<Enrollment?> GetByLearnerAndCohortAsync(LearnerProfileId learnerProfileId, CohortId cohortId, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).SingleOrDefaultAsync(x => x.LearnerProfileId == learnerProfileId && x.CohortId == cohortId, cancellationToken);
    public Task<int> CountActiveByCohortAsync(CohortId cohortId, CancellationToken cancellationToken = default) => Query(false).CountAsync(x => x.CohortId == cohortId && x.Status == EnrollmentStatus.Active, cancellationToken);
    public Task<int> CountSeatOccupyingByCohortAsync(CohortId cohortId, CancellationToken cancellationToken = default)
        => Query(false).CountAsync(x => x.CohortId == cohortId && (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Suspended), cancellationToken);
}


public sealed class CohortTrainerAssignmentRepository(PedagoraPilotDbContext db) : Repository<CohortTrainerAssignment, CohortTrainerAssignmentId>(db), ICohortTrainerAssignmentRepository
{
    public Task<CohortTrainerAssignment?> GetByUserAndCohortAsync(Guid organizationId, Guid authGateUserId, CohortId cohortId, bool isTracking = false, CancellationToken cancellationToken = default)
        => Query(isTracking).SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.AuthGateUserId == authGateUserId && x.CohortId == cohortId, cancellationToken);
}
