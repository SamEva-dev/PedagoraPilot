using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workforce.Events;

namespace PedagoraPilot.Domain.Workforce;
public sealed class RemoteWorkRequest : AggregateRoot<RemoteWorkRequestId>
{
    private readonly List<RemoteWorkActivity> _activities = [];
    private RemoteWorkRequest()
    {
    }

    private RemoteWorkRequest(RemoteWorkRequestId id, Guid organizationId, Guid siteId, Guid authGateUserId, string userDisplayName, string? userEmail, DateOnly date, RemoteWorkPeriod period, TimeOnly? startTime, TimeOnly? endTime, string? comment) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("REMOTE_WORK_ORGANIZATION_REQUIRED");
        if (siteId == Guid.Empty)
            throw new DomainException("REMOTE_WORK_SITE_REQUIRED");
        if (authGateUserId == Guid.Empty)
            throw new DomainException("REMOTE_WORK_USER_REQUIRED");
        if (period == RemoteWorkPeriod.Custom && (!startTime.HasValue || !endTime.HasValue || endTime <= startTime))
            throw new DomainException("REMOTE_WORK_TIME_RANGE_INVALID");
        OrganizationId = organizationId;
        SiteId = siteId;
        AuthGateUserId = authGateUserId;
        UserDisplayName = Req(userDisplayName, "REMOTE_WORK_USER_NAME_REQUIRED", 200);
        UserEmail = Opt(userEmail, 250);
        Date = date;
        Period = period;
        StartTime = startTime;
        EndTime = endTime;
        Comment = Opt(comment, 2000);
        Status = RemoteWorkRequestStatus.Requested;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid AuthGateUserId { get; private set; }
    public string UserDisplayName { get; private set; } = string.Empty;
    public string? UserEmail { get; private set; }
    public DateOnly Date { get; private set; }
    public RemoteWorkPeriod Period { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public RemoteWorkRequestStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public Guid? ApproverUserId { get; private set; }
    public string? ApproverDisplayName { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<RemoteWorkActivity> Activities => _activities.AsReadOnly();

    public static RemoteWorkRequest Create(Guid organizationId, Guid siteId, Guid authGateUserId, string userDisplayName, string? userEmail, DateOnly date, RemoteWorkPeriod period, TimeOnly? startTime, TimeOnly? endTime, string? comment, IEnumerable<(string Code, string Label)> activities)
    {
        var x = new RemoteWorkRequest(RemoteWorkRequestId.New(), organizationId, siteId, authGateUserId, userDisplayName, userEmail, date, period, startTime, endTime, comment);
        foreach (var a in activities)
            x.AddActivity(a.Code, a.Label);
        x.RaiseDomainEvent(new RemoteWorkRequestedDomainEvent(x.Id, organizationId, siteId, authGateUserId, x.UserEmail));
        return x;
    }

    public void Approve(Guid approverUserId, string approverDisplayName)
    {
        EnsurePending();
        if (approverUserId == Guid.Empty)
            throw new DomainException("REMOTE_WORK_APPROVER_REQUIRED");
        Status = RemoteWorkRequestStatus.Approved;
        ApproverUserId = approverUserId;
        ApproverDisplayName = Req(approverDisplayName, "REMOTE_WORK_APPROVER_NAME_REQUIRED", 200);
        DecidedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new RemoteWorkDecisionRecordedDomainEvent(Id, OrganizationId, Status, UserEmail));
    }

    public void Reject(Guid approverUserId, string approverDisplayName)
    {
        EnsurePending();
        Status = RemoteWorkRequestStatus.Rejected;
        ApproverUserId = approverUserId;
        ApproverDisplayName = Req(approverDisplayName, "REMOTE_WORK_APPROVER_NAME_REQUIRED", 200);
        DecidedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new RemoteWorkDecisionRecordedDomainEvent(Id, OrganizationId, Status, UserEmail));
    }

    public void Complete()
    {
        if (Status != RemoteWorkRequestStatus.Approved)
            throw new DomainException("REMOTE_WORK_NOT_APPROVED");
        Status = RemoteWorkRequestStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActivity(RemoteWorkActivityId activityId, RemoteWorkActivityStatus status)
    {
        if (Status is RemoteWorkRequestStatus.Rejected or RemoteWorkRequestStatus.Cancelled)
            throw new DomainException("REMOTE_WORK_REQUEST_LOCKED");
        var a = _activities.SingleOrDefault(x => x.Id == activityId) ?? throw new DomainException("REMOTE_WORK_ACTIVITY_NOT_FOUND");
        a.SetStatus(status);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new RemoteWorkActivityUpdatedDomainEvent(Id, OrganizationId, activityId, status));
    }

    private void AddActivity(string code, string label)
    {
        if (_activities.Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            return;
        _activities.Add(new(RemoteWorkActivityId.New(), Id, code, label));
    }

    private void EnsurePending()
    {
        if (Status != RemoteWorkRequestStatus.Requested)
            throw new DomainException("REMOTE_WORK_REQUEST_ALREADY_DECIDED");
    }

    private static string Req(string? v, string k, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(k);
        var x = v.Trim();
        if (x.Length > max)
            throw new DomainException(k);
        return x;
    }

    private static string? Opt(string? v, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            return null;
        var x = v.Trim();
        return x.Length <= max ? x : x[..max];
    }
}
