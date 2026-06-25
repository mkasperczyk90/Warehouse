namespace Warehouse.Contracts.Topology;

/// <summary>
/// Integration event: a room's maintained environment was re-tuned. Published through the transactional
/// outbox after the change is committed; Inventory refreshes the environment of every
/// <c>LocationSnapshot</c> in that room so put-away re-validates against the new range. Changing a room
/// never moves goods — incompatibilities are surfaced for a human to resolve (UC-14).
///
/// <b>Additive-only.</b> This is the V1 wire shape and must never change — a new field ships as
/// <c>RoomEnvironmentChangedV2</c>, never as an edit here. Primitives only.
/// </summary>
public sealed record RoomEnvironmentChangedV1(
    string Warehouse,
    string Room,
    decimal MinCelsius,
    decimal MaxCelsius,
    DateTimeOffset OccurredAt);
