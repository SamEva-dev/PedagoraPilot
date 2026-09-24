using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Contracts.Provisioning;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Organizations.Provision;
public sealed class ProvisionOrganizationCommandHandler : IRequestHandler<ProvisionOrganizationCommand, ProvisionOrganizationResponse>
{
    private readonly IOrganizationRepository _organizations;
    private readonly IObjectMapper _mapper;
    public ProvisionOrganizationCommandHandler(IOrganizationRepository organizations, IObjectMapper mapper)
    {
        _organizations = organizations;
        _mapper = mapper;
    }

    public async Task<ProvisionOrganizationResponse> Handle(ProvisionOrganizationCommand request, CancellationToken cancellationToken)
    {
        // Business idempotence in addition to HTTP Idempotency-Key protection.
        var existing = await _organizations.GetByOwnerUserIdAsync(request.ExternalUserId, isTracking: false, cancellationToken);
        if (existing is not null)
            return _mapper.Map<Organization, ProvisionOrganizationResponse>(existing);
        var code = await GenerateUniqueCodeAsync(cancellationToken);
        var organization = Organization.Provision(request.ExternalUserId, code, request.LegalName, request.CountryCode, request.Email, request.Phone);
        await _organizations.AddAsync(organization, cancellationToken);
        return _mapper.Map<Organization, ProvisionOrganizationResponse>(organization);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var candidate = $"PP-{Guid.NewGuid():N}"[..11].ToUpperInvariant();
            if (!await _organizations.CodeExistsAsync(candidate, cancellationToken))
                return candidate;
        }

        throw new InvalidOperationException("Unable to generate a unique organization code.");
    }
}
