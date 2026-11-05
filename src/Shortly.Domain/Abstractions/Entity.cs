using System.Runtime.CompilerServices;

namespace Shortly.Domain.Abstractions;

/// <summary>
/// Base class for entities identified by a <see cref="Guid"/>. Two entities are equal when they are of the same type and
/// share the same identity. An entity that has no identity yet (an empty id) is only equal to itself.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    protected Entity() { }

    public Guid Id { get; init; }

    /// <summary>True while the entity has no identity of its own.</summary>
    private bool IsTransient => Id == Guid.Empty;

    public bool Equals(Entity? other) =>
        other is not null
        && (ReferenceEquals(this, other) || (GetType() == other.GetType() && !IsTransient && Id == other.Id));

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() =>
        IsTransient ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}

/// <summary>
/// Base class for entities identified by a key of type <typeparamref name="TId"/> (for example a database-generated
/// <see cref="long"/>). Equality follows the same rules as <see cref="Entity"/>: same type and same identity, and an entity
/// whose key is still the default value (not saved yet) is only equal to itself.
/// Note: the hash code of an unsaved entity differs from its hash code after it was saved, so do not keep unsaved
/// entities in hash-based collections across a save.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
{
    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity() { }

    public TId Id { get; protected set; } = default!;

    private bool IsTransient => EqualityComparer<TId>.Default.Equals(Id, default!);

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && (ReferenceEquals(this, other)
            || (GetType() == other.GetType() && !IsTransient && EqualityComparer<TId>.Default.Equals(Id, other.Id)));

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() =>
        IsTransient ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
