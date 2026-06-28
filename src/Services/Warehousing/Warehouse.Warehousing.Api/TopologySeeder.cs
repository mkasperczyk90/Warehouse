using Microsoft.EntityFrameworkCore;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// Dev-only seed for the Topology context: two warehouse sites (Wrocław, Poznań) with the rooms,
/// locations and docks the admin panel demonstrates. Idempotent — it no-ops once any site exists. The
/// warehouse codes here are the identity the desk sends as <c>X-Warehouse-Id</c>, so they line up with
/// the Inventory seed (see <see cref="InventorySeeder"/>).
/// </summary>
internal static class TopologySeeder
{
    public static async Task SeedAsync(TopologyDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Warehouses.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Warehouses.Add(Wroclaw());
        db.Warehouses.Add(Poznan());
        await db.SaveChangesAsync(cancellationToken);
    }

    private static WarehouseSite Wroclaw()
    {
        var code = WarehouseCode.Of("WH01");
        var site = WarehouseSite.Establish(code, "Wrocław DC", Address.Of("Powstańców 12", "Wrocław", "50-001", "PL"));

        site.AddRoom(RoomCode.Of("CR1"), RoomType.ColdRoom, RoomEnvironment.For(RoomType.ColdRoom, TemperatureRange.Of(2, 6)));
        site.AddRoom(RoomCode.Of("STD"), RoomType.Standard);
        site.AddRoom(RoomCode.Of("FZ1"), RoomType.Freezer);
        site.AddRoom(RoomCode.Of("QC"), RoomType.Standard);
        site.AddRoom(RoomCode.Of("HZ"), RoomType.HazmatZone);

        AddLocation(site, "CR1", "WH01-CR1-A03-R2-S1");
        AddLocation(site, "CR1", "WH01-CR1-A01-R1-S4");
        AddLocation(site, "CR1", "WH01-CR1-PICKFACE-12"); // cold pick face — replenishment target (UC-06)
        AddLocation(site, "STD", "WH01-STD-A07-R3-S2");
        AddLocation(site, "FZ1", "WH01-FZ1-B02-R4-S1");
        AddLocation(site, "QC", "WH01-QC-HOLD-02");

        site.AddDock(DockCode.Of("D01"), DockDirection.Inbound);
        site.AddDock(DockCode.Of("D02"), DockDirection.Outbound);
        return site;
    }

    private static WarehouseSite Poznan()
    {
        var code = WarehouseCode.Of("WH02");
        var site = WarehouseSite.Establish(code, "Poznań DC", Address.Of("Półwiejska 4", "Poznań", "61-001", "PL"));

        site.AddRoom(RoomCode.Of("CR1"), RoomType.ColdRoom, RoomEnvironment.For(RoomType.ColdRoom, TemperatureRange.Of(2, 6)));
        site.AddRoom(RoomCode.Of("FZ1"), RoomType.Freezer);

        AddLocation(site, "CR1", "WH02-CR1-A01-R1-S1");
        AddLocation(site, "FZ1", "WH02-FZ1-B01-R2-S3");

        site.AddDock(DockCode.Of("PZ1"), DockDirection.Both);
        return site;
    }

    private static void AddLocation(WarehouseSite site, string room, string code) =>
        site.AddLocation(
            RoomCode.Of(room), LocationCode.Of(code), LocationKind.Rack,
            Volume.FromCubicMeters(1.5m), Weight.FromKilograms(600m));
}
