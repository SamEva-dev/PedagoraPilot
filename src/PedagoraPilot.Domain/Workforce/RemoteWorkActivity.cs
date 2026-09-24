using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workforce;
public sealed class RemoteWorkActivity : Entity<RemoteWorkActivityId>
{
    private RemoteWorkActivity()
    {
    }

    internal RemoteWorkActivity(RemoteWorkActivityId id, RemoteWorkRequestId requestId, string code, string label) : base(id)
    {
        RequestId = requestId;
        Code = string.IsNullOrWhiteSpace(code) ? throw new DomainException("REMOTE_WORK_ACTIVITY_CODE_REQUIRED") : code.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? throw new DomainException("REMOTE_WORK_ACTIVITY_LABEL_REQUIRED") : label.Trim();
        Status = RemoteWorkActivityStatus.Todo;
    }

    public RemoteWorkRequestId RequestId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public RemoteWorkActivityStatus Status { get; private set; }

    internal void SetStatus(RemoteWorkActivityStatus status) => Status = status;
}
