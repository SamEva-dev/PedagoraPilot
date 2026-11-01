using Microsoft.AspNetCore.Authorization;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Authorization;
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUser _currentUser;
    // À injecter dans le constructeur si absent
    private readonly ILogger<PermissionAuthorizationHandler> _logger;
    public PermissionAuthorizationHandler(ICurrentUser currentUser, ILogger<PermissionAuthorizationHandler> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var granted = _currentUser.HasPermission(requirement.Permission);

        _logger.LogInformation(
                "AUTH-DIAG: Authenticated={Authenticated}, AuthType={AuthType}, Permission={Permission}, Granted={Granted}",
                context.User.Identity?.IsAuthenticated ?? false,
                context.User.Identity?.AuthenticationType ?? "none",
                requirement.Permission,
                _currentUser.HasPermission(requirement.Permission));

        if (granted)
        {
            context.Succeed(requirement);
        }

        if (_currentUser.IsAuthenticated && _currentUser.HasPermission(requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
