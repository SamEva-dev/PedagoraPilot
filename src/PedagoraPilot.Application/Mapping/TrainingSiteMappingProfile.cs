using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Mapping;
public sealed class OrganizationWorkspaceMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<Organization, OrganizationWorkspaceReadModel>().ForMember(d => d.Code, o => o.MapFrom(s => s.Code.Value)).ForMember(d => d.Active, o => o.MapFrom(s => s.Status == OrganizationStatus.Active));
        configuration.CreateMap<TrainingSite, TrainingSiteReadModel>().ForMember(d => d.Code, o => o.MapFrom(s => s.Code.Value)).ForMember(d => d.Active, o => o.MapFrom(s => s.IsActive));
    }
}

public sealed record OrganizationWorkspaceReadModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string LegalName { get; init; } = "";
    public bool Active { get; init; }
}

public sealed record TrainingSiteReadModel
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string City { get; init; } = "";
    public string? ExternalKey { get; init; }
    public bool Active { get; init; }
}
