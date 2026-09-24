using System.Security.Claims;
using System.Text.Json;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Authorization;
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;
    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public Guid? UserId => TryReadGuid(ClaimTypes.NameIdentifier, "sub");
    public Guid? OrganizationId => TryReadGuid(AuthGateClaimNames.OrganizationId, AuthGateClaimNames.OrganizationIdShort);
    public string? Application => User.FindFirst(AuthGateClaimNames.Application)?.Value;
    public string? Email => User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
    public string? DisplayName => User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? User.FindFirst("preferred_username")?.Value;
    public IReadOnlySet<string> Roles => ReadValues(AuthGateClaimNames.Roles, ClaimTypes.Role);
    public IReadOnlySet<string> Permissions => ReadValues(AuthGateClaimNames.Permissions, "permission");

    public bool HasPermission(string permission) => Permissions.Contains(permission) || IsPlatformAdministrator();
    private bool IsPlatformAdministrator()
    {
        if (string.Equals(User.FindFirst(AuthGateClaimNames.PlatformAdmin)?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        return Roles.Any(role => role.Contains("PlatformAdministrator", StringComparison.OrdinalIgnoreCase) || role.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase) || role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
    }

    private Guid? TryReadGuid(params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var raw = User.FindFirst(claimType)?.Value;
            if (Guid.TryParse(raw, out var value))
                return value;
        }

        return null;
    }

    private HashSet<string> ReadValues(params string[] claimTypes)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in User.Claims.Where(c => claimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase)))
        {
            var raw = claim.Value.Trim();
            if (raw.Length == 0)
                continue;
            if (raw.StartsWith('['))
            {
                try
                {
                    foreach (var item in JsonSerializer.Deserialize<string[]>(raw) ?? [])
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                            result.Add(item.Trim());
                    }

                    continue;
                }
                catch (JsonException)
                {
                // Fall through to scalar parsing.
                }
            }

            foreach (var item in raw.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                result.Add(item);
        }

        return result;
    }
}
