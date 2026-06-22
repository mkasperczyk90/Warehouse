namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: an operator confirmed a pick task (UC-10). Published by Logistics; consumed by
/// Inventory to consume the hard allocation and deduct the picked quantity from the ledger.
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>PickConfirmedV2</c>. Primitives only.
/// </summary>
public sealed record PickConfirmedV1(
    Guid OrderId,
    int Sequence,
    string Location,
    string Sku,
    string? BatchNumber,
    decimal Quantity,
    string Unit,
    DateTimeOffset OccurredAt);
