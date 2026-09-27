using Itech.Emailing.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PedagoraPilot.Application.Abstractions.Identity;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Access.Invitations;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Access;

public sealed class AccessInvitationProcessor(
    IAccessInvitationRepository invitations,
    IApplicationIdentityAccess identity,
    IOrganizationMemberRepository members,
    IUnitOfWork unitOfWork,
    IEmailingService emailing,
    IConfiguration configuration,
    IHostEnvironment environment) : IAccessInvitationProcessor
{
    public async Task ProcessCreatedAsync(Guid invitationId, string token, CancellationToken ct = default)
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
        await emailing.QueueHtmlAsync(
            invitation.Email,
            "Invitation à rejoindre Pedagora Pilot",
            $"<p>Bonjour {System.Net.WebUtility.HtmlEncode(displayName)},</p><p>Vous êtes invité à rejoindre Pedagora Pilot.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Accepter l'invitation</a></p>",
            $"Bonjour {displayName}, vous êtes invité à rejoindre Pedagora Pilot : {url}",
            attachments: null,
            tags: EmailUseCaseTags.AccessInviteUser,
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

            var member = await members.GetByAuthGateUserAsync(invitation.OrganizationId, provisioned.UserId, true, ct);
            if (member is null)
            {
                member = OrganizationMember.Create(invitation.OrganizationId, provisioned.UserId, invitation.Email, invitation.FirstName, invitation.LastName);
                await members.AddAsync(member, ct);
            }
            else
            {
                member.SynchronizeIdentity(invitation.Email, invitation.FirstName, invitation.LastName, true);
            }
            await members.SyncRoleProfilesAsync(member, new[] { invitation.RequestedRole }, true, ct);
            invitation.Accept(provisioned.UserId);
            await unitOfWork.SaveChangesAsync(ct);
            return provisioned.UserId;
        }
        catch (Exception ex)
        {
            invitation.RecordRetryableFailure(ex.Message);
            await unitOfWork.SaveChangesAsync(ct);
            throw;
        }
    }
}
