using Itech.Emailing.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PedagoraPilot.Application.Abstractions.Identity;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Access.Invitations;
using PedagoraPilot.Application.Access.Members;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Infrastructure.Access;

public sealed class AccessInvitationProcessor(
    IAccessInvitationRepository invitations,
    IApplicationIdentityAccess identity,
    IOrganizationMemberRepository members,
    IStudentMemberProvisioner studentMembers,
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

        var bypass = configuration.GetValue<bool>("AccessInvitations:BypassEmailAcceptance");
        if (bypass)
        {
            if (environment.IsProduction())
                throw new InvalidOperationException("AccessInvitations:BypassEmailAcceptance is forbidden in Production.");
            var password = configuration["AccessInvitations:DevelopmentDefaultPassword"];
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("AccessInvitations:DevelopmentDefaultPassword is required when bypass is enabled.");
            await AcceptCoreAsync(invitation, password, emailConfirmed: true, ct);
            return;
        }

        var acceptBase = configuration["AccessInvitations:FrontendAcceptUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(acceptBase))
            throw new InvalidOperationException("AccessInvitations:FrontendAcceptUrl is required when email acceptance is enabled.");

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

    public async Task<Guid> AcceptAsync(string token, string? password, CancellationToken ct = default)
    {
        var hash = AccessInvitation.ComputeTokenHash(token);
        var invitation = await invitations.GetByTokenHashAsync(hash, true, ct)
            ?? throw new InvalidOperationException("ACCESS_INVITATION_NOT_FOUND");
        if (!invitation.MatchesToken(token) || !invitation.IsUsable(DateTime.UtcNow))
            throw new InvalidOperationException("ACCESS_INVITATION_INVALID_OR_EXPIRED");
        var bypass = configuration.GetValue<bool>("AccessInvitations:BypassEmailAcceptance");
        if (!bypass && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("ACCESS_INVITATION_PASSWORD_REQUIRED");
        return await AcceptCoreAsync(invitation, password, emailConfirmed: bypass, ct);
    }

    private async Task<Guid> AcceptCoreAsync(AccessInvitation invitation, string? password, bool emailConfirmed, CancellationToken ct)
    {
        invitation.MarkProcessing();
        await unitOfWork.SaveChangesAsync(ct);
        try
        {
            var provisioned = await identity.EnsureAsync(new EnsureApplicationIdentityRequest(
                invitation.OrganizationId, invitation.Email, invitation.FirstName, invitation.LastName,
                invitation.RequestedRole, password, emailConfirmed), ct);

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
                    member.SynchronizeIdentity(invitation.Email, invitation.FirstName, invitation.LastName, true);
                }
                await members.SyncRoleProfilesAsync(member, new[] { invitation.RequestedRole }, true, txCt);

                if (string.Equals(invitation.RequestedRole, PedagoraPilotRoleCodes.Student, StringComparison.OrdinalIgnoreCase))
                    await studentMembers.EnsureAsync(invitation.OrganizationId, provisioned.UserId, invitation.Email, invitation.FirstName, invitation.LastName, invitation.CohortId, invitation.Phone, invitation.BirthDate, invitation.EnrolledOn, txCt);

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
