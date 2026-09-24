using DomainRelay.EFCore.DomainEvents;

namespace PedagoraPilot.Domain.Common;
/// <summary>
/// Compatibility aggregate root for the aggregates created before the strongly-typed-id convention.
/// New aggregates must derive from AggregateRoot&lt;TId&gt;.
/// </summary>
public abstract class AggregateRoot : Entity, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];
    protected AggregateRoot(Guid id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Aggregate root with a strongly typed identifier. This is the required base type for all new aggregates.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : struct, IIdentifier
{
    private readonly List<IDomainEvent> _domainEvents = [];
    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
