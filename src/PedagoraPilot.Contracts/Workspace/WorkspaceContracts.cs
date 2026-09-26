using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Contracts.Training;

namespace PedagoraPilot.Contracts.Workspace;
public sealed record WorkspaceOrganizationDto(Guid Id, string Key, string Code, string Name, string ShortName, string City, bool Active, string PrimaryColor, string SecondaryColor);
public sealed record WorkspaceSiteDto(Guid Id, string Key, Guid OrganizationId, string OrganizationKey, string Code, string Name, string City, string Address, string PostalCode, string Phone, string Email, string Manager, string Status, bool Active);
public sealed record WorkspaceSelectionDto(Guid OrganizationId, Guid? SiteId, Guid? ProgramId = null, Guid? CohortId = null);
public sealed record WorkspaceBootstrapDto(IReadOnlyCollection<WorkspaceOrganizationDto> Organizations, IReadOnlyCollection<WorkspaceSiteDto> Sites, IReadOnlyCollection<TrainingProgramDto> Programs, IReadOnlyCollection<ProgramOfferingDto> Offerings, IReadOnlyCollection<CohortDto> Cohorts, WorkspaceSelectionDto? DefaultSelection);
public sealed record CreateTrainingSiteRequest(string Code, string Name, string City, string Address, string PostalCode, string Phone, string Email, string Manager, string Status, string? ExternalKey);
public sealed record UpdateTrainingSiteRequest(string Code, string Name, string City, string Address, string PostalCode, string Phone, string Email, string Manager, string Status);
