using Microsoft.AspNetCore.Authorization;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Authorization;
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUser _currentUser;
    public PermissionAuthorizationHandler(ICurrentUser currentUser) => _currentUser = currentUser;
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (_currentUser.IsAuthenticated && _currentUser.HasPermission(requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
