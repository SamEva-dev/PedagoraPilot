using System.Text.Json;
using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Application.Catalog.Referentials;

public sealed record DeaStructureResult(Guid ReferentialVersionId, JsonElement Content, DateTime UpdatedAtUtc);
public sealed record GetDeaStructureQuery(Guid VersionId) : IQuery<DeaStructureResult?>;
public sealed record SaveDeaStructureDraftCommand(Guid VersionId, JsonElement Content) : ICommand<DeaStructureResult>;
/// <summary>Read-only section projection for DEA UI; structure remains the only source of truth.</summary>
public sealed record DeaStructureSectionResult(Guid ReferentialVersionId, string Section, JsonElement Items, DateTime UpdatedAtUtc);
public sealed record GetDeaStructureSectionQuery(Guid VersionId, string Section) : IQuery<DeaStructureSectionResult?>;
public sealed class GetDeaStructureSectionQueryHandler(
    IDeaReferentialStructureRepository structures, IReferentialVersionRepository versions,
    IReferentialRepository referentials, IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites, ITrainingProgramRepository programs,
    IProgramFamilyRepository families, ICurrentUser current)
    : IRequestHandler<GetDeaStructureSectionQuery, DeaStructureSectionResult?>
{
    public async Task<DeaStructureSectionResult?> Handle(GetDeaStructureSectionQuery r, CancellationToken ct)
    {
        // Never load an arbitrary property name from a URL; allow only supported sections.
        var section = r.Section.ToLowerInvariant() switch
        {
            "blocks" => "blocks",
            "competencies" => "competencies",
            "modules" => "modules",
            "stages" => "stages",
            "assessments" => "assessmentByBlock",
            _ => throw new ConflictApplicationException("DEA_STRUCTURE_SECTION_INVALID")
        };
        await DeaStructureAccess.EnsureAsync(r.VersionId, versions, referentials, programs, families,
            offerings, sites, current, false, ct);
        var row = await structures.ByVersionAsync(r.VersionId, false, ct);
        if (row is null) return null;
        using var document = JsonDocument.Parse(row.ContentJson);
        if (!document.RootElement.TryGetProperty(section, out var items) || items.ValueKind != JsonValueKind.Array)
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        return new DeaStructureSectionResult(r.VersionId, r.Section.ToLowerInvariant(), items.Clone(), row.UpdatedAtUtc);
    }
}


