namespace PedagoraPilot.Application.Abstractions.Security;
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? OrganizationId { get; }

    string? Application { get; }

    string? Email { get; }

    string? DisplayName { get; }

    IReadOnlySet<string> Roles { get; }

    IReadOnlySet<string> Permissions { get; }

    bool IsPlatformAdministrator { get; }

    IReadOnlyCollection<ContextualScopeAssignment> ContextualScopes { get; }

    bool HasContextualScopeRestrictions { get; }

    bool HasPermission(string permission);

    bool CanViewSite(Guid siteId);

    bool CanManageSite(Guid siteId);

    bool CanViewProgram(Guid siteId, Guid programId);

    bool CanManageProgram(Guid siteId, Guid programId);

    bool CanViewCohort(Guid siteId, Guid programId, Guid cohortId);

    bool CanManageCohort(Guid siteId, Guid programId, Guid cohortId);

    bool CanViewExam(Guid siteId, Guid programId, Guid cohortId, Guid examSessionId);

    bool CanManageExam(Guid siteId, Guid programId, Guid cohortId, Guid examSessionId);
}
