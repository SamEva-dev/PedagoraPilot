using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface ICohortRepository : IRepository<Cohort, CohortId>
{
    Task<bool> CodeExistsAsync(Guid organizationId, string code, CohortId? exceptId = null, CancellationToken cancellationToken = default);
}

public interface IPersonRepository : IRepository<Person, PersonId>
{
    Task<Person?> GetByEmailAsync(string email, bool isTracking = false, CancellationToken cancellationToken = default);
}

public interface ILearnerProfileRepository : IRepository<LearnerProfile, LearnerProfileId>
{
    Task<LearnerProfile?> GetByPersonIdAsync(PersonId personId, bool isTracking = false, CancellationToken cancellationToken = default);
}

public interface IEnrollmentRepository : IRepository<Enrollment, EnrollmentId>
{
    Task<Enrollment?> GetByLearnerAndCohortAsync(LearnerProfileId learnerProfileId, CohortId cohortId, bool isTracking = false, CancellationToken cancellationToken = default);
    Task<int> CountActiveByCohortAsync(CohortId cohortId, CancellationToken cancellationToken = default);
}
