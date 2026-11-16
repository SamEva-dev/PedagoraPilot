using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Api.Authorization;

/// <summary>Checks the persisted organization-level module flag in addition to HasPermission.
/// Route resource IDs are resolved to their owning organization; the URL alone is never trusted.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StatisticsModuleGuardAttribute : TypeFilterAttribute
{
    public StatisticsModuleGuardAttribute() : base(typeof(StatisticsModuleGuardFilter)) { }
}

public sealed class StatisticsModuleGuardFilter(
    IOrganizationRepository organizations,
    ITrainingSiteRepository sites,
    ICohortRepository cohorts,
    ICurrentUser current) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        Guid organizationId;
        if (TryGuid(context, "organizationId", out var requestedOrganizationId))
            organizationId = requestedOrganizationId;
        else if (TryGuid(context, "siteId", out var siteId))
        {
            var site = await sites.GetByIdAsync(siteId, false, context.HttpContext.RequestAborted);
            if (site is null) { context.Result = new NotFoundResult(); return; }
            organizationId = site.OrganizationId;
        }
        else if (TryGuid(context, "cohortId", out var cohortId))
        {
            var cohort = await cohorts.GetByIdAsync(new CohortId(cohortId), false, context.HttpContext.RequestAborted);
            if (cohort is null) { context.Result = new NotFoundResult(); return; }
            organizationId = cohort.OrganizationId;
        }
        else { context.Result = new BadRequestResult(); return; }

        // Existing handler checks remain in force; this filter never grants permissions.
        if (!current.IsAuthenticated || (current.OrganizationId != organizationId && !current.IsPlatformAdministrator))
        {
            context.Result = new ForbidResult();
            return;
        }
        var organization = await organizations.GetByIdAsync(organizationId, false, context.HttpContext.RequestAborted);
        if (organization is null) { context.Result = new NotFoundResult(); return; }
        if (!organization.EnabledModules.Contains("statistics", StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new { code = "ORGANIZATION_MODULE_DISABLED", module = "statistics" }) { StatusCode = 403 };
            return;
        }
        await next();
    }

    private static bool TryGuid(ActionExecutingContext context, string name, out Guid value)
    {
        if (context.ActionArguments.TryGetValue(name, out var raw) && raw is Guid guid && guid != Guid.Empty)
        {
            value = guid;
            return true;
        }
        value = Guid.Empty;
        return false;
    }
}
