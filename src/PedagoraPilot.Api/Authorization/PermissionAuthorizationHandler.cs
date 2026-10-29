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
            "AUTH-DIAG: Permission={Permission}, Granted={Granted}, ClaimTypes={ClaimTypes}",
            requirement.Permission,
            granted,
            string.Join(",", context.User.Claims
                .Select(c => c.Type)
                .Distinct()));

        if (granted)
        {
            context.Succeed(requirement);
        }

        if (_currentUser.IsAuthenticated && _currentUser.HasPermission(requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
