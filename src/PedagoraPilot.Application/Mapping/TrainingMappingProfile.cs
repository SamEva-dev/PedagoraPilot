using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Application.Mapping;
public sealed class TrainingMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<Cohort, CohortReadModel>();
        configuration.CreateMap<Person, PersonReadModel>();
        configuration.CreateMap<LearnerProfile, LearnerProfileReadModel>();
        configuration.CreateMap<Enrollment, EnrollmentReadModel>();
        configuration.CreateMap<TrainingSession, TrainingSessionReadModel>();
        configuration.CreateMap<AttendanceEntry, AttendanceEntryReadModel>();
    }
}

public sealed record CohortReadModel
{
    public CohortId Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid SiteId { get; init; }
    public Guid ProgramOfferingId { get; init; }
    public Guid ReferentialVersionId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int Capacity { get; init; }
    public CohortStatus Status { get; init; }
    public string? ExternalKey { get; init; }
}

public sealed record PersonReadModel
{
    public PersonId Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? ExternalKey { get; init; }
}

public sealed record LearnerProfileReadModel
{
    public LearnerProfileId Id { get; init; }
    public PersonId PersonId { get; init; }
    public Guid? AuthGateUserId { get; init; }
    public LearnerProfileStatus Status { get; init; }
    public string? ExternalKey { get; init; }
}

public sealed record EnrollmentReadModel
{
    public EnrollmentId Id { get; init; }
    public Guid OrganizationId { get; init; }
    public LearnerProfileId LearnerProfileId { get; init; }
    public CohortId CohortId { get; init; }
    public DateOnly EnrolledOn { get; init; }
    public DateOnly? EndedOn { get; init; }
    public EnrollmentStatus Status { get; init; }
    public string? ExternalKey { get; init; }
}

public sealed record TrainingSessionReadModel
{
    public TrainingSessionId Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid SiteId { get; init; }
    public CohortId CohortId { get; init; }
    public TrainingSessionType Type { get; init; }
    public TrainingSessionModality Modality { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    public string? TrainerAuthGateUserId { get; init; }
    public string? TrainerDisplayName { get; init; }
    public string? Location { get; init; }
    public string? Objective { get; init; }
    public string? Supports { get; init; }
    public string? Comments { get; init; }
    public TrainingSessionStatus Status { get; init; }
    public SessionAudienceMode AudienceMode { get; init; }
    public string? ExternalKey { get; init; }
    public int PlannedMinutes { get; init; }
}

public sealed record AttendanceEntryReadModel
{
    public AttendanceEntryId Id { get; init; }
    public EnrollmentId EnrollmentId { get; init; }
    public AttendanceStatus Status { get; init; }
    public DateTimeOffset? ArrivalAtUtc { get; init; }
    public DateTimeOffset? DepartureAtUtc { get; init; }
    public int ExpectedMinutes { get; init; }
    public int PresentMinutes { get; init; }
    public int MissedMinutes { get; init; }
    public int CatchupMinutes { get; init; }
    public bool AddToCatchup { get; init; }
    public string? Comment { get; init; }
}
