using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain.Services;

/// <summary>
/// A location-to-location move touches two StockItems but is ONE physical fact —
/// so it must produce exactly one ledger entry. This service is the only way to
/// move stock between locations (put-away, internal moves); StockItem's transfer
/// halves are internal.
/// </summary>
public static class StockTransferService
{
    private static readonly MovementType[] TransferTypes = [MovementType.PutAway, MovementType.Move];

    public static StockMovement Transfer(
        StockItem source,
        StockItem target,
        Quantity quantity,
        MovementType type,
        string performedBy,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(quantity);

        if (!TransferTypes.Contains(type))
        {
            throw new DomainException(
                "movement_type_invalid",
                $"Transfer accepts only {string.Join('/', TransferTypes)}, got {type}.");
        }

        if (source.Sku != target.Sku)
        {
            throw new DomainException(
                "transfer_sku_mismatch",
                $"Cannot transfer between stock items of different SKUs: {source.Sku} and {target.Sku}.");
        }

        if (source.Batch != target.Batch)
        {
            throw new DomainException(
                "transfer_batch_mismatch",
                $"Cannot transfer between different batches: {source.Batch} and {target.Batch}.");
        }

        if (source.Location == target.Location)
        {
            throw new DomainException(
                "transfer_same_location",
                $"Source and target are the same location: {source.Location}.");
        }

        source.TransferOut(quantity);
        target.TransferIn(quantity);
        return StockMovement.Record(
            type, source.Sku, source.Batch, source.Location, target.Location, quantity, performedBy, reason);
    }
}
