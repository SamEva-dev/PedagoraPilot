using PedagoraPilot.Application.Abstractions.Security;
using DomainRelay.Abstractions;
using PedagoraPilot.Domain.Training;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Application.Catalog.Programs;
public sealed class CreateProgramCommandHandler(
    IProgramFamilyRepository families,
    ITrainingProgramRepository programs,
    IReferentialRepository referentials,
    IReferentialVersionRepository versions,
    ICurrentUser current) : IRequestHandler<CreateProgramCommand, TrainingProgramDto>
{
    public async Task<TrainingProgramDto> Handle(CreateProgramCommand r, CancellationToken ct)
    {
        var organizationId = TenantScope.Organization(current);
        var family = await families.GetByCodeAsync(r.FamilyCode, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramFamilyNotFound);
        if (await programs.CodeExistsAsync(organizationId, r.Code, null, ct))
            throw new ConflictApplicationException(ErrorKeys.ProgramCodeAlreadyExists);

        var p = TrainingProgram.Create(family.Id, r.Code, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, r.EnabledModules, r.ExternalKey, organizationId);
        if (Enum.TryParse<ProgramStatus>(r.Status, true, out var s) && s != ProgramStatus.Active)
            p.Update(family.Id, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, s, r.EnabledModules);
        await programs.AddAsync(p, ct);

        // A tenant-created formation must immediately own a usable referential version.
        // Otherwise a cohort cannot be created because Cohort requires an active ReferentialVersion.
        var referential = Referential.Create(p.Id, p.Code.Value, p.Name);
        await referentials.AddAsync(referential, ct);
        var (versionLabel, certificationCode) = ReferentialVersionText.Parse(r.ReferenceVersion);
        var version = ReferentialVersion.CreateDraft(
            referential.Id,
            versionLabel,
            certificationCode,
            DateOnly.FromDateTime(DateTime.UtcNow),
            p.DurationHours,
            0,
            0,
            p.Capabilities);
        version.Publish(DateTime.UtcNow);
        await versions.AddAsync(version, ct);

        return ProgramDtoFactory.Create(p, family.Code, [], ReferentialVersionText.Display(version));
    }
}

public sealed class UpdateProgramCommandHandler(
    IProgramFamilyRepository families,
    ITrainingProgramRepository programs,
    IProgramOfferingRepository offerings,
    IReferentialRepository referentials,
    IReferentialVersionRepository versions,
    ICurrentUser current) : IRequestHandler<UpdateProgramCommand, TrainingProgramDto>
{
    public async Task<TrainingProgramDto> Handle(UpdateProgramCommand r, CancellationToken ct)
    {
        var p = await programs.GetByIdAsync(r.Id, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        if (p.OrganizationId.HasValue)
            TenantScope.Ensure(current, p.OrganizationId.Value);
        else
            TenantScope.RequirePlatform(current);
        var family = await families.GetByCodeAsync(r.FamilyCode, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramFamilyNotFound);
        if (!Enum.TryParse<ProgramStatus>(r.Status, true, out var status))
            status = ProgramStatus.Active;
        p.Update(family.Id, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, status, r.EnabledModules);

        var referential = await referentials.Query(false).FirstOrDefaultAsync(x => x.ProgramId == p.Id, ct);
        string? displayVersion = null;
        if (referential is not null)
        {
            var active = await versions.Query(true)
                .Where(x => x.ReferentialId == referential.Id && x.Status == ReferentialVersionStatus.Active)
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync(ct);
            var (versionLabel, certificationCode) = ReferentialVersionText.Parse(r.ReferenceVersion);
            displayVersion = active is null ? null : ReferentialVersionText.Display(active);
            if (active is null || !ReferentialVersionText.Equals(active, versionLabel, certificationCode))
            {
                if (active is not null)
                    active.Archive(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
                var next = ReferentialVersion.CreateDraft(
                    referential.Id,
                    versionLabel,
                    certificationCode,
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    p.DurationHours,
                    0,
                    0,
                    p.Capabilities);
                next.Publish(DateTime.UtcNow);
                await versions.AddAsync(next, ct);
                displayVersion = ReferentialVersionText.Display(next);
            }
        }

        var sites = await offerings.Query(false).Where(x => x.ProgramId == p.Id && x.IsActive).Select(x => x.ExternalKey ?? x.SiteId.ToString()).ToListAsync(ct);
        return ProgramDtoFactory.Create(p, family.Code, sites, displayVersion ?? r.ReferenceVersion);
    }
}

public sealed class SetProgramOfferingCommandHandler(ITrainingProgramRepository programs, ITrainingSiteRepository sites, IProgramOfferingRepository offerings, ICohortRepository cohorts, ICurrentUser current) : IRequestHandler<SetProgramOfferingCommand, ProgramOfferingDto>
{
    public async Task<ProgramOfferingDto> Handle(SetProgramOfferingCommand r, CancellationToken ct)
    {
        var p = await programs.GetByIdAsync(r.ProgramId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var s = await sites.GetByIdAsync(r.SiteId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
        TenantScope.Ensure(current, s.OrganizationId);
        if (p.OrganizationId.HasValue && p.OrganizationId.Value != s.OrganizationId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        ContextualScope.EnsureCanManageProgram(current, s.Id, p.Id);
        var x = await offerings.GetAsync(r.SiteId, r.ProgramId, true, ct);
        if (x is null)
        {
            if (!r.Active)
                throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
            x = ProgramOffering.Create(r.SiteId, r.ProgramId, $"off-{s.Code.Value.ToLowerInvariant()}-{p.Code.Value.ToLowerInvariant()}");
            await offerings.AddAsync(x, ct);
        }

        if (!r.Active && await cohorts.Query(false).AnyAsync(c => c.ProgramOfferingId == x.Id
            && c.Status != CohortStatus.Completed && c.Status != CohortStatus.Cancelled, ct))
            throw new ConflictApplicationException(ErrorKeys.ProgramOfferingActiveCohorts);
        x.SetActive(r.Active);
        return new ProgramOfferingDto(x.Id, x.ExternalKey ?? x.Id.ToString(), x.SiteId, s.ExternalKey ?? x.SiteId.ToString(), x.ProgramId, p.ExternalKey ?? x.ProgramId.ToString(), x.IsActive);
    }
}

internal static class ReferentialVersionText
{
    public static (string VersionLabel, string? CertificationCode) Parse(string value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length == 0) return ("1", null);
        var normalized = text.Replace('·', '-');
        var parts = normalized.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && parts[0].StartsWith("RNCP", StringComparison.OrdinalIgnoreCase))
            return (parts[1], parts[0].ToUpperInvariant());
        return (text, text.StartsWith("RNCP", StringComparison.OrdinalIgnoreCase) ? text.ToUpperInvariant() : null);
    }

    public static string Display(ReferentialVersion version) =>
        string.IsNullOrWhiteSpace(version.CertificationCode)
            ? version.VersionLabel
            : $"{version.CertificationCode} · {version.VersionLabel}";

    public static bool Equals(ReferentialVersion current, string versionLabel, string? certificationCode) =>
        string.Equals(current.VersionLabel, versionLabel, StringComparison.OrdinalIgnoreCase)
        && string.Equals(current.CertificationCode ?? string.Empty, certificationCode ?? string.Empty, StringComparison.OrdinalIgnoreCase);
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
