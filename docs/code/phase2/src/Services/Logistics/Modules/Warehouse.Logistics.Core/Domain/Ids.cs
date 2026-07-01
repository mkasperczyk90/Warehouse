namespace Warehouse.Logistics.Core.Domain;

public readonly record struct DeliveryId(Guid Value)
{
    public static DeliveryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct PickListId(Guid Value)
{
    public static PickListId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct ShipmentId(Guid Value)
{
    public static ShipmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
