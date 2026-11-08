using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Infrastructure.Persistence;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Api.Controllers;

/// <summary>
/// Product-owned organization labels for the authenticated identity.
/// This endpoint never grants memberships or selects a tenant; AuthGate owns both.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/organization-labels")]
public sealed class WorkspaceContextNamesController(
    PedagoraPilotDbContext db,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId || userId == Guid.Empty)
            return Unauthorized();

        // Only disclose organization names related to this verified global identity.
        // The caller must separately intersect this projection with ACTIVE AuthGate
        // memberships; this read-only list is never used for authorization.
        var organizations = await db.Organizations
            .AsNoTracking()
            // Keep parity with the server-side workspace bootstrap policy.
            // A suspended or closed organization cannot be selected even by its owner.
            .Where(org => org.Status == OrganizationStatus.Draft ||
                          org.Status == OrganizationStatus.Active ||
                          org.Status == OrganizationStatus.Restricted)
            // Ownership originates in explicit organization provisioning (Register).
            // An invitation grants only an active membership, never ownership.
            .Where(org => org.OwnerUserId == userId || db.OrganizationMembers.Any(member =>
                member.OrganizationId == org.Id &&
                member.AuthGateUserId == userId && member.IsActive))
            .OrderBy(org => org.LegalName)
            .Select(org => new
            {
                organizationId = org.Id,
                name = org.ShortName != "" ? org.ShortName : org.LegalName
            })
            .ToListAsync(cancellationToken);

        Response.Headers.CacheControl = "no-store";
        return Ok(new { organizations });
    }
}
