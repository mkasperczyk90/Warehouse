namespace Warehouse.Warehousing.Inventory.Domain;

public readonly record struct StockItemId(Guid Value)
{
    public static StockItemId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

/// <summary>Identity of a soft reservation (SKU-level, created at order time).</summary>
public readonly record struct StockReservationId(Guid Value)
{
    public static StockReservationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

/// <summary>Identity of a hard allocation (pinned to a StockItem, created at wave/pick time).</summary>
public readonly record struct AllocationId(Guid Value)
{
    public static AllocationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct MovementId(Guid Value)
{
    public static MovementId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct StocktakeId(Guid Value)
{
    public static StocktakeId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct BatchId(Guid Value)
{
    public static BatchId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct HandlingUnitId(Guid Value)
{
    public static HandlingUnitId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
