using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.IntegrationTests;

/// <summary>Domain factories for the persistence tests (built through the real aggregates, so the
/// round-trip also exercises the EF mappings of their value objects).</summary>
internal static class Sample
{
    public static OutboundOrder Order(decimal quantity = 10, string sku = "SKU-1", string warehouse = "WH01") =>
        OutboundOrder.Create(
            new PartyRoleRef(Guid.NewGuid()),
            Address.Of("Main 1", "Wrocław", "50-001", "PL"),
            WarehouseRef.Of(warehouse),
            DateTimeOffset.UtcNow.AddDays(2),
            [(ProductCode.Of(sku), Quantity.Of(quantity, UnitOfMeasure.Piece))]);

    public static InboundDelivery DeliveryWithSlot(string dockCode, DateTimeOffset from, DateTimeOffset to)
    {
        var delivery = InboundDelivery.Announce(
            new PartyRoleRef(Guid.NewGuid()),
            WarehouseRef.Of("WH01"),
            DateTimeOffset.UtcNow.AddDays(1),
            [(ProductCode.Of("SKU-1"), Quantity.Of(100, UnitOfMeasure.Piece), null)]);
        delivery.AssignDockSlot(DockSlot.Of(dockCode, from, to));
        return delivery;
    }
}
