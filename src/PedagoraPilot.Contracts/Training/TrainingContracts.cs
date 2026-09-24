namespace PedagoraPilot.Contracts.Training;
public sealed record CohortDto(Guid Id, string Key, Guid OrganizationId, Guid SiteId, Guid ProgramOfferingId, Guid ReferentialVersionId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, int LearnerCount, string Status);
public sealed record CreateCohortRequest(Guid ProgramOfferingId, Guid ReferentialVersionId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, string? ExternalKey);
public sealed record UpdateCohortRequest(string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, string Status);
public sealed record LearnerDto(Guid EnrollmentId, Guid LearnerProfileId, Guid PersonId, Guid CohortId, string FirstName, string LastName, string DisplayName, string Email, string? Phone, DateOnly? BirthDate, string EnrollmentStatus, DateOnly EnrolledOn, string? ExternalKey);
public sealed record EnrollLearnerRequest(string FirstName, string LastName, string Email, string? Phone, DateOnly? BirthDate, DateOnly? EnrolledOn, Guid? AuthGateUserId, string? PersonExternalKey, string? LearnerExternalKey, string? EnrollmentExternalKey);
public sealed record ChangeEnrollmentStatusRequest(string Status, DateOnly? EndedOn);
