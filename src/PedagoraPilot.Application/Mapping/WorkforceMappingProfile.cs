using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workforce;

namespace PedagoraPilot.Application.Mapping;
public sealed class WorkforceMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<RemoteWorkRequest, RemoteWorkRequestReadModel>();
        configuration.CreateMap<RemoteWorkActivity, RemoteWorkActivityReadModel>();
    }
}

public sealed class RemoteWorkRequestReadModel
{
    public RemoteWorkRequestId Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SiteId { get; set; }
    public Guid AuthGateUserId { get; set; }
    public string UserDisplayName { get; set; } = "";
    public string? UserEmail { get; set; }
    public DateOnly Date { get; set; }
    public RemoteWorkPeriod Period { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public RemoteWorkRequestStatus Status { get; set; }
    public string? Comment { get; set; }
    public Guid? ApproverUserId { get; set; }
    public string? ApproverDisplayName { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
}

public sealed class RemoteWorkActivityReadModel
{
    public RemoteWorkActivityId Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public RemoteWorkActivityStatus Status { get; set; }
}
