using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Training.Learners;

internal static class LearnerContextualAccess
{
    public static async Task EnsureCohortAsync(
        Cohort cohort,
        IProgramOfferingRepository offerings,
        ICurrentUser current,
        bool manage,
        CancellationToken ct)
    {
        if (manage)
            await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        else
            await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
    }

    public static async Task EnsureEnrollmentAsync(
        Enrollment enrollment,
        ICohortRepository cohorts,
        IProgramOfferingRepository offerings,
        ICurrentUser current,
        bool manage,
        CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        await EnsureCohortAsync(cohort, offerings, current, manage, ct);
    }
}
