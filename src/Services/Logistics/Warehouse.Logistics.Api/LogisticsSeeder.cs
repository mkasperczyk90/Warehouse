using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Api;

/// <summary>
/// Dev-only seed for the Logistics context: a handful of announced inbound deliveries and placed
/// outbound orders so the admin panel's Inbound/Outbound screens show real data the moment MSW is off.
/// Aggregates are built straight from their domain factories (no SKU-replica check), exactly as
/// <see cref="TopologySeeder"/> does — the seeded product codes are the same SKUs the Catalog seed
/// defines (WH01/WH02), so the lines resolve once the catalog replica catches up. Idempotent: it
/// no-ops once any delivery or order exists.
/// </summary>
internal static class LogisticsSeeder
{
    // Supplier/customer party-role labels (an opaque Party-role id in production; a free-text label here).
    private const string DairySupplier = "Dairy Co.";
    private const string PackagingSupplier = "Acme Packaging";
    private const string FreshMarket = "Fresh Market sp. z o.o.";
    private const string Bistro24 = "Bistro 24";

    public static async Task SeedAsync(LogisticsDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Deliveries.AnyAsync(cancellationToken) || await db.Orders.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        db.Deliveries.Add(InboundDelivery.Announce(
            new PartyRoleRef(DairySupplier), WarehouseRef.Of("WH01"), now.AddDays(1),
            [
                Line("MILK-1L", 240, "pcs"),
                Line("YOG-400", 120, "pcs"),
                Line("CHEESE-5KG", 30, "kg"),
            ]));

        db.Deliveries.Add(InboundDelivery.Announce(
            new PartyRoleRef(PackagingSupplier), WarehouseRef.Of("WH01"), now.AddDays(2),
            [Line("BOX-L", 500, "pcs")]));

        db.Deliveries.Add(InboundDelivery.Announce(
            new PartyRoleRef(DairySupplier), WarehouseRef.Of("WH02"), now.AddDays(1),
            [Line("BERRY-1KG", 80, "pcs")]));

        db.Orders.Add(OutboundOrder.Create(
            new PartyRoleRef(FreshMarket),
            Address.Of("Rynek 1", "Wrocław", "50-101", "PL"),
            WarehouseRef.Of("WH01"), now.AddDays(1),
            [OrderLine("MILK-1L", 48, "pcs"), OrderLine("YOG-400", 24, "pcs")]));

        db.Orders.Add(OutboundOrder.Create(
            new PartyRoleRef(Bistro24),
            Address.Of("Półwiejska 4", "Poznań", "61-001", "PL"),
            WarehouseRef.Of("WH02"), now.AddDays(2),
            [OrderLine("BERRY-1KG", 12, "pcs")]));

        // Dispatch board demo: packed orders, each with a shipment at a different lifecycle stage so the
        // board's columns (awaiting carrier → carrier assigned → pickup notice sent) are populated.
        var awaiting = PackedShipment("Hotel Vega", "WH01", "MILK-1L");

        var assigned = PackedShipment("Bistro 24", "WH01", "YOG-400");
        assigned.Shipment.AssignCarrier(new PartyRoleRef("DH"), "Tue 14:00");

        var noticeSent = PackedShipment("Poznań Catering", "WH02", "BERRY-1KG");
        noticeSent.Shipment.AssignCarrier(new PartyRoleRef("GL"), "Wed 09:00");
        noticeSent.Shipment.SendPickupNotice();

        foreach (var (order, shipment) in new[] { awaiting, assigned, noticeSent })
        {
            db.Orders.Add(order);
            db.Shipments.Add(shipment);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>A packed order with its shipment opened on the dispatch board (AwaitingCarrier).</summary>
    private static (OutboundOrder Order, Shipment Shipment) PackedShipment(string customer, string warehouse, string sku)
    {
        var order = OutboundOrder.Create(
            new PartyRoleRef(customer), Address.Of("ul. Dostawcza 1", "—", "00-000", "PL"),
            WarehouseRef.Of(warehouse), DateTimeOffset.UtcNow.AddDays(1), [OrderLine(sku, 24, "pcs")]);
        order.MarkReserved(fully: true);
        order.StartPicking();
        order.MarkPacked();

        var shipment = Shipment.CreateAwaitingCarrier(order.Id);
        shipment.AddPackage(Weight.FromKilograms(12), PackageDimensions.Of(60, 40, 40));
        return (order, shipment);
    }

    private static (ProductCode, Quantity, DeliveryPack?) Line(string sku, decimal qty, string unit) =>
        (ProductCode.Of(sku), Quantity.Of(qty, UnitOfMeasure.FromCode(unit)), null);

    private static (ProductCode, Quantity) OrderLine(string sku, decimal qty, string unit) =>
        (ProductCode.Of(sku), Quantity.Of(qty, UnitOfMeasure.FromCode(unit)));
}
