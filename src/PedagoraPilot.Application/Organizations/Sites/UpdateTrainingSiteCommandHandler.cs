using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Workspace;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Organizations.Sites;
public sealed class UpdateTrainingSiteCommandHandler(ITrainingSiteRepository sites, IOrganizationRepository organizations, ICurrentUser current, IOrganizationMemberRepository members)
    : IRequestHandler<UpdateTrainingSiteCommand, WorkspaceSiteDto>
{
    public async Task<WorkspaceSiteDto> Handle(UpdateTrainingSiteCommand request, CancellationToken ct)
    {
        TenantScope.Ensure(current, request.OrganizationId);
        var site = await sites.GetByIdAsync(request.SiteId, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
        if (site.OrganizationId != request.OrganizationId)
            throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
        ContextualScope.EnsureCanManageSite(current, site.Id);
        var organization = await organizations.GetByIdAsync(request.OrganizationId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);

        if (!site.Code.Value.Equals(request.Code, StringComparison.OrdinalIgnoreCase)
            && await sites.CodeExistsAsync(request.OrganizationId, request.Code, ct))
            throw new ConflictApplicationException(ErrorKeys.SiteCodeAlreadyExists);

        // Resolve the requested identity before mutating the tracked aggregate.
        string managerName = site.Manager;
        if (request.ManagerAuthGateUserId is Guid managerId)
        {
            // The verified organization owner may manage their own sites even if
            // AuthGate provisioning did not create an OrganizationMember row.
            if (managerId == organization.OwnerUserId)
            {
                var ownerMember = await members.GetByAuthGateUserAsync(request.OrganizationId, managerId, false, ct);
                managerName = !string.IsNullOrWhiteSpace(ownerMember?.DisplayName)
                    ? ownerMember.DisplayName
                    : current.UserId == managerId && !string.IsNullOrWhiteSpace(current.DisplayName)
                        ? current.DisplayName
                        : organization.OwnerEmail;
            }
            else
            {
                var member = await members.GetByAuthGateUserAsync(request.OrganizationId, managerId, false, ct);
                if (member is null || !member.IsActive)
                    throw new ConflictApplicationException("SITE_MANAGER_MEMBER_REQUIRED");
                var roleCodes = await members.GetRoleCodesAsync(request.OrganizationId, managerId, ct);
                if (!roleCodes.Any(x => x.Equals("PedagoraPilot.SiteDirection", StringComparison.OrdinalIgnoreCase)))
                    throw new ConflictApplicationException("SITE_MANAGER_DIRECTION_REQUIRED");
                managerName = member.DisplayName;
            }
        }
        else if (!site.ManagerAuthGateUserId.HasValue &&
                 !string.Equals(request.Manager?.Trim(), site.Manager, StringComparison.Ordinal))
            throw new ConflictApplicationException("SITE_MANAGER_ID_REQUIRED");

        if (ParseStatus(request.Status) != TrainingSiteStatus.Inactive &&
            !site.ManagerAuthGateUserId.HasValue && !request.ManagerAuthGateUserId.HasValue)
            throw new ConflictApplicationException("SITE_MANAGER_ID_REQUIRED");

        site.Update(request.Code, request.Name, request.City, request.Address, request.PostalCode,
            request.Phone, request.Email, managerName, ParseStatus(request.Status));
        if (request.ManagerAuthGateUserId is Guid assignedId)
            site.AssignManager(assignedId, managerName);

        return new WorkspaceSiteDto(site.Id, site.ExternalKey ?? $"site-{site.Code.Value.ToLowerInvariant()}",
            site.OrganizationId, $"org-{organization.Code.Value.ToLowerInvariant()}", site.Code.Value,
            site.Name, site.City, site.Address, site.PostalCode, site.Phone, site.Email, site.Manager,
            site.Status.ToString().ToLowerInvariant(), site.IsActive, site.ManagerAuthGateUserId);
    }

    private static TrainingSiteStatus ParseStatus(string value)
        => value.Equals("attention", StringComparison.OrdinalIgnoreCase)
            ? TrainingSiteStatus.Attention
            : value.Equals("inactive", StringComparison.OrdinalIgnoreCase)
                ? TrainingSiteStatus.Inactive
                : TrainingSiteStatus.Active;
}
