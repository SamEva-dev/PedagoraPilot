using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Contracts.Provisioning;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Organizations.Queries;
public sealed class GetOrganizationVerificationQueryHandler : IRequestHandler<GetOrganizationVerificationQuery, OrganizationVerificationResponse?>
{
    private readonly IOrganizationRepository _organizations;
    private readonly IObjectMapper _mapper;
    public GetOrganizationVerificationQueryHandler(IOrganizationRepository organizations, IObjectMapper mapper)
    {
        _organizations = organizations;
        _mapper = mapper;
    }

    public async Task<OrganizationVerificationResponse?> Handle(GetOrganizationVerificationQuery request, CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(request.OrganizationId, isTracking: false, cancellationToken);
        return organization is null ? null : _mapper.Map<Organization, OrganizationVerificationResponse>(organization);
    }
}
