using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Workforce;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Workforce;
public sealed record CreateRemoteWorkCommand(Guid SiteId, DateOnly Date, string Period, TimeOnly? StartTime, TimeOnly? EndTime, string? Comment, IReadOnlyCollection<RemoteWorkActivityInput>? Activities) : ICommand<RemoteWorkRequestDto>;
public sealed record DecideRemoteWorkCommand(RemoteWorkRequestId RequestId, bool Approved) : ICommand<RemoteWorkRequestDto>;
public sealed record UpdateRemoteWorkActivityCommand(RemoteWorkRequestId RequestId, RemoteWorkActivityId ActivityId, string Status) : ICommand<RemoteWorkRequestDto>;
