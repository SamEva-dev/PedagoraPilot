using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Workspace;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Organizations.Sites;
public sealed class UpdateTrainingSiteCommandHandler(ITrainingSiteRepository sites, IOrganizationRepository organizations, ICurrentUser current)
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

        site.Update(request.Code, request.Name, request.City, request.Address, request.PostalCode,
            request.Phone, request.Email, request.Manager, ParseStatus(request.Status));

        return new WorkspaceSiteDto(site.Id, site.ExternalKey ?? $"site-{site.Code.Value.ToLowerInvariant()}",
            site.OrganizationId, $"org-{organization.Code.Value.ToLowerInvariant()}", site.Code.Value,
            site.Name, site.City, site.Address, site.PostalCode, site.Phone, site.Email, site.Manager,
            site.Status.ToString().ToLowerInvariant(), site.IsActive);
    }

    private static TrainingSiteStatus ParseStatus(string value)
        => value.Equals("attention", StringComparison.OrdinalIgnoreCase)
            ? TrainingSiteStatus.Attention
            : value.Equals("inactive", StringComparison.OrdinalIgnoreCase)
                ? TrainingSiteStatus.Inactive
                : TrainingSiteStatus.Active;
}
