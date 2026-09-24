namespace PedagoraPilot.Domain.Common;
public abstract class Entity
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(id));
        Id = id;
    }

    protected Entity()
    {
        Id = Guid.Empty;
    }

    public Guid Id { get; protected init; }
}

public abstract class Entity<TId>
    where TId : struct, IIdentifier
{
    protected Entity(TId id)
    {
        if (id.IsEmpty)
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(id));
        Id = id;
    }

    protected Entity()
    {
        Id = default;
    }

    public TId Id { get; protected init; }
}
