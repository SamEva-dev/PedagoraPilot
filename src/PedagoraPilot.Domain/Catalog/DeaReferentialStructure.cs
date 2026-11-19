using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Catalog;

/// <summary>A draft-only, version-scoped DEA structure. Publication is deliberately separate.</summary>
public sealed class DeaReferentialStructure : AggregateRoot<DeaReferentialStructureId>
{
    private DeaReferentialStructure() { }
    private DeaReferentialStructure(Guid versionId, string contentJson, DateTime now) : base(DeaReferentialStructureId.New())
    {
        ReferentialVersionId = versionId;
        ContentJson = contentJson;
        CreatedAtUtc = UpdatedAtUtc = now;
    }
    public Guid ReferentialVersionId { get; private set; }
    public string ContentJson { get; private set; } = "{}";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public static DeaReferentialStructure Create(Guid versionId, string json, DateTime now) => new(versionId, json, now);
    public void ReplaceDraft(string json, DateTime now) { ContentJson = json; UpdatedAtUtc = now; }
}
