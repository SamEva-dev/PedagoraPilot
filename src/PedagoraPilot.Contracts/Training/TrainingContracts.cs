namespace PedagoraPilot.Contracts.Training;
public sealed record CohortDto(Guid Id, string Key, Guid OrganizationId, Guid SiteId, Guid ProgramOfferingId, Guid ReferentialVersionId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, int LearnerCount, Guid? PedagogicalManagerAuthGateUserId, string PedagogicalManagerDisplayName, string Status);
public sealed record CreateCohortRequest(Guid ProgramOfferingId, Guid ReferentialVersionId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, Guid? PedagogicalManagerAuthGateUserId, string? PedagogicalManagerDisplayName, string? ExternalKey, string? InitialStatus = null);
public sealed record ReassignCohortManagerRequest(Guid AuthGateUserId);
public sealed record UpdateCohortRequest(string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, string Status);
public sealed record LearnerDirectoryDto(Guid LearnerProfileId, Guid PersonId, Guid? AuthGateUserId, string FirstName, string LastName, string DisplayName, string Email, string? Phone, DateOnly? BirthDate, Guid? EnrollmentId, Guid? CohortId, string? CohortName, string? EnrollmentStatus, DateOnly? EnrolledOn);
public sealed record LearnerDto(Guid EnrollmentId, Guid LearnerProfileId, Guid PersonId, Guid CohortId, string FirstName, string LastName, string DisplayName, string Email, string? Phone, DateOnly? BirthDate, string EnrollmentStatus, DateOnly EnrolledOn, string? ExternalKey);
public sealed record EnrollLearnerRequest(string FirstName, string LastName, string Email, string? Phone, DateOnly? BirthDate, DateOnly? EnrolledOn, Guid? AuthGateUserId, string? PersonExternalKey, string? LearnerExternalKey, string? EnrollmentExternalKey);
public sealed record ChangeEnrollmentStatusRequest(string Status, DateOnly? EndedOn);
