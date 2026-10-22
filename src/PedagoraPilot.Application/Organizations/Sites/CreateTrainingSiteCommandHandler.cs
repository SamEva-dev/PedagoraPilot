using PedagoraPilot.Application.Abstractions.Security;
using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Workspace;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Organizations.Sites;
public sealed class CreateTrainingSiteCommandHandler : IRequestHandler<CreateTrainingSiteCommand, WorkspaceSiteDto>
{
    private readonly IOrganizationRepository _orgs;
    private readonly ITrainingSiteRepository _sites;
    private readonly ICurrentUser _current;
    private readonly IOrganizationMemberRepository _members;

    public CreateTrainingSiteCommandHandler(IOrganizationRepository orgs, ITrainingSiteRepository sites, ICurrentUser current, IOrganizationMemberRepository members)
    {
        _orgs = orgs;
        _sites = sites;
        _current = current;
        _members = members;
    }

    public async Task<WorkspaceSiteDto> Handle(CreateTrainingSiteCommand r, CancellationToken ct)
    {
        TenantScope.Ensure(_current, r.OrganizationId);
        if (_current.HasContextualScopeRestrictions)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var org = await _orgs.GetByIdAsync(r.OrganizationId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
        if (await _sites.CodeExistsAsync(r.OrganizationId, r.Code, ct))
            throw new ConflictApplicationException(ErrorKeys.SiteCodeAlreadyExists);

        var status = ParseStatus(r.Status);
        var site = TrainingSite.Create(r.OrganizationId, r.Code, r.Name, r.City, r.ExternalKey,
            r.Address, r.PostalCode, r.Phone, r.Email, r.Manager, status);
        if (r.ManagerAuthGateUserId is Guid managerId)
        {
            var member = await _members.GetByAuthGateUserAsync(r.OrganizationId, managerId, false, ct);
            if (member is null || !member.IsActive)
                throw new ConflictApplicationException("SITE_MANAGER_MEMBER_REQUIRED");
            var roleCodes = await _members.GetRoleCodesAsync(r.OrganizationId, managerId, ct);
            if (!roleCodes.Any(x => x.Equals("PedagoraPilot.SiteDirection", StringComparison.OrdinalIgnoreCase)))
                throw new ConflictApplicationException("SITE_MANAGER_DIRECTION_REQUIRED");
            site.AssignManager(managerId, member.DisplayName);
        }
        else
            throw new ConflictApplicationException("SITE_MANAGER_ID_REQUIRED");
        await _sites.AddAsync(site, ct);
        return ToDto(site, org.Code.Value);
    }

    private static TrainingSiteStatus ParseStatus(string value)
        => value.Equals("attention", StringComparison.OrdinalIgnoreCase)
            ? TrainingSiteStatus.Attention
            : value.Equals("inactive", StringComparison.OrdinalIgnoreCase)
                ? TrainingSiteStatus.Inactive
                : TrainingSiteStatus.Active;

    private static WorkspaceSiteDto ToDto(TrainingSite site, string organizationCode)
        => new(site.Id, site.ExternalKey ?? $"site-{site.Code.Value.ToLowerInvariant()}", site.OrganizationId,
            $"org-{organizationCode.ToLowerInvariant()}", site.Code.Value, site.Name, site.City,
            site.Address, site.PostalCode, site.Phone, site.Email, site.Manager,
            site.Status.ToString().ToLowerInvariant(), site.IsActive, site.ManagerAuthGateUserId);
}
