using DomainRelay.Abstractions;
using DomainRelay.EFCore.DomainEvents;

namespace PedagoraPilot.Domain.Common;
public abstract record DomainEvent : IDomainEvent, INotification
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid EventId { get; init; }
    public DateTime OccurredOnUtc { get; init; }
}
