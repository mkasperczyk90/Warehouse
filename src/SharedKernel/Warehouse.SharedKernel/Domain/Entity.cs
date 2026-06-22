namespace Warehouse.SharedKernel.Domain;

/// <summary>
/// Base class for entities: identity-based equality. <typeparamref name="TId"/> is expected
/// to be a strongly-typed id (readonly record struct) defined by the owning module.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>EF Core materialization constructor.</summary>
    protected Entity() => Id = default!;

    public TId Id { get; protected set; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && (ReferenceEquals(this, other) ||
                              (other.GetType() == GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id)));

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
