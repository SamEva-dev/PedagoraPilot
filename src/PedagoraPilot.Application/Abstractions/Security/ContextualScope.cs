using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Abstractions.Security;

public static class ContextualScope
{
    public static void EnsureCanViewSite(ICurrentUser user, Guid siteId)
    {
        if (!user.CanViewSite(siteId))
            Deny();
    }

    public static void EnsureCanManageSite(ICurrentUser user, Guid siteId)
    {
        if (!user.CanManageSite(siteId))
            Deny();
    }

    public static void EnsureCanViewProgram(ICurrentUser user, Guid siteId, Guid programId)
    {
        if (!user.CanViewProgram(siteId, programId))
            Deny();
    }

    public static void EnsureCanManageProgram(ICurrentUser user, Guid siteId, Guid programId)
    {
        if (!user.CanManageProgram(siteId, programId))
            Deny();
    }

    public static void EnsureCanViewCohort(ICurrentUser user, Guid siteId, Guid programId, Guid cohortId)
    {
        if (!user.CanViewCohort(siteId, programId, cohortId))
            Deny();
    }

    public static void EnsureCanManageCohort(ICurrentUser user, Guid siteId, Guid programId, Guid cohortId)
    {
        if (!user.CanManageCohort(siteId, programId, cohortId))
            Deny();
    }

    public static async Task<Guid> EnsureCanViewCohortAsync(
        ICurrentUser user,
        Cohort cohort,
        IProgramOfferingRepository offerings,
        CancellationToken ct)
    {
        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        EnsureCanViewCohort(user, cohort.SiteId, offering.ProgramId, cohort.Id.Value);
        return offering.ProgramId;
    }

    public static async Task<Guid> EnsureCanManageCohortAsync(
        ICurrentUser user,
        Cohort cohort,
        IProgramOfferingRepository offerings,
        CancellationToken ct)
    {
        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        EnsureCanManageCohort(user, cohort.SiteId, offering.ProgramId, cohort.Id.Value);
        return offering.ProgramId;
    }

    public static void EnsureCanViewExam(ICurrentUser user, Guid siteId, Guid programId, Guid cohortId, Guid examSessionId)
    {
        if (!user.CanViewExam(siteId, programId, cohortId, examSessionId))
            Deny();
    }

    public static void EnsureCanManageExam(ICurrentUser user, Guid siteId, Guid programId, Guid cohortId, Guid examSessionId)
    {
        if (!user.CanManageExam(siteId, programId, cohortId, examSessionId))
            Deny();
    }

    private static void Deny() => throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
}
