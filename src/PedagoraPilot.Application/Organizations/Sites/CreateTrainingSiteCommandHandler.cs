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
    public CreateTrainingSiteCommandHandler(IOrganizationRepository orgs, ITrainingSiteRepository sites)
    {
        _orgs = orgs;
        _sites = sites;
    }

    public async Task<WorkspaceSiteDto> Handle(CreateTrainingSiteCommand r, CancellationToken ct)
    {
        var org = await _orgs.GetByIdAsync(r.OrganizationId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
        if (await _sites.CodeExistsAsync(r.OrganizationId, r.Code, ct))
            throw new ConflictApplicationException(ErrorKeys.SiteCodeAlreadyExists);
        var site = TrainingSite.Create(r.OrganizationId, r.Code, r.Name, r.City, r.ExternalKey);
        await _sites.AddAsync(site, ct);
        return new WorkspaceSiteDto(site.Id, site.ExternalKey ?? $"site-{site.Code.Value.ToLowerInvariant()}", site.OrganizationId, $"org-{org.Code.Value.ToLowerInvariant()}", site.Code.Value, site.Name, site.City, site.IsActive);
    }
}
