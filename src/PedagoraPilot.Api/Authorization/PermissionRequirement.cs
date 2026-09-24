using Microsoft.AspNetCore.Authorization;

namespace PedagoraPilot.Api.Authorization;
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
