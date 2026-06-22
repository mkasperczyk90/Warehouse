namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// The dock buffer is where a goods receipt first lands (UC-02): a virtual staging location per
/// warehouse, addressed <c>{WAREHOUSE}-DOCK-BUFFER</c>. Put-away (UC-04) later moves the stock out
/// of it into real storage locations. Centralised here so receipt and put-away agree on the address.
/// </summary>
public static class DockBuffer
{
    public static LocationCode For(WarehouseCode warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        return LocationCode.Of($"{warehouse.Value}-DOCK-BUFFER");
    }
}
