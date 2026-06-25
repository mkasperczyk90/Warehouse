using Warehouse.Contracts.Logistics;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Services;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Inventory.Application.ConfirmPutAway;

/// <summary>
/// UC-04 — the operator scanned a buffer item and a target location: move the stock out of the dock
/// buffer into storage (one <c>PutAway</c> ledger entry). When the buffer is emptied, reply to
/// Logistics with <see cref="PutAwayCompletedV1"/> so the delivery completes.
/// </summary>
public sealed record ConfirmPutAwayCommand(
    Guid DeliveryId,
    string WarehouseCode,
    string Sku,
    string? BatchNumber,
    decimal Quantity,
    string Unit,
    string ToLocation,
    string PerformedBy);

public sealed class ConfirmPutAwayHandler(
    IStockItemRepository stockItems,
    IProductSnapshotRepository products,
    ILocationSnapshotRepository locations,
    IStockLedger ledger,
    IDbContextOutbox<InventoryDbContext> outbox)
{
    public async Task HandleAsync(ConfirmPutAwayCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var buffer = DockBuffer.For(WarehouseCode.Of(command.WarehouseCode));
        var sku = Sku.Of(command.Sku);
        var batch = command.BatchNumber is { } number ? BatchNumber.Of(number) : null;
        var unit = UnitOfMeasure.FromCode(command.Unit);
        var quantity = Quantity.Of(command.Quantity, unit);
        var target = LocationCode.Of(command.ToLocation);

        // Load the whole buffer (tracked) so we can both find the source and tell when it is emptied.
        var bufferItems = await stockItems.ListAtAsync(buffer, cancellationToken);
        var source = bufferItems.FirstOrDefault(i =>
                         i.Sku == sku && (batch is null ? i.Batch is null : i.Batch == batch))
            ?? throw new DomainException(
                "put_away_no_buffer_stock",
                $"No {sku} {(batch is null ? string.Empty : batch + " ")}in the dock buffer of {command.WarehouseCode}.");

        await EnsureCompatibleAsync(sku, target, quantity, cancellationToken);

        var destination = await stockItems.GetAtAsync(sku, batch, target, cancellationToken);
        if (destination is null)
        {
            destination = StockItem.CreateAt(target, sku, batch, unit);
            stockItems.Add(destination);
        }

        var movement = StockTransferService.Transfer(
            source, destination, quantity, MovementType.PutAway,
            command.PerformedBy, reason: $"Delivery {command.DeliveryId}");
        ledger.Append(movement);

        // Heuristic completion: when nothing is left in the buffer, the delivery is fully put away.
        // (Per-delivery tracking of buffer stock is deferred; the dev/e2e flow handles one delivery.)
        if (bufferItems.All(i => i.OnHand.IsZero))
        {
            await outbox.PublishAsync(new PutAwayCompletedV1(command.DeliveryId, DateTimeOffset.UtcNow));
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }

    /// <summary>
    /// Enforces the hard storage-compatibility invariant (temperature / hazmat / capacity) before the
    /// stock moves, reading Topology's <c>LocationSnapshot</c> and the Catalog's <c>ProductSnapshot</c>
    /// replicas — no cross-service query (ADR-0003). A location Topology has not announced cannot be a
    /// put-away target; an unknown product cannot be validated, so both are rejected rather than waved
    /// through.
    /// </summary>
    private async Task EnsureCompatibleAsync(
        Sku sku, LocationCode target, Quantity quantity, CancellationToken cancellationToken)
    {
        var location = await locations.FindAsync(target, cancellationToken)
            ?? throw new DomainException(
                "put_away_location_unknown", $"Location {target} is not known to the warehouse topology.");

        var product = await products.FindAsync(sku, cancellationToken)
            ?? throw new DomainException(
                "put_away_product_unknown",
                $"Product {sku} is not yet known to inventory; cannot validate storage compatibility.");

        // Current occupancy at the target: sum each resident stock line by its product's footprint.
        var occupiedVolume = Volume.Zero;
        var occupiedWeight = Weight.Zero;
        foreach (var item in await stockItems.ListAtAsync(target, cancellationToken))
        {
            if (item.OnHand.IsZero)
            {
                continue;
            }

            var resident = item.Sku == sku ? product : await products.FindAsync(item.Sku, cancellationToken);
            if (resident is null)
            {
                continue;
            }

            occupiedVolume += Volume.FromCubicMeters(resident.UnitVolume.CubicMeters * item.OnHand.Amount);
            occupiedWeight += Weight.FromKilograms(resident.UnitWeight.Kilograms * item.OnHand.Amount);
        }

        var check = PutAwayPolicy.CanStore(product, location, quantity, occupiedVolume, occupiedWeight);
        if (!check.IsAllowed)
        {
            throw new DomainException("put_away_incompatible", check.RejectionReason!);
        }
    }
}
