namespace PedagoraPilot.Contracts.Workforce;
public sealed record RemoteWorkActivityDto(Guid Id, string Code, string Label, string Status);
public sealed record RemoteWorkRequestDto(Guid Id, Guid OrganizationId, Guid SiteId, Guid AuthGateUserId, string UserDisplayName, string? UserEmail, DateOnly Date, string Period, TimeOnly? StartTime, TimeOnly? EndTime, string Status, string? Comment, Guid? ApproverUserId, string? ApproverDisplayName, DateTimeOffset? DecidedAtUtc, IReadOnlyCollection<RemoteWorkActivityDto> Activities);
public sealed record CreateRemoteWorkRequest(Guid SiteId, DateOnly Date, string Period, TimeOnly? StartTime, TimeOnly? EndTime, string? Comment, IReadOnlyCollection<RemoteWorkActivityInput>? Activities);
public sealed record RemoteWorkActivityInput(string Code, string Label);
public sealed record RecordRemoteWorkDecisionRequest(bool Approved);
public sealed record UpdateRemoteWorkActivityRequest(string Status);
public sealed record RemoteWorkPolicyDto(bool Enabled, bool ApprovalRequired, int MaxDaysPerWeek, bool HalfDayAllowed, bool EndOfDayReport);

