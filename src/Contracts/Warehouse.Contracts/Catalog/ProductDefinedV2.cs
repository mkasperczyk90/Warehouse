namespace Warehouse.Contracts.Catalog;

/// <summary>
/// Integration event: a product was added to the catalog — V2. A superset of
/// <see cref="ProductDefinedV1"/> that additionally carries the unit footprint (weight + volume) and the
/// required temperature range, so Inventory can enforce the hard storage-compatibility invariant
/// (cold chain + capacity) at put-away from its local replica alone (ADR-0003). <see cref="MinCelsius"/>
/// /<see cref="MaxCelsius"/> are <c>null</c> when the product has no temperature requirement (ambient).
///
/// <b>Additive-only.</b> V2 was added, never edited into V1: existing V1 consumers (e.g. Logistics' SKU
/// validation) keep binding to their shape. A further field would ship as <c>ProductDefinedV3</c>.
/// Primitives only: the contract carries no domain types.
/// </summary>
public sealed record ProductDefinedV2(
    string Sku,
    string Name,
    string BaseUnit,
    bool RequiresColdChain,
    bool IsHazardous,
    bool IsBatchTracked,
    decimal UnitWeightKg,
    decimal UnitVolumeM3,
    decimal? MinCelsius,
    decimal? MaxCelsius,
    DateTimeOffset OccurredAt);
