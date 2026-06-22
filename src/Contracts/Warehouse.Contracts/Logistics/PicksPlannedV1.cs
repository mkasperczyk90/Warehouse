namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: Inventory hard-allocated stock for a released order and planned the concrete
/// picks (the reply to <see cref="PickingReleasedV1"/>). Consumed by Logistics to build the routed
/// <c>PickList</c> (UC-10).
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>PicksPlannedV2</c>. Primitives only.
/// </summary>
public sealed record PicksPlannedV1(
    Guid OrderId,
    IReadOnlyList<PlannedPickV1> Picks,
    DateTimeOffset OccurredAt);

/// <summary>One planned pick: a quantity of a SKU/batch to take from a concrete location.</summary>
public sealed record PlannedPickV1(
    string Location,
    string Sku,
    string? BatchNumber,
    decimal Quantity,
    string Unit);
