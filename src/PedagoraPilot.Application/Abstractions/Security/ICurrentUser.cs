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

    bool HasPermission(string permission);
}
