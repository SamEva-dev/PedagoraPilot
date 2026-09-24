namespace PedagoraPilot.Contracts.Catalog;
public sealed record ProgramFamilyDto(Guid Id, string Code, string Name, string Icon, bool Active);
public sealed record TrainingProgramDto(Guid Id, string Key, string Code, string Name, string FamilyCode, string Category, string Icon, string DescriptionKey, int DurationHours, string Status, IReadOnlyCollection<string> EnabledModules, IReadOnlyCollection<string> SiteKeys, string? ReferenceVersion);
public sealed record ProgramOfferingDto(Guid Id, string Key, Guid SiteId, string SiteKey, Guid ProgramId, string ProgramKey, bool Active);
public sealed record CreateProgramRequest(string FamilyCode, string Code, string Name, string DescriptionKey, string Icon, int DurationHours, string Status, IReadOnlyCollection<string> EnabledModules, string? ExternalKey);
public sealed record UpdateProgramRequest(string FamilyCode, string Name, string DescriptionKey, string Icon, int DurationHours, string Status, IReadOnlyCollection<string> EnabledModules);
public sealed record SetProgramOfferingRequest(bool Active);
public sealed record ReferentialVersionDto(Guid Id, string Key, Guid ReferentialId, Guid ProgramId, string ProgramKey, string Code, string Name, string Version, string? CertificationCode, string Status, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int TotalHours, int SheetCount, int RequiredDocumentCount, IReadOnlyCollection<string> EnabledModules, string? NotesKey);
public sealed record CreateReferentialVersionRequest(string Version, string? CertificationCode, DateOnly EffectiveFrom, int TotalHours, int SheetCount, int RequiredDocumentCount, IReadOnlyCollection<string> EnabledModules, string? NotesKey, string? ExternalKey);
