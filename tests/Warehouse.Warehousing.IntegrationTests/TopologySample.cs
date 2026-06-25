using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.IntegrationTests;

/// <summary>Fully-formed warehouse aggregates for the Topology database tests.</summary>
internal static class TopologySample
{
    /// <summary>A cold-storage site: one cold room (2..6°C) holding one rack location, plus an inbound dock.</summary>
    public static WarehouseSite ColdSite(string code = "WAW1", string name = "Warsaw DC")
    {
        var site = WarehouseSite.Establish(
            WarehouseCode.Of(code),
            name,
            Address.Of("ul. Kolejowa 7", "Wrocław", "50-001", "PL"));

        site.AddRoom(RoomCode.Of("CHLD1"), RoomType.ColdRoom, RoomEnvironment.For(RoomType.ColdRoom, TemperatureRange.Of(2, 6)));
        site.AddLocation(
            RoomCode.Of("CHLD1"),
            LocationCode.Of($"{code}-CHLD1-A-01"),
            LocationKind.Rack,
            Volume.FromCubicMeters(1.5m),
            Weight.FromKilograms(500));
        site.AddDock(DockCode.Of("D01"), DockDirection.Inbound);

        return site;
    }

    /// <summary>An ambient site with a single standard room (no locations), for list/count assertions.</summary>
    public static WarehouseSite AmbientSite(string code = "POZ1", string name = "Poznań DC")
    {
        var site = WarehouseSite.Establish(
            WarehouseCode.Of(code),
            name,
            Address.Of("ul. Magazynowa 1", "Poznań", "60-001", "PL"));

        site.AddRoom(RoomCode.Of("STD1"), RoomType.Standard);
        return site;
    }
}
