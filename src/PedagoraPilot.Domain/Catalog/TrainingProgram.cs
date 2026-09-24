using PedagoraPilot.Domain.Catalog.Events;
using PedagoraPilot.Domain.Catalog.ValueObjects;
using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Catalog;
public sealed class TrainingProgram : AggregateRoot
{
    private readonly List<string> _capabilities = [];
    private TrainingProgram()
    {
    }

    private TrainingProgram(Guid id, Guid familyId, ProgramCode code, string name, string descriptionKey, string icon, int durationHours, string? externalKey) : base(id)
    {
        FamilyId = familyId;
        Code = code;
        Name = name.Trim();
        DescriptionKey = descriptionKey.Trim();
        Icon = icon.Trim();
        DurationHours = durationHours;
        ExternalKey = externalKey;
        Status = ProgramStatus.Active;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid FamilyId { get; private set; }
    public ProgramCode Code { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string DescriptionKey { get; private set; } = string.Empty;
    public string Icon { get; private set; } = "ph-graduation-cap";
    public int DurationHours { get; private set; }
    public ProgramStatus Status { get; private set; }
    public string? ExternalKey { get; private set; }
    public IReadOnlyCollection<string> Capabilities => _capabilities.AsReadOnly();
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static TrainingProgram Create(Guid familyId, string code, string name, string descriptionKey, string icon, int durationHours, IEnumerable<string> capabilities, string? externalKey = null)
    {
        if (durationHours <= 0)
            throw new DomainException("PROGRAM_DURATION_INVALID");
        var x = new TrainingProgram(Guid.NewGuid(), familyId, ProgramCode.Create(code), name, descriptionKey, icon, durationHours, externalKey);
        x.ReplaceCapabilities(capabilities);
        x.RaiseDomainEvent(new TrainingProgramCreatedDomainEvent(x.Id));
        return x;
    }

    public void Update(Guid familyId, string name, string descriptionKey, string icon, int durationHours, ProgramStatus status, IEnumerable<string> capabilities)
    {
        if (durationHours <= 0)
            throw new DomainException("PROGRAM_DURATION_INVALID");
        FamilyId = familyId;
        Name = name.Trim();
        DescriptionKey = descriptionKey.Trim();
        Icon = icon.Trim();
        DurationHours = durationHours;
        Status = status;
        ReplaceCapabilities(capabilities);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingProgramUpdatedDomainEvent(Id));
    }

    private void ReplaceCapabilities(IEnumerable<string> values)
    {
        _capabilities.Clear();
        _capabilities.AddRange(values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
        if (_capabilities.Count == 0)
            throw new DomainException("PROGRAM_CAPABILITIES_REQUIRED");
    }
}
