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
        if (root.ValueKind != JsonValueKind.Object)
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");

        // Draft authoring is progressive: an empty scaffold and partially completed
        // sections must be persistable. This is NOT a regulatory publication check.
        var blocks = ReadArray(root, "blocks", null).ToArray();
        var competencies = ReadArray(root, "competencies", null).ToArray();
        var modules = ReadArray(root, "modules", null).ToArray();
        var stages = ReadArray(root, "stages", null).ToArray();
        var assessments = ReadArray(root, "assessmentByBlock", null).ToArray();
        if (blocks.Length > 5 || competencies.Length > 11 || modules.Length > 10 || assessments.Length > 5)
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        ValidateUniqueCodes(blocks);
        ValidateUniqueCodes(competencies);
        ValidateUniqueCodes(modules);

        var blockCodes = blocks.Select(RequiredCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var competencyNumbers = new HashSet<int>();
        foreach (var competency in competencies)
        {
            var number = GetInt(competency, "number");
            if (number is < 1 or > 11 || !competencyNumbers.Add(number)
                || !string.Equals(RequiredString(competency, "kind"), "Professional", StringComparison.OrdinalIgnoreCase))
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        }

        var assignedNumbers = new HashSet<int>();
        foreach (var block in blocks)
        {
            if (!block.TryGetProperty("competencyNumbers", out var refs) || refs.ValueKind != JsonValueKind.Array)
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
            foreach (var reference in refs.EnumerateArray())
            {
                if (reference.ValueKind != JsonValueKind.Number || !reference.TryGetInt32(out var number)
                    || !competencyNumbers.Contains(number) || !assignedNumbers.Add(number))
                    throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
            }
        }

        var assessmentCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assessment in assessments)
        {
            var block = RequiredString(assessment, "block");
            if (!blockCodes.Contains(block) || !assessmentCodes.Add(block)
                || !assessment.TryGetProperty("modes", out var modes) || modes.ValueKind != JsonValueKind.Array
                || modes.GetArrayLength() == 0
                || modes.EnumerateArray().Any(m => m.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(m.GetString())))
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
        }

        var moduleNumbers = new HashSet<int>();
        var moduleHours = 0;
        foreach (var module in modules)
        {
            var number = GetInt(module, "number");
            var hours = GetRequiredModuleHours(module);
            if (number is < 1 or > 10 || hours == 0 || !moduleNumbers.Add(number))
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
            moduleHours += hours;
        }
        var internshipHours = 0;
        foreach (var stage in stages)
        {
            var hours = GetInt(stage, "hours");
            if (hours == 0 || string.IsNullOrWhiteSpace(RequiredString(stage, "label")))
                throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
            internshipHours += hours;
        }

        if (GetInt(root, "totalHours") != 801 || GetInt(root, "instituteHours") != 556
            || GetInt(root, "internshipHours") != 245 || GetInt(root, "pedagogicalFollowupHours") != 3
            || moduleHours > 553 || internshipHours > 245)
            throw new ConflictApplicationException("DEA_STRUCTURE_HOURS_INVALID");
        // Full-size sections must also respect their complete cumulative constraints.
        if (modules.Length == 10 && moduleHours != 553)
            throw new ConflictApplicationException("DEA_STRUCTURE_HOURS_INVALID");
        if (blocks.Length == 5 && competencies.Length == 11 && assignedNumbers.Count != 11)
            throw new ConflictApplicationException("DEA_STRUCTURE_INVALID");
    }
    // Keep the existing generic error for cross-section validation, but return a
    // field-specific code for missing, non-numeric, zero or negative module hours.
    private static int GetRequiredModuleHours(JsonElement module)
    {
        if (module.ValueKind != JsonValueKind.Object
            || !module.TryGetProperty("hours", out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var hours)
            || hours <= 0)
            throw new ConflictApplicationException("DEA_STRUCTURE_MODULE_HOURS_REQUIRED");
        return hours;
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
    private static void ValidateUniqueCodes(IEnumerable<JsonElement> rows)
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
