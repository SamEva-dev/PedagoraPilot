using System.Security.Claims;
using System.Text.Json;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Authorization;
public sealed class HttpCurrentUser : ICurrentUser
{
    private const string ScopePrefix = "pedagora.scope.";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private IReadOnlyCollection<ContextualScopeAssignment>? _contextualScopes;

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

    public IReadOnlyCollection<ContextualScopeAssignment> ContextualScopes =>
        _contextualScopes ??= ParseContextualScopes(Permissions);

    public bool HasContextualScopeRestrictions =>
        !IsPlatformAdministrator
        && ContextualScopes.Count > 0
        && ContextualScopes.All(scope => scope.Level != ContextualScopeLevel.Organization);

    public bool HasPermission(string permission) => Permissions.Contains(permission) || IsPlatformAdministrator;

    public bool IsPlatformAdministrator
    {
        get
        {
            if (string.Equals(User.FindFirst(AuthGateClaimNames.PlatformAdmin)?.Value, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            return Roles.Any(role => role.Equals("PlatformAdministrator", StringComparison.OrdinalIgnoreCase) || role.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase) || role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool CanViewSite(Guid siteId)
    {
        if (!HasContextualScopeRestrictions) return true;
        return ContextualScopes.Any(scope => scope.SiteId == siteId);
    }

    public bool CanManageSite(Guid siteId)
    {
        if (!HasContextualScopeRestrictions) return true;
        return ContextualScopes.Any(scope => scope.Level == ContextualScopeLevel.Site && scope.SiteId == siteId);
    }

    public bool CanViewProgram(Guid siteId, Guid programId)
    {
        if (!HasContextualScopeRestrictions) return true;
        return ContextualScopes.Any(scope =>
            (scope.Level == ContextualScopeLevel.Site && scope.SiteId == siteId)
            || (scope.ProgramId == programId && scope.SiteId == siteId));
    }

    public bool CanManageProgram(Guid siteId, Guid programId)
    {
        if (!HasContextualScopeRestrictions) return true;
        return ContextualScopes.Any(scope =>
            (scope.Level == ContextualScopeLevel.Site && scope.SiteId == siteId)
            || (scope.Level == ContextualScopeLevel.Program && scope.SiteId == siteId && scope.ProgramId == programId));
    }

    public bool CanViewCohort(Guid siteId, Guid programId, Guid cohortId)
    {
        if (!HasContextualScopeRestrictions) return true;
        return ContextualScopes.Any(scope =>
            (scope.Level == ContextualScopeLevel.Site && scope.SiteId == siteId)
            || (scope.Level == ContextualScopeLevel.Program && scope.SiteId == siteId && scope.ProgramId == programId)
            || (scope.CohortId == cohortId && scope.SiteId == siteId && scope.ProgramId == programId));
    }

    public bool CanManageCohort(Guid siteId, Guid programId, Guid cohortId)
    {
        if (!HasContextualScopeRestrictions) return true;
        return ContextualScopes.Any(scope =>
            (scope.Level == ContextualScopeLevel.Site && scope.SiteId == siteId)
            || (scope.Level == ContextualScopeLevel.Program && scope.SiteId == siteId && scope.ProgramId == programId)
            || (scope.Level == ContextualScopeLevel.Cohort && scope.SiteId == siteId && scope.ProgramId == programId && scope.CohortId == cohortId));
    }

    public bool CanViewExam(Guid siteId, Guid programId, Guid cohortId, Guid examSessionId)
    {
        if (!HasContextualScopeRestrictions) return true;
        return ContextualScopes.Any(scope =>
            (scope.Level == ContextualScopeLevel.Site && scope.SiteId == siteId)
            || (scope.Level == ContextualScopeLevel.Program && scope.SiteId == siteId && scope.ProgramId == programId)
            || (scope.Level == ContextualScopeLevel.Cohort && scope.SiteId == siteId && scope.ProgramId == programId && scope.CohortId == cohortId)
            || (scope.Level == ContextualScopeLevel.Exam && scope.SiteId == siteId && scope.ProgramId == programId && scope.CohortId == cohortId && scope.ExamSessionId == examSessionId));
    }

    public bool CanManageExam(Guid siteId, Guid programId, Guid cohortId, Guid examSessionId) =>
        CanViewExam(siteId, programId, cohortId, examSessionId);

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

    private static IReadOnlyCollection<ContextualScopeAssignment> ParseContextualScopes(IEnumerable<string> permissions)
    {
        var scopes = new List<ContextualScopeAssignment>();
        foreach (var permission in permissions)
        {
            if (!permission.StartsWith(ScopePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = permission.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var kind = parts[0][ScopePrefix.Length..].Trim().ToLowerInvariant();
            switch (kind)
            {
                case "organization":
                    scopes.Add(new ContextualScopeAssignment(ContextualScopeLevel.Organization));
                    break;
                case "site" when parts.Length == 2 && Guid.TryParse(parts[1], out var siteId):
                    scopes.Add(new ContextualScopeAssignment(ContextualScopeLevel.Site, siteId));
                    break;
                case "program" when parts.Length == 3
                    && Guid.TryParse(parts[1], out var programSiteId)
                    && Guid.TryParse(parts[2], out var programId):
                    scopes.Add(new ContextualScopeAssignment(ContextualScopeLevel.Program, programSiteId, programId));
                    break;
                case "cohort" when parts.Length == 4
                    && Guid.TryParse(parts[1], out var cohortSiteId)
                    && Guid.TryParse(parts[2], out var cohortProgramId)
                    && Guid.TryParse(parts[3], out var cohortId):
                    scopes.Add(new ContextualScopeAssignment(ContextualScopeLevel.Cohort, cohortSiteId, cohortProgramId, cohortId));
                    break;
                case "exam" when parts.Length == 5
                    && Guid.TryParse(parts[1], out var examSiteId)
                    && Guid.TryParse(parts[2], out var examProgramId)
                    && Guid.TryParse(parts[3], out var examCohortId)
                    && Guid.TryParse(parts[4], out var examId):
                    scopes.Add(new ContextualScopeAssignment(ContextualScopeLevel.Exam, examSiteId, examProgramId, examCohortId, examId));
                    break;
            }
        }

        return scopes;
    }
}
