using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Application.Workplace;

public sealed class WorkplaceContextualAccess(
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    ICurrentUser current)
{
    public async Task EnsureAsync(WorkplacePeriod period, bool manage, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(period.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (manage)
            await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        else
            await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
    }

    public async Task<bool> CanViewAsync(WorkplacePeriod period, CancellationToken ct)
    {
        try
        {
            await EnsureAsync(period, manage: false, ct);
            return true;
        }
        catch (ForbiddenApplicationException)
        {
            return false;
        }
    }
}
