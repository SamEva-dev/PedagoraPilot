using PedagoraPilot.Application.Abstractions.Security;
using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Application.Catalog.Programs;
public sealed class CreateProgramCommandHandler(IProgramFamilyRepository families, ITrainingProgramRepository programs, ICurrentUser current) : IRequestHandler<CreateProgramCommand, TrainingProgramDto>
{
    public async Task<TrainingProgramDto> Handle(CreateProgramCommand r, CancellationToken ct)
    {
        TenantScope.RequirePlatform(current);
        var family = await families.GetByCodeAsync(r.FamilyCode, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramFamilyNotFound);
        if (await programs.CodeExistsAsync(r.Code, null, ct))
            throw new ConflictApplicationException(ErrorKeys.ProgramCodeAlreadyExists);
        var p = TrainingProgram.Create(family.Id, r.Code, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, r.EnabledModules, r.ExternalKey);
        if (Enum.TryParse<ProgramStatus>(r.Status, true, out var s) && s != ProgramStatus.Active)
            p.Update(family.Id, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, s, r.EnabledModules);
        await programs.AddAsync(p, ct);
        return ProgramDtoFactory.Create(p, family.Code, [], null);
    }
}

public sealed class UpdateProgramCommandHandler(IProgramFamilyRepository families, ITrainingProgramRepository programs, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<UpdateProgramCommand, TrainingProgramDto>
{
    public async Task<TrainingProgramDto> Handle(UpdateProgramCommand r, CancellationToken ct)
    {
        TenantScope.RequirePlatform(current);
        var p = await programs.GetByIdAsync(r.Id, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var family = await families.GetByCodeAsync(r.FamilyCode, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramFamilyNotFound);
        if (!Enum.TryParse<ProgramStatus>(r.Status, true, out var status))
            status = ProgramStatus.Active;
        p.Update(family.Id, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, status, r.EnabledModules);
        var sites = await offerings.Query(false).Where(x => x.ProgramId == p.Id && x.IsActive).Select(x => x.ExternalKey ?? x.SiteId.ToString()).ToListAsync(ct);
        return ProgramDtoFactory.Create(p, family.Code, sites, null);
    }
}

public sealed class SetProgramOfferingCommandHandler(ITrainingProgramRepository programs, ITrainingSiteRepository sites, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<SetProgramOfferingCommand, ProgramOfferingDto>
{
    public async Task<ProgramOfferingDto> Handle(SetProgramOfferingCommand r, CancellationToken ct)
    {
        var p = await programs.GetByIdAsync(r.ProgramId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var s = await sites.GetByIdAsync(r.SiteId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
        TenantScope.Ensure(current, s.OrganizationId);
        var x = await offerings.GetAsync(r.SiteId, r.ProgramId, true, ct);
        if (x is null)
        {
            x = ProgramOffering.Create(r.SiteId, r.ProgramId, $"off-{s.Code.Value.ToLowerInvariant()}-{p.Code.Value.ToLowerInvariant()}");
            await offerings.AddAsync(x, ct);
        }

        x.SetActive(r.Active);
        return new ProgramOfferingDto(x.Id, x.ExternalKey ?? x.Id.ToString(), x.SiteId, s.ExternalKey ?? x.SiteId.ToString(), x.ProgramId, p.ExternalKey ?? x.ProgramId.ToString(), x.IsActive);
    }
}

internal static class ProgramDtoFactory
{
    public static TrainingProgramDto Create(TrainingProgram p, string familyCode, IReadOnlyCollection<string> siteKeys, string? refVersion, IReadOnlyCollection<string>? capabilities = null) => new(p.Id, p.ExternalKey ?? p.Id.ToString(), p.Code.Value, p.Name, familyCode, LegacyCategory(familyCode), p.Icon, p.DescriptionKey, p.DurationHours, p.Status.ToString().ToLowerInvariant(), capabilities ?? p.Capabilities, siteKeys, refVersion);
    private static string LegacyCategory(string familyCode) => familyCode.ToUpperInvariant() switch
    {
        "ROAD_EDUCATION" => "teacher",
        "MOTORCYCLE" => "motorcycle",
        "HEAVY_VEHICLE" => "heavy-vehicle",
        "PASSENGER_TRANSPORT" => "passenger-transport",
        _ => familyCode.ToLowerInvariant().Replace('_', '-')};
}
