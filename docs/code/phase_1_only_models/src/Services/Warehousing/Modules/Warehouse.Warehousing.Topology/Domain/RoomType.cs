namespace Warehouse.Warehousing.Topology.Domain;

/// <summary>Description archetype (🟦): what kind of room this is.</summary>
public enum RoomType
{
    Standard = 0,
    ColdRoom = 1,
    Freezer = 2,
    HazmatZone = 3,
}

public enum LocationKind
{
    Rack = 0,
    Floor = 1,

    /// <summary>Buffer next to a dock — goods land here on receipt, before put-away.</summary>
    DockBuffer = 2,
}

public enum DockDirection
{
    Inbound = 0,
    Outbound = 1,
    Both = 2,
}
