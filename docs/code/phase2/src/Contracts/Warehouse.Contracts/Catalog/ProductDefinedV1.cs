namespace Warehouse.Contracts.Catalog;

/// <summary>
/// Integration event: a product was added to the catalog. Published through the transactional
/// outbox after the <c>ProductType</c> is committed, and consumed by other services (Inventory
/// seeds a <c>ProductSnapshot</c> from it).
///
/// <b>Additive-only.</b> This is the V1 wire shape and must never change — consumers in other
/// deployments bind to it. A new field ships as <c>ProductDefinedV2</c>, never as an edit here.
/// Primitives only: the contract carries no domain types, so every service can reference it safely.
/// </summary>
public sealed record ProductDefinedV1(
    string Sku,
    string Name,
    string BaseUnit,
    bool RequiresColdChain,
    bool IsHazardous,
    bool IsBatchTracked,
    DateTimeOffset OccurredAt);
