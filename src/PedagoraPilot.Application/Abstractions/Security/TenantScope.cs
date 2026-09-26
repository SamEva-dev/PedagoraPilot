using PedagoraPilot.Application.Common.Errors;

namespace PedagoraPilot.Application.Abstractions.Security;

/// <summary>Every tenant-owned resource must be checked against the authenticated scope.</summary>
public static class TenantScope
{
    public static bool IsPlatformAdministrator(ICurrentUser user) => user.IsPlatformAdministrator;

    public static Guid? Organization(ICurrentUser user)
    {
        if (!user.IsAuthenticated)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        if (IsPlatformAdministrator(user))
            return null;
        return user.OrganizationId ?? throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }

    public static void Ensure(ICurrentUser user, Guid organizationId)
    {
        var scopedOrganization = Organization(user);
        if (scopedOrganization.HasValue && scopedOrganization.Value != organizationId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }

    public static void RequirePlatform(ICurrentUser user)
    {
        if (!user.IsAuthenticated || !IsPlatformAdministrator(user))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}
