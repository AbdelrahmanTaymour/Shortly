namespace Shortly.Domain.Abstractions;

public abstract class Entity
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    protected Entity() { }

    public Guid Id { get; init; }
}

public abstract class Entity<TId>
{
    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity() { }

    public TId Id { get; set; }
}