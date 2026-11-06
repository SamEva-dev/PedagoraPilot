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

    private TrainingProgram(Guid id, Guid? organizationId, Guid familyId, ProgramCode code, string name, string descriptionKey, string icon, int durationHours, string? externalKey, Guid? responsibleTrainerAuthGateUserId, string? responsibleTrainerDisplayName) : base(id)
    {
        OrganizationId = organizationId;
        FamilyId = familyId;
        Code = code;
        Name = name.Trim();
        DescriptionKey = descriptionKey.Trim();
        Icon = icon.Trim();
        DurationHours = durationHours;
        ExternalKey = externalKey;
        ResponsibleTrainerAuthGateUserId = responsibleTrainerAuthGateUserId;
        ResponsibleTrainerDisplayName = NormalizeOptional(responsibleTrainerDisplayName);
        Status = ProgramStatus.Draft;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid? OrganizationId { get; private set; }
    public Guid FamilyId { get; private set; }
    public ProgramCode Code { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string DescriptionKey { get; private set; } = string.Empty;
    public string Icon { get; private set; } = "ph-graduation-cap";
    public int DurationHours { get; private set; }
    public ProgramStatus Status { get; private set; }
    public string? ExternalKey { get; private set; }
    public Guid? ResponsibleTrainerAuthGateUserId { get; private set; }
    public string? ResponsibleTrainerDisplayName { get; private set; }
    public IReadOnlyCollection<string> Capabilities => _capabilities.AsReadOnly();
    // Non-persistent flag: only a deliberate module edit may replace join-table rows.
    public bool CapabilitiesChanged { get; private set; }

    // Rehydrate the ignored DDD collection from its persisted join table.
    // This is not a domain mutation and must never trigger a rewrite.
    public void RestoreCapabilities(IEnumerable<string> values)
    {
        _capabilities.Clear();
        _capabilities.AddRange(values.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
        CapabilitiesChanged = false;
    }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static TrainingProgram Create(Guid familyId, string code, string name, string descriptionKey, string icon, int durationHours, IEnumerable<string> capabilities, string? externalKey = null, Guid? organizationId = null, Guid? responsibleTrainerAuthGateUserId = null, string? responsibleTrainerDisplayName = null)
    {
        if (durationHours <= 0)
            throw new DomainException("PROGRAM_DURATION_INVALID");
        var x = new TrainingProgram(Guid.NewGuid(), organizationId, familyId, ProgramCode.Create(code), name, descriptionKey, icon, durationHours, externalKey, responsibleTrainerAuthGateUserId, responsibleTrainerDisplayName);
        x.ReplaceCapabilities(capabilities);
        x.RaiseDomainEvent(new TrainingProgramCreatedDomainEvent(x.Id));
        return x;
    }

    // Backward-compatible overload: existing callers that do not edit the responsible trainer
    // keep the current assignment instead of being forced to pass the new fields.
    public void Update(Guid familyId, string name, string descriptionKey, string icon, int durationHours, ProgramStatus status, IEnumerable<string> capabilities)
        => Update(familyId, name, descriptionKey, icon, durationHours, status, capabilities, ResponsibleTrainerAuthGateUserId, ResponsibleTrainerDisplayName);

    public void Update(Guid familyId, string name, string descriptionKey, string icon, int durationHours, ProgramStatus status, IEnumerable<string> capabilities, Guid? responsibleTrainerAuthGateUserId, string? responsibleTrainerDisplayName)
    {
        if (durationHours <= 0)
            throw new DomainException("PROGRAM_DURATION_INVALID");
        FamilyId = familyId;
        Name = name.Trim();
        DescriptionKey = descriptionKey.Trim();
        Icon = icon.Trim();
        DurationHours = durationHours;
        // Editing identity and pedagogy never changes the administrative lifecycle.
        ResponsibleTrainerAuthGateUserId = responsibleTrainerAuthGateUserId;
        ResponsibleTrainerDisplayName = NormalizeOptional(responsibleTrainerDisplayName);
        ReplaceCapabilities(capabilities);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingProgramUpdatedDomainEvent(Id));
    }

    public void ChangeStatus(ProgramStatus target)
    {
        if (Status == target) return;
        if (!Enum.IsDefined(target) || target == ProgramStatus.Draft)
            throw new DomainException("PROGRAM_STATUS_TRANSITION_INVALID");
        if (target == ProgramStatus.Active && !ResponsibleTrainerAuthGateUserId.HasValue)
            throw new DomainException("PROGRAM_ACTIVATION_RESPONSIBLE_REQUIRED");
        if (target == ProgramStatus.Active && _capabilities.Count == 0)
            throw new DomainException("PROGRAM_CAPABILITIES_REQUIRED");
        Status = target;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingProgramUpdatedDomainEvent(Id));
    }

    public void AssignResponsibleTrainer(Guid authGateUserId, string displayName)
    {
        if (authGateUserId == Guid.Empty)
            throw new DomainException("PROGRAM_RESPONSIBLE_TRAINER_INVALID");

        ResponsibleTrainerAuthGateUserId = authGateUserId;
        ResponsibleTrainerDisplayName = NormalizeOptional(displayName);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingProgramUpdatedDomainEvent(Id));
    }

    public void MarkDeleted() => RaiseDomainEvent(new TrainingProgramDeletedDomainEvent(Id, OrganizationId));

    private static string? NormalizeOptional(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length == 0) return null;
        return text.Length <= 220 ? text : text[..220];
    }

    private void ReplaceCapabilities(IEnumerable<string> values)
    {
        _capabilities.Clear();
        _capabilities.AddRange(values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
        if (_capabilities.Count == 0)
            throw new DomainException("PROGRAM_CAPABILITIES_REQUIRED");
        CapabilitiesChanged = true;
    }
}
