namespace Warehouse.Contracts.Topology;

/// <summary>
/// Integration event: a storage location was added to the warehouse topology. Published through the
/// transactional outbox after the <c>WarehouseSite</c> is committed, and consumed by Inventory, which
/// keeps a local <c>LocationSnapshot</c> (capacity + environment) to validate put-away without a
/// cross-service query (ADR-0003).
///
/// <b>Additive-only.</b> This is the V1 wire shape and must never change — consumers in other
/// deployments bind to it. A new field ships as <c>LocationDefinedV2</c>, never as an edit here.
/// Primitives only: the contract carries no domain types, so every service can reference it safely.
/// </summary>
public sealed record LocationDefinedV1(
    string Warehouse,
    string Room,
    string Location,
    string Kind,
    decimal CapacityM3,
    decimal MaxLoadKg,
    decimal MinCelsius,
    decimal MaxCelsius,
    bool IsHazmatZone,
    DateTimeOffset OccurredAt);