public sealed class GetDeaStructureQueryHandler(
    IDeaReferentialStructureRepository structures, IReferentialVersionRepository versions,
    IReferentialRepository referentials, IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites, ITrainingProgramRepository programs,
    IProgramFamilyRepository families, ICurrentUser current)
    : IRequestHandler<GetDeaStructureQuery, DeaStructureResult?>
{
    public async Task<DeaStructureResult?> Handle(GetDeaStructureQuery r, CancellationToken ct)
    {
        await DeaStructureAccess.EnsureAsync(r.VersionId, versions, referentials, programs, families,
            offerings, sites, current, false, ct);
        var row = await structures.ByVersionAsync(r.VersionId, false, ct);
        return row is null ? null : new DeaStructureResult(r.VersionId, JsonDocument.Parse(row.ContentJson).RootElement.Clone(), row.UpdatedAtUtc);
    }
}
public sealed class SaveDeaStructureDraftCommandHandler(
    IDeaReferentialStructureRepository structures, IReferentialVersionRepository versions,
    IReferentialRepository referentials, IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites, ITrainingProgramRepository programs,
    IProgramFamilyRepository families, ICurrentUser current)
    : IRequestHandler<SaveDeaStructureDraftCommand, DeaStructureResult>
{
    public async Task<DeaStructureResult> Handle(SaveDeaStructureDraftCommand r, CancellationToken ct)
    {
        var version = await DeaStructureAccess.EnsureAsync(r.VersionId, versions, referentials, programs, families,
            offerings, sites, current, true, ct);
        if (version.Status != ReferentialVersionStatus.Draft)
            throw new ConflictApplicationException(ErrorKeys.ReferentialVersionNotDraft);
        DeaStructureValidation.Validate(r.Content);
        var now = DateTime.UtcNow;
        var row = await structures.ByVersionAsync(r.VersionId, true, ct);
        if (row is null) { row = DeaReferentialStructure.Create(r.VersionId, r.Content.GetRawText(), now); await structures.AddAsync(row, ct); }
        else row.ReplaceDraft(r.Content.GetRawText(), now);
        return new DeaStructureResult(r.VersionId, r.Content.Clone(), now);
    }
}
internal static class DeaStructureAccess
{
    internal static async Task<ReferentialVersion> EnsureAsync(Guid versionId,
        IReferentialVersionRepository versions, IReferentialRepository referentials,
        ITrainingProgramRepository programs, IProgramFamilyRepository families,
        IProgramOfferingRepository offerings, ITrainingSiteRepository sites,
        ICurrentUser current, bool manage, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, versionId, versions, referentials,
            offerings, sites, programs, ct, manage);
        var v = await versions.GetByIdAsync(versionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialVersionNotFound);
        var rf = await referentials.GetByIdAsync(v.ReferentialId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialNotFound);
        var program = await programs.GetByIdAsync(rf.ProgramId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var family = await families.GetByCodeAsync("EMERGENCY_MEDICAL", false, ct);
        if (family is null || program.FamilyId != family.Id) throw new ConflictApplicationException("DEA_STRUCTURE_NOT_EMERGENCY_MEDICAL");
        return v;
    }
}
internal static class DeaStructureValidation
{
    internal static void Validate(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        // Verify shape and internally consistent references, not regulatory accreditation.
        var blocks = ReadArray(root, "blocks", 5);
        var competencies = ReadArray(root, "competencies", 11);
        var modules = ReadArray(root, "modules", 10);
        var stages = ReadArray(root, "stages", null);
        var assessments = ReadArray(root, "assessmentByBlock", 5);
        ValidateUniqueCodes(blocks);
        ValidateUniqueCodes(competencies);
        ValidateUniqueCodes(modules);
        var blockCodes = blocks.Select(b => RequiredCode(b)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assessmentsSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assessment in assessments)
        {
            var code = RequiredString(assessment, "block");
            if (!blockCodes.Contains(code) || !assessmentsSeen.Add(code)
                || !assessment.TryGetProperty("modes", out var modes) || modes.ValueKind != JsonValueKind.Array
                || modes.GetArrayLength() == 0
                || modes.EnumerateArray().Any(m => m.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(m.GetString())))
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        }
        var moduleNumbers = modules.Select(m => GetInt(m, "number")).ToArray();
        if (moduleNumbers.Distinct().Count() != 10 || moduleNumbers.Min() != 1 || moduleNumbers.Max() != 10)
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        if (stages.Any(stage => GetInt(stage, "hours") == 0 || string.IsNullOrWhiteSpace(RequiredString(stage, "label"))))
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        if (GetInt(root, "totalHours") != 801 || GetInt(root, "instituteHours") != 556
            || GetInt(root, "internshipHours") != 245)
            throw new ConflictApplicationException("DEA_STRUCTURE_HOURS_INVALID");
        var followUpHours = GetInt(root, "pedagogicalFollowupHours");
        var moduleSum = modules.Sum(m => GetInt(m, "hours"));
        var internshipSum = stages.Sum(m => GetInt(m, "hours"));
        if (followUpHours != 3 || moduleSum + followUpHours != 556 || internshipSum != 245)
            throw new ConflictApplicationException("DEA_STRUCTURE_HOURS_INVALID");
        var numbers = competencies.Select(c => GetInt(c, "number")).ToArray();
        if (competencies.Any(c => !string.Equals(RequiredString(c, "kind"), "Professional", StringComparison.OrdinalIgnoreCase)))
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        if (numbers.Distinct().Count() != 11 || numbers.Min() != 1 || numbers.Max() != 11)
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        var assigned = new List<int>();
        foreach (var block in blocks)
        {
            if (!block.TryGetProperty("competencyNumbers", out var refs) || refs.ValueKind != JsonValueKind.Array)
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
            foreach (var n in refs.EnumerateArray())
                assigned.Add(n.ValueKind == JsonValueKind.Number && n.TryGetInt32(out var value) ? value : -1);
        }
        if (assigned.Count != 11 || assigned.Distinct().Count() != 11 || !assigned.Order().SequenceEqual(numbers.Order()))
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
    }
    private static string RequiredCode(JsonElement row) => RequiredString(row, "code");
    private static string RequiredString(JsonElement row, string property)
    {
        if (row.ValueKind != JsonValueKind.Object || !row.TryGetProperty(property, out var value)
            || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        return value.GetString()!;
    }
    private static JsonElement.ArrayEnumerator ReadArray(JsonElement root, string name, int? exact)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array
            || (exact.HasValue && value.GetArrayLength() != exact.Value))
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        return value.EnumerateArray();
    }
    private static int GetInt(JsonElement row, string property)
    {
        if (!row.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var number) || number < 0)
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        return number;
    }
    private static void ValidateUniqueCodes(JsonElement.ArrayEnumerator rows)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.ValueKind != JsonValueKind.Object || !row.TryGetProperty("code", out var code)
                || code.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(code.GetString())
                || !seen.Add(code.GetString()!))
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        }
    }
}
