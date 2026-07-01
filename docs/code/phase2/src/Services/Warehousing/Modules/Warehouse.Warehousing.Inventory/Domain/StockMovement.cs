using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): one immutable entry of the stock ledger.
/// Append-only by design — a mistake is corrected with a reversing movement, never an
/// edit. Current stock is a projection of movements; the table forbids UPDATE/DELETE.
/// </summary>
public sealed class StockMovement
{
    private StockMovement(
        MovementId id,
        MovementType type,
        Sku sku,
        BatchNumber? batch,
        LocationCode? from,
        LocationCode? to,
        Quantity quantity,
        string performedBy,
        string? reason,
        DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Sku = sku;
        Batch = batch;
        From = from;
        To = to;
        Quantity = quantity;
        PerformedBy = performedBy;
        Reason = reason;
        OccurredAt = occurredAt;
    }

    private StockMovement()
    {
    }

    public MovementId Id { get; }

    public MovementType Type { get; }

    public Sku Sku { get; } = null!;

    public BatchNumber? Batch { get; }

    /// <summary>Source location; null for movements entering the warehouse (receipt, transfer-in).</summary>
    public LocationCode? From { get; }

    /// <summary>Target location; null for movements leaving the warehouse (dispatch, transfer-out).</summary>
    public LocationCode? To { get; }

    public Quantity Quantity { get; } = null!;

    public string PerformedBy { get; } = null!;

    public string? Reason { get; }

    public DateTimeOffset OccurredAt { get; }

    public static StockMovement Record(
        MovementType type,
        Sku sku,
        BatchNumber? batch,
        LocationCode? from,
        LocationCode? to,
        Quantity quantity,
        string performedBy,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentException.ThrowIfNullOrWhiteSpace(performedBy);

        if (quantity.IsZero)
        {
            throw new DomainException("movement_quantity_zero", "A stock movement must move a positive quantity.");
        }

        if (from is null && to is null)
        {
            throw new DomainException("movement_locations_missing", "A stock movement needs a source or a target location.");
        }

        return new StockMovement(
            MovementId.New(), type, sku, batch, from, to, quantity, performedBy, reason, DateTimeOffset.UtcNow);
    }
}
