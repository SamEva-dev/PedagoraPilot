using Itech.Emailing.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PedagoraPilot.Application.Abstractions.Identity;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Access.Invitations;
using PedagoraPilot.Application.Access.Members;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Infrastructure.Access;

public sealed class AccessInvitationProcessor(
    IAccessInvitationRepository invitations,
    IApplicationIdentityAccess identity,
    IOrganizationMemberRepository members,
    IStudentMemberProvisioner studentMembers,
    IWorkplaceTutorAssignmentRepository tutorAssignments,
    IUnitOfWork unitOfWork,
    PedagoraPilot.Infrastructure.Persistence.PedagoraPilotDbContext db,
    IIdempotentEmailingService emailing,
    IConfiguration configuration,
    IHostEnvironment environment) : IAccessInvitationProcessor
{
    public async Task ProcessCreatedAsync(Guid invitationId, string token, Guid eventId, CancellationToken ct = default)
    {
        var invitation = await invitations.GetByIdAsync(new AccessInvitationId(invitationId), true, ct);
        if (invitation is null || !invitation.IsUsable(DateTime.UtcNow)) return;

        var bypass = IsInvitationBypassEnabled();
        if (bypass)
        {
            // QA bypass is restricted to explicit staging configuration. It bypasses
            // email acceptance, NOT AuthGate authentication or tenant authorization.
            var stagingQa = environment.IsStaging();
            var password = stagingQa
                ? configuration["QA:InvitationInitialPassword"]
                : configuration["AccessInvitations:DevelopmentDefaultPassword"];
            if (string.IsNullOrWhiteSpace(password) || (stagingQa && password.Length < 8))
                throw new InvalidOperationException(stagingQa
                    ? "QA:InvitationInitialPassword must be configured with at least 8 characters in staging."
                    : "AccessInvitations:DevelopmentDefaultPassword is required when bypass is enabled.");
            await AcceptCoreAsync(invitation, password, emailConfirmed: true, ct);
            return;
        }

        var acceptBase = configuration["AccessInvitations:FrontendAcceptUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(acceptBase))
            throw new InvalidOperationException("AccessInvitations:FrontendAcceptUrl is required when email acceptance is enabled.");

        // Production links must not disclose a single-use invitation token over HTTP.
        if (!Uri.TryCreate(acceptBase, UriKind.Absolute, out var acceptUri) ||
            (!environment.IsDevelopment() && acceptUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("ACCESS_INVITATION_HTTPS_URL_REQUIRED");
        var url = $"{acceptBase}/{Uri.EscapeDataString(token)}";
        var displayName = string.Join(' ', new[] { invitation.FirstName, invitation.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (displayName.Length == 0) displayName = invitation.Email;
        await emailing.QueueHtmlOnceAsync(
            invitation.Email,
            "Invitation à rejoindre Pedagora Pilot",
            $"<p>Bonjour {System.Net.WebUtility.HtmlEncode(displayName)},</p><p>Vous êtes invité à rejoindre Pedagora Pilot.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Accepter l'invitation</a></p>",
            $"Bonjour {displayName}, vous êtes invité à rejoindre Pedagora Pilot : {url}",
            attachments: null,
            tags: EmailUseCaseTags.AccessInviteUser,
            idempotencyKey: $"pedagora:invite:{invitation.OrganizationId:N}:{eventId:N}",
            tenantId: invitation.OrganizationId,
            cancellationToken: ct);
    }

    public async Task<AccessInvitationPreparation> PrepareAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("ACCESS_INVITATION_INVALID_OR_EXPIRED");
        var hash = AccessInvitation.ComputeTokenHash(token);
        var invitation = await invitations.GetByTokenHashAsync(hash, false, ct)
            ?? throw new InvalidOperationException("ACCESS_INVITATION_NOT_FOUND");
        if (!invitation.MatchesToken(token) || !invitation.IsUsable(DateTime.UtcNow))
            throw new InvalidOperationException("ACCESS_INVITATION_INVALID_OR_EXPIRED");
        // An obsolete tutor invitation must not even offer a password enrollment page.
        if (string.Equals(invitation.RequestedRole, PedagoraPilotRoleCodes.InternshipTutor, StringComparison.OrdinalIgnoreCase))
        {
            var linked = await tutorAssignments.GetByInvitationAsync(invitation.OrganizationId, invitation.Id.Value, ct);
            if (!linked.Any(x => x.Status == PedagoraPilot.Domain.Workplace.WorkplaceTutorAssignmentStatus.Pending))
                throw new UnauthorizedAccessException("WORKPLACE_TUTOR_INVITATION_REVOKED");
        }
        var existingActive = await identity.ExistingActiveIdentityAsync(invitation.Email, ct);
        return new AccessInvitationPreparation(invitation.Email, existingActive);
    }

    public async Task<AcceptedAccessInvitationResult> AcceptAsync(string token, string? password, string? authenticatedEmail, CancellationToken ct = default, Guid? authenticatedSubject = null)
    {
        var hash = AccessInvitation.ComputeTokenHash(token);
        var invitation = await invitations.GetByTokenHashAsync(hash, true, ct)
            ?? throw new InvalidOperationException("ACCESS_INVITATION_NOT_FOUND");
        if (!invitation.MatchesToken(token) || !invitation.IsUsable(DateTime.UtcNow))
            throw new InvalidOperationException("ACCESS_INVITATION_INVALID_OR_EXPIRED");
        // Prevent an obsolete invitation from provisioning a role after tutor reassignment.
        if (string.Equals(invitation.RequestedRole, PedagoraPilotRoleCodes.InternshipTutor, StringComparison.OrdinalIgnoreCase))
        {
            var attached = await tutorAssignments.GetByInvitationAsync(invitation.OrganizationId, invitation.Id.Value, ct);
            if (attached.Count == 0 || attached.All(x => x.Status != PedagoraPilot.Domain.Workplace.WorkplaceTutorAssignmentStatus.Pending))
                throw new UnauthorizedAccessException("WORKPLACE_TUTOR_INVITATION_REVOKED");
        }
        var bypass = IsInvitationBypassEnabled();
        var existingActive = await identity.ExistingActiveIdentityAsync(invitation.Email, ct);
        if (!bypass && !existingActive && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("ACCESS_INVITATION_PASSWORD_REQUIRED");
        // A forwarded invitation link proves possession of the email message, not the
        // identity owning an existing account. Require an authenticated subject with
        // the exact verified email before granting an application membership.
        if (existingActive && !string.Equals(authenticatedEmail?.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("ACCESS_INVITATION_AUTHENTICATION_REQUIRED");
        // Existing AuthGate identities retain their password; never overwrite or reset it.
        if (existingActive) password = null;
        if (authenticatedSubject.HasValue && (!existingActive || authenticatedSubject.Value == Guid.Empty))
            throw new UnauthorizedAccessException("ACCESS_INVITATION_OIDC_SUBJECT_INVALID");
        // Possession of the valid, single-use invitation token proves control of the invited address.
        // The identity must therefore be confirmed for this invitation even when bypass is disabled.
        var userId = await AcceptCoreAsync(invitation, password, emailConfirmed: true, ct, authenticatedSubject);
        return new AcceptedAccessInvitationResult(userId, invitation.Email);
    }

    private bool IsInvitationBypassEnabled()
    {
        // Never silently allow the legacy local-development bypass in staging.
        var legacyBypass = configuration.GetValue<bool>("AccessInvitations:BypassEmailAcceptance");
        if (legacyBypass && !environment.IsDevelopment())
            throw new InvalidOperationException("AccessInvitations:BypassEmailAcceptance is permitted only in Development.");

        var qaBypass = configuration.GetValue<bool>("QA:BypassInvitationAcceptance");
        if (qaBypass && (!environment.IsStaging() || !configuration.GetValue<bool>("QA:Enabled")))
            throw new InvalidOperationException("QA:BypassInvitationAcceptance requires QA:Enabled in Staging.");

        return legacyBypass || qaBypass;
    }

    private async Task<Guid> AcceptCoreAsync(AccessInvitation invitation, string? password, bool emailConfirmed, CancellationToken ct, Guid? expectedOidcSubject = null)
    {
        invitation.MarkProcessing();
        await unitOfWork.SaveChangesAsync(ct);
        try
        {
            var provisioned = await identity.EnsureAsync(new EnsureApplicationIdentityRequest(
                invitation.OrganizationId, invitation.Email, invitation.FirstName, invitation.LastName,
                invitation.RequestedRole, password, emailConfirmed, expectedOidcSubject, ReactivateRevokedMembership: true), ct);

            // AuthGate is the authority for application access. A provisioning response
            // describing inactive application access must never authorize a local membership,
            // even if the identity itself is valid. Do not consume this invitation.
            if (provisioned.UserId == Guid.Empty || !provisioned.AccessActive)
                throw new UnauthorizedAccessException("ACCESS_APPLICATION_MEMBERSHIP_INACTIVE");

            // Keep member, learner, enrollment and invitation acceptance in ONE local DB transaction.
            // AuthGate is an external system and cannot participate in that transaction.
            return await unitOfWork.ExecuteInTransactionAsync(async txCt =>
            {
                var member = await members.GetByAuthGateUserAsync(invitation.OrganizationId, provisioned.UserId, true, txCt);
                if (member is null)
                {
                    member = OrganizationMember.Create(invitation.OrganizationId, provisioned.UserId, invitation.Email, invitation.FirstName, invitation.LastName);
                    await members.AddAsync(member, txCt);
                }
                else
                {
                    // A new invitation explicitly issued by an Access.Manage administrator
                    // is the reinstatement workflow. Never resurrect other organization scopes.
                    member.SynchronizeIdentity(invitation.Email, invitation.FirstName, invitation.LastName, true);
                }

                // SyncRoleProfilesAsync synchronizes *all* role profile tables and deactivates
                // roles absent from its input. Preserve existing active roles when accepting
                // an additional invitation in the same organization.
                var roleCodes = (await members.GetRoleCodesAsync(invitation.OrganizationId, provisioned.UserId, txCt))
                    .Append(invitation.RequestedRole)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (!OrganizationRoleCompatibilityPolicy.IsAllowed(roleCodes))
                    throw new ConflictApplicationException(OrganizationRoleCompatibilityPolicy.ConflictKey);
                await members.SyncRoleProfilesAsync(member, roleCodes, true, txCt);

                if (string.Equals(invitation.RequestedRole, PedagoraPilotRoleCodes.Student, StringComparison.OrdinalIgnoreCase))
                    await studentMembers.EnsureAsync(invitation.OrganizationId, provisioned.UserId, invitation.Email, invitation.FirstName, invitation.LastName, invitation.CohortId, invitation.Phone, invitation.BirthDate, invitation.EnrolledOn, txCt);

                if (string.Equals(invitation.RequestedRole, PedagoraPilotRoleCodes.InternshipTutor, StringComparison.OrdinalIgnoreCase))
                {
                    var assignments = await tutorAssignments.GetByInvitationAsync(invitation.OrganizationId, invitation.Id.Value, txCt);
                    if (assignments.Count == 0) throw new ConflictApplicationException("WORKPLACE_TUTOR_ASSIGNMENT_NOT_FOUND");
                    var pendingAssignments = assignments.Where(x => x.Status == PedagoraPilot.Domain.Workplace.WorkplaceTutorAssignmentStatus.Pending).ToArray();
                    if (pendingAssignments.Length == 0) throw new ConflictApplicationException("WORKPLACE_TUTOR_INVITATION_REVOKED");
                    foreach (var assignment in pendingAssignments)
                        assignment.Activate(provisioned.UserId, invitation.Email);
                }
                invitation.Accept(provisioned.UserId);
                await unitOfWork.SaveChangesAsync(txCt);
                return provisioned.UserId;
            }, ct);
        }
        catch (Exception ex)
        {
            // A rolled-back transaction leaves EF entities tracked as if they had persisted.
            // Detach them before recording retry state, or SaveChanges could commit an enrollment
            // that failed the capacity/invitation transaction.
            db.ChangeTracker.Clear();
            var persistedInvitation = await invitations.GetByIdAsync(invitation.Id, true, ct);
            if (persistedInvitation is not null)
            {
                persistedInvitation.RecordRetryableFailure(ex.Message);
                await unitOfWork.SaveChangesAsync(ct);
            }
            throw;
        }
    }

}
