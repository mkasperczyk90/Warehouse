using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Services;

namespace Warehouse.Warehousing.Inventory.Application.Storage;

/// <summary>
/// The hard storage-compatibility invariant (temperature / hazmat / capacity), shared by put-away
/// (UC-04) and replenishment moves (UC-06). Reads Topology's <c>LocationSnapshot</c> and the Catalog's
/// <c>ProductSnapshot</c> replicas — no cross-service query (ADR-0003). A location Topology has not
/// announced cannot be a target; an unknown product cannot be validated, so both are rejected rather than
/// waved through.
/// </summary>
public sealed class StorageCompatibility(
    IStockItemRepository stockItems,
    IProductSnapshotRepository products,
    ILocationSnapshotRepository locations)
{
    /// <param name="codePrefix">
    /// Prefix for the stable error codes the caller surfaces (e.g. <c>put_away</c> / <c>move</c>), so each
    /// flow keeps its own contract — <c>{prefix}_location_unknown</c>, <c>{prefix}_product_unknown</c>,
    /// <c>{prefix}_incompatible</c>.
    /// </param>
    public async Task EnsureCanStoreAsync(
        Sku sku, LocationCode target, Quantity quantity, string codePrefix, CancellationToken cancellationToken = default)
    {
        var location = await locations.FindAsync(target, cancellationToken)
            ?? throw new DomainException(
                $"{codePrefix}_location_unknown", $"Location {target} is not known to the warehouse topology.");

        var product = await products.FindAsync(sku, cancellationToken)
            ?? throw new DomainException(
                $"{codePrefix}_product_unknown",
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
            throw new DomainException($"{codePrefix}_incompatible", check.RejectionReason!);
        }
    }
}
