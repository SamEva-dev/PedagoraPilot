using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Contracts.Provisioning;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Mapping;
public sealed class OrganizationMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<Organization, ProvisionOrganizationResponse>().ForMember(d => d.OrganizationId, o => o.MapFrom(s => s.Id)).ForMember(d => d.Code, o => o.MapFrom(s => s.Code.Value)).ForMember(d => d.Name, o => o.MapFrom(s => s.LegalName)).ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
        configuration.CreateMap<Organization, OrganizationVerificationResponse>().ForMember(d => d.Name, o => o.MapFrom(s => s.LegalName)).ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString())).ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsLoginAllowed));
    }
}
