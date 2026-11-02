using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Access.Members;

/// <summary>Product-owned role compatibility within ONE organization. Never used by AuthGate.</summary>
public static class OrganizationRoleCompatibilityPolicy
{
    public const string ConflictKey = "ACCESS_ROLE_COMBINATION_FORBIDDEN";

    public static bool IsAllowed(IEnumerable<string> roles)
    {
        var set = roles.Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (set.Count <= 1) return true;

        // Only these three responsibilities can coexist. All other product roles
        // (including administrators, directions, student, secretariat and read-only)
        // are exclusive within an organization.
        return set.All(role =>
            string.Equals(role, PedagoraPilotRoleCodes.Trainer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, PedagoraPilotRoleCodes.PedagogicalManager, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, PedagoraPilotRoleCodes.Jury, StringComparison.OrdinalIgnoreCase));
    }
}
