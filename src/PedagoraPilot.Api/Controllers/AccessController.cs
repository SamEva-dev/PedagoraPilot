using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Identity;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Access.Invitations;
using PedagoraPilot.Application.Access.Assignments;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;

/// <summary>
/// Product-facing access boundary. Product invitations are owned by PedagoraPilot.
/// AuthGate remains behind this boundary only for generic identity/application-access operations.
/// </summary>
[ApiController, Route("api/v1/access"), Authorize]
public sealed class AccessController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IOrganizationMemberRepository members,
    IOrganizationRepository organizations,
    IAccessInvitationRepository invitations,
    IUserBusinessAssignmentService businessAssignments,
    IApplicationIdentityAccess identityAccess,
    IUnitOfWork unitOfWork,
    ICurrentUser current,
    IMediator mediator) : ControllerBase
{
    [HttpGet, HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || current.OrganizationId.Value == Guid.Empty) return Forbid();
        var localAssignments = await businessAssignments.ListForOrganizationAsync(current.OrganizationId.Value, ct);
        var organization = await organizations.GetByIdAsync(current.OrganizationId.Value, false, ct);
        if (organization is null) return NotFound();

        // During the migration of access administration, active membership details still come from
        // AuthGate. Pedagora invitations are stripped from that response and replaced by the product-owned
        // invitations below. This keeps the screen compatible while removing AuthGate invitation ownership.
        var authResult = await ForwardRawAsync(HttpMethod.Get, "/api/pedagora/access" + Request.QueryString, null, ct);
        if (!authResult.IsSuccessStatusCode)
            return new ContentResult { StatusCode = (int)authResult.StatusCode, Content = authResult.Content, ContentType = authResult.ContentType };

        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(authResult.Content) ? "{}" : authResult.Content);
        var root = document.RootElement;
        var items = new List<JsonObject>();
        if (root.TryGetProperty("items", out var authItems) && authItems.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in authItems.EnumerateArray())
            {
                if (item.TryGetProperty("isInvitation", out var invitationFlag) && invitationFlag.ValueKind == JsonValueKind.True)
                    continue;

                var node = JsonNode.Parse(item.GetRawText())?.AsObject();
                if (node is null) continue;

                if (node["id"]?.GetValue<string>() is { Length: > 0 } rawId && Guid.TryParse(rawId, out var authGateUserId))
                {
                    // AuthGate owns the effective authentication role. Keep that role when it is
                    // present and only fall back to the PedagoraPilot role profiles when AuthGate
                    // has no product role. This avoids stale legacy role-profile rows forcing the
                    // UI to display OrganizationAdministrator for every member.
                    var hasAuthGateRole = node["roles"] is JsonArray authRoles
                        && authRoles.Any(x => x is not null && !string.IsNullOrWhiteSpace(x.GetValue<string>()));
                    if (!hasAuthGateRole)
                    {
                        var productRoles = await members.GetRoleCodesAsync(current.OrganizationId.Value, authGateUserId, ct);
                        if (productRoles.Count > 0)
                            node["roles"] = JsonSerializer.SerializeToNode(productRoles);
                    }

                    // Server-derived per-tenant ownership, never inferred from role labels.
                    node["isOrganizationOwner"] = organization.OwnerUserId == authGateUserId;

                    // Business assignments belong to PedagoraPilot. Never expose AuthGate scope
                    // assignments in this product-facing list.
                    node["assignments"] = JsonSerializer.SerializeToNode(
                        localAssignments.TryGetValue(authGateUserId, out var assignments)
                            ? assignments
                            : Array.Empty<UserBusinessAssignmentDto>(),
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }

                items.Add(node);
            }
        }

        var pending = await invitations.ListForOrganizationAsync(current.OrganizationId.Value, false, ct);
        var localInvitationRows = pending.Select(x => new
        {
            id = x.Id.Value.ToString("D"),
            email = x.Email,
            firstName = x.FirstName,
            lastName = x.LastName,
            status = "Invited",
            primaryRole = x.RequestedRole,
            roles = new[] { x.RequestedRole },
            permissions = Array.Empty<object>(),
            assignments = Array.Empty<object>(),
            isInvitation = true
        }).ToArray();

        var rolesAvailable = root.TryGetProperty("rolesAvailable", out var roles) && roles.ValueKind == JsonValueKind.Array
            ? roles.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).Where(x => x is not null).Cast<string>().ToArray()
            : PedagoraPilotRoleCodes.All.Where(x => x != PedagoraPilotRoleCodes.PlatformAdministrator).ToArray();

        var active = items.Cast<object>().ToList();
        var merged = active.Concat(localInvitationRows.Cast<object>()).ToArray();
        return Ok(new
        {
            items = merged,
            rolesAvailable,
            totalCount = merged.Length,
            page = 1,
            pageSize = Math.Max(merged.Length, 1)
        });
    }

    [HttpGet("trainers"), HasPermission(PedagoraPilotPermissionCodes.Cohorts.Manage)]
    public async Task<IActionResult> Trainers(CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || current.OrganizationId.Value == Guid.Empty) return Forbid();
        var rows = await members.GetTrainerDirectoryAsync(current.OrganizationId.Value, ct);
        return Ok(rows.Select(x => new
        {
            id = x.AuthGateUserId.ToString("D"), email = x.Email, firstName = x.FirstName, lastName = x.LastName,
            status = "Active", roles = x.Roles, permissions = Array.Empty<object>(), assignments = Array.Empty<object>(), isInvitation = false
        }));
    }

    public sealed record InviteRequest(string FirstName, string LastName, string Email, string Role,
        Guid? SiteId, Guid? ProgramId, Guid? CohortId, Guid? ExamSessionId,
        string? Phone = null, DateOnly? BirthDate = null, DateOnly? EnrolledOn = null);

    [HttpPost("invite"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> Invite([FromBody] InviteRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateAccessInvitationCommand(body.FirstName, body.LastName, body.Email, body.Role,
            body.SiteId, body.ProgramId, body.CohortId, body.ExamSessionId,
            body.Phone, body.BirthDate, body.EnrolledOn), ct);
        return Ok(new { id = result.Id, result.Email, result.FirstName, result.LastName, role = result.Role, status = result.Status });
    }

    public sealed record AcceptInvitationRequest(string Token, string? Password);

    [AllowAnonymous, HttpPost("invitations/accept")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest body, [FromServices] IAccessInvitationProcessor processor, CancellationToken ct)
    {
        var userId = await processor.AcceptAsync(body.Token, body.Password, ct);
        return Ok(new { userId, status = "Accepted" });
    }

    [HttpDelete("invitations/{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> RevokeInvitation(Guid id, CancellationToken ct)
    {
        await mediator.Send(new CancelAccessInvitationCommand(id), ct);
        return NoContent();
    }

    public sealed record SetRolesRequest(IReadOnlyList<string> Roles);

    [HttpPut("{userId:guid}/roles"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> SetRoles(Guid userId, [FromBody] SetRolesRequest body, CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || current.OrganizationId.Value == Guid.Empty) return Forbid();
        if (!current.UserId.HasValue || current.UserId.Value == Guid.Empty) return Forbid();
        if (userId == current.UserId.Value) return BadRequest(new { error = "cannot_change_own_roles" });

        var requested = body?.Roles?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
        if (requested.Length != 1) return BadRequest(new { error = "invalid_product_role" });
        var canonicalRole = NormalizeRole(requested[0]);
        if (canonicalRole is null) return BadRequest(new { error = "invalid_product_role" });

        var member = await members.GetByAuthGateUserAsync(current.OrganizationId.Value, userId, true, ct);
        if (member is null) return NotFound(new { error = "member_not_found" });

        var currentRoles = await members.GetRoleCodesAsync(current.OrganizationId.Value, userId, ct);
        var targetIsAdministrator = currentRoles.Contains(PedagoraPilotRoleCodes.OrganizationAdministrator, StringComparer.OrdinalIgnoreCase);
        var newIsAdministrator = string.Equals(canonicalRole, PedagoraPilotRoleCodes.OrganizationAdministrator, StringComparison.OrdinalIgnoreCase);
        if ((targetIsAdministrator || newIsAdministrator) && !current.HasPermission(PedagoraPilotPermissionCodes.Access.PrivilegedManage))
            return Forbid();
        if (targetIsAdministrator && !newIsAdministrator
            && await members.CountActiveRoleAsync(current.OrganizationId.Value, PedagoraPilotRoleCodes.OrganizationAdministrator, ct) <= 1)
            return Conflict(new { error = "last_administrator" });

        await identityAccess.SetRoleAsync(new SetApplicationRoleRequest(current.OrganizationId.Value, userId, canonicalRole), ct);
        await members.SyncRoleProfilesAsync(member, new[] { canonicalRole }, true, ct);
        member.RegisterRoleChange(canonicalRole);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("ownership/transfer"), HasPermission(PedagoraPilotPermissionCodes.Organization.OwnershipTransfer)]
    public Task<IActionResult> TransferOwnership([FromBody] JsonElement body, CancellationToken ct) => ForwardAsync(HttpMethod.Post, "/api/pedagora/access/ownership/transfer", body, ct);

    public sealed record SetStatusRequest(bool Active);

    [HttpPut("{userId:guid}/status"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> SetStatus(Guid userId, [FromBody] SetStatusRequest body, CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || current.OrganizationId.Value == Guid.Empty) return Forbid();
        if (!current.UserId.HasValue || current.UserId.Value == Guid.Empty) return Forbid();
        if (userId == current.UserId.Value) return BadRequest(new { error = "cannot_change_own_access" });

        var member = await members.GetByAuthGateUserAsync(current.OrganizationId.Value, userId, true, ct);
        if (member is null) return NotFound(new { error = "member_not_found" });

        // Capture the canonical role before AuthGate changes the membership status. A suspended
        // Pedagora profile has inactive role rows, so the local projection alone cannot restore
        // the role on reactivation.
        var snapshot = await GetAuthGateMemberSnapshotAsync(userId, ct);
        if (snapshot is null) return NotFound(new { error = "membership_not_found" });

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new { active = body.Active }));
        var raw = await ForwardRawAsync(HttpMethod.Put, $"/api/pedagora/access/{userId:D}/status", json.RootElement, ct);
        if (!raw.IsSuccessStatusCode)
            return new ContentResult { StatusCode = (int)raw.StatusCode, Content = raw.Content, ContentType = raw.ContentType };

        member.SynchronizeIdentity(snapshot.Email, snapshot.FirstName, snapshot.LastName, body.Active);
        await members.SyncRoleProfilesAsync(member, new[] { snapshot.PrimaryRole }, body.Active, ct);
        member.RegisterStatusChange(body.Active);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{userId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> Revoke(Guid userId, CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || current.OrganizationId.Value == Guid.Empty) return Forbid();
        if (!current.UserId.HasValue || current.UserId.Value == Guid.Empty) return Forbid();
        if (userId == current.UserId.Value) return BadRequest(new { error = "cannot_revoke_own_access" });

        var member = await members.GetByAuthGateUserAsync(current.OrganizationId.Value, userId, true, ct);
        if (member is null) return NotFound(new { error = "member_not_found" });

        var raw = await ForwardRawAsync(HttpMethod.Delete, $"/api/pedagora/access/{userId:D}", null, ct);
        if (!raw.IsSuccessStatusCode)
            return new ContentResult { StatusCode = (int)raw.StatusCode, Content = raw.Content, ContentType = raw.ContentType };

        // Revoke immediately in the product projection. AuthGate's SyncPedagoraMember outbox is
        // retained as a reconciliation mechanism, but the UI and business directories no longer
        // wait for that asynchronous message to become consistent.
        member.SynchronizeIdentity(member.Email, member.FirstName, member.LastName, false);
        await members.SyncRoleProfilesAsync(member, Array.Empty<string>(), false, ct);
        member.RegisterRevoked();
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("{userId:guid}/permissions/{key}"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public Task<IActionResult> SetPermission(Guid userId, string key, [FromBody] JsonElement body, CancellationToken ct) => ForwardAsync(HttpMethod.Put, $"/api/pedagora/access/{userId:D}/permissions/{Uri.EscapeDataString(key)}", body, ct);

    public sealed record AddBusinessAssignmentRequest(string Role, Guid CohortId, bool MakePedagogicalManager = false);

    [HttpPost("{userId:guid}/assignments"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> AddAssignment(Guid userId, [FromBody] AddBusinessAssignmentRequest body, CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || current.OrganizationId.Value == Guid.Empty) return Forbid();
        if (body.CohortId == Guid.Empty) return BadRequest(new { error = "cohort_required" });
        var result = await businessAssignments.AddAsync(current.OrganizationId.Value, userId, body.CohortId, body.Role, body.MakePedagogicalManager, ct);
        return Ok(result);
    }

    [HttpDelete("{userId:guid}/assignments/{assignmentId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> RemoveAssignment(Guid userId, Guid assignmentId, CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || current.OrganizationId.Value == Guid.Empty) return Forbid();
        await businessAssignments.RemoveAsync(current.OrganizationId.Value, userId, assignmentId, ct);
        return NoContent();
    }

    private sealed record AuthGateMemberSnapshot(string Email, string FirstName, string LastName, string PrimaryRole);

    private async Task<AuthGateMemberSnapshot?> GetAuthGateMemberSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var raw = await ForwardRawAsync(HttpMethod.Get,
            $"/api/pedagora/access?page=1&pageSize=100&search={Uri.EscapeDataString(userId.ToString("D"))}", null, ct);
        if (!raw.IsSuccessStatusCode || string.IsNullOrWhiteSpace(raw.Content)) return null;

        using var document = JsonDocument.Parse(raw.Content);
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idNode) || !Guid.TryParse(idNode.GetString(), out var id) || id != userId) continue;
            var role = item.TryGetProperty("primaryRole", out var primaryRole) ? primaryRole.GetString() : null;
            if (string.IsNullOrWhiteSpace(role) && item.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var roleNode in roles.EnumerateArray())
                {
                    if (roleNode.ValueKind != JsonValueKind.String) continue;
                    role = roleNode.GetString();
                    if (!string.IsNullOrWhiteSpace(role)) break;
                }
            }
            if (string.IsNullOrWhiteSpace(role)) return null;
            return new AuthGateMemberSnapshot(
                item.TryGetProperty("email", out var email) ? email.GetString() ?? string.Empty : string.Empty,
                item.TryGetProperty("firstName", out var firstName) ? firstName.GetString() ?? string.Empty : string.Empty,
                item.TryGetProperty("lastName", out var lastName) ? lastName.GetString() ?? string.Empty : string.Empty,
                role);
        }
        return null;
    }

    private static string? NormalizeRole(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (value.Length == 0) return null;
        var roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["organization_admin"] = PedagoraPilotRoleCodes.OrganizationAdministrator,
            ["organization_direction"] = PedagoraPilotRoleCodes.OrganizationDirection,
            ["site_direction"] = PedagoraPilotRoleCodes.SiteDirection,
            ["pedagogical_manager"] = PedagoraPilotRoleCodes.PedagogicalManager,
            ["secretariat"] = PedagoraPilotRoleCodes.Secretariat,
            ["trainer"] = PedagoraPilotRoleCodes.Trainer,
            ["student"] = PedagoraPilotRoleCodes.Student,
            ["jury"] = PedagoraPilotRoleCodes.Jury,
            ["read_only"] = PedagoraPilotRoleCodes.ReadOnly,
            ["direction"] = PedagoraPilotRoleCodes.OrganizationDirection,
            ["formateur"] = PedagoraPilotRoleCodes.Trainer,
            ["stagiaire"] = PedagoraPilotRoleCodes.Student
        };
        if (roles.TryGetValue(value, out var mapped)) return mapped;
        return PedagoraPilotRoleCodes.All.Contains(value, StringComparer.OrdinalIgnoreCase)
               && !string.Equals(value, PedagoraPilotRoleCodes.PlatformAdministrator, StringComparison.OrdinalIgnoreCase)
            ? value
            : null;
    }

    private async Task<IActionResult> ForwardAsync(HttpMethod method, string path, JsonElement? body, CancellationToken ct)
    {
        var raw = await ForwardRawAsync(method, path, body, ct);
        return new ContentResult { StatusCode = (int)raw.StatusCode, Content = raw.Content, ContentType = raw.ContentType };
    }

    private async Task<(System.Net.HttpStatusCode StatusCode, bool IsSuccessStatusCode, string Content, string ContentType)> ForwardRawAsync(HttpMethod method, string path, JsonElement? body, CancellationToken ct)
    {
        var baseUrl = configuration["AuthGate:BaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("AuthGate:BaseUrl is required.");
        using var request = new HttpRequestMessage(method, baseUrl + path);
        var authorization = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(authorization)) request.Headers.TryAddWithoutValidation("Authorization", authorization);
        if (body.HasValue) request.Content = new StringContent(body.Value.GetRawText(), Encoding.UTF8, "application/json");
        var client = httpClientFactory.CreateClient("AuthGateAccess");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var content = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, response.IsSuccessStatusCode, content, response.Content?.Headers.ContentType?.ToString() ?? "application/json");
    }
}
