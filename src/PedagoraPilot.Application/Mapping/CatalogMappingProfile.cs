using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Application.Mapping;
public sealed class CatalogMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration c)
    {
        c.CreateMap<ProgramFamily, ProgramFamilyReadModel>();
        c.CreateMap<TrainingProgram, TrainingProgramReadModel>().ForMember(d => d.Code, o => o.MapFrom(s => s.Code.Value));
        c.CreateMap<ProgramOffering, ProgramOfferingReadModel>();
        c.CreateMap<Referential, ReferentialReadModel>();
        c.CreateMap<ReferentialVersion, ReferentialVersionReadModel>();
    }
}

public sealed record ProgramFamilyReadModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "";
    public bool IsActive { get; init; }
}

public sealed record TrainingProgramReadModel
{
    public Guid Id { get; init; }
    public Guid FamilyId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string DescriptionKey { get; init; } = "";
    public string Icon { get; init; } = "";
    public int DurationHours { get; init; }
    public ProgramStatus Status { get; init; }
    public string? ExternalKey { get; init; }
}

public sealed record ProgramOfferingReadModel
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public Guid ProgramId { get; init; }
    public bool IsActive { get; init; }
    public string? ExternalKey { get; init; }
}

public sealed record ReferentialReadModel
{
    public Guid Id { get; init; }
    public Guid ProgramId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? ExternalKey { get; init; }
}

public sealed record ReferentialVersionReadModel
{
    public Guid Id { get; init; }
    public Guid ReferentialId { get; init; }
    public string VersionLabel { get; init; } = "";
    public string? CertificationCode { get; init; }
    public ReferentialVersionStatus Status { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public int TotalHours { get; init; }
    public int SheetCount { get; init; }
    public int RequiredDocumentCount { get; init; }
    public string? NotesKey { get; init; }
    public string? ExternalKey { get; init; }
}
