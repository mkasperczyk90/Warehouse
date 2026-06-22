using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Topology.Domain.Events;

/// <summary>Feeds the Inventory context's LocationSnapshot replica.</summary>
public sealed record LocationDefined(
    WarehouseCode Warehouse,
    RoomCode Room,
    LocationCode Location,
    LocationKind Kind,
    Volume Capacity,
    Weight MaxLoad,
    RoomEnvironment Environment,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Existing stock may have become incompatible — Inventory re-validates and reports.</summary>
public sealed record RoomEnvironmentChanged(
    WarehouseCode Warehouse,
    RoomCode Room,
    RoomEnvironment Environment,
    DateTimeOffset OccurredAt) : IDomainEvent;
