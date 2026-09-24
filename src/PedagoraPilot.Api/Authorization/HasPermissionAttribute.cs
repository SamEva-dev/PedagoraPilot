using Microsoft.AspNetCore.Authorization;

namespace PedagoraPilot.Api.Authorization;
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";
    public HasPermissionAttribute(string permission) => Policy = PolicyPrefix + permission;
}
