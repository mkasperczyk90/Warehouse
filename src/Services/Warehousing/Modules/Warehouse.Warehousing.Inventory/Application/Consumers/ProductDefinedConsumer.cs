using Warehouse.Contracts.Catalog;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Application.Consumers;

/// <summary>
/// Projects the Catalog's <see cref="ProductDefinedV2"/> into Inventory's local
/// <see cref="ProductSnapshot"/> (ADR-0003). V2 carries the unit footprint (weight + volume) and the
/// required temperature range, so the put-away policy now validates cold chain and capacity from real
/// data — no longer neutral defaults. Idempotent: a redelivery re-applies the same projection.
/// </summary>
public sealed class ProductDefinedConsumer(IProductSnapshotRepository products, IUnitOfWork unitOfWork)
{
    public async Task Handle(ProductDefinedV2 message, CancellationToken cancellationToken)
    {
        var sku = Sku.Of(message.Sku);
        var unit = UnitOfMeasure.FromCode(message.BaseUnit);
        var unitWeight = Weight.FromKilograms(message.UnitWeightKg);
        var unitVolume = Volume.FromCubicMeters(message.UnitVolumeM3);
        var requiredTemperature = message is { MinCelsius: { } min, MaxCelsius: { } max }
            ? TemperatureRange.Of(min, max)
            : null;
        var existing = await products.FindAsync(sku, cancellationToken);

        if (existing is null)
        {
            products.Add(new ProductSnapshot(
                sku,
                message.Name,
                unit,
                unitWeight,
                unitVolume,
                requiredTemperature,
                message.RequiresColdChain,
                message.IsHazardous,
                message.IsBatchTracked,
                hasExpiryDate: false,
                message.OccurredAt));
        }
        else
        {
            existing.Apply(
                message.Name,
                unit,
                unitWeight,
                unitVolume,
                requiredTemperature,
                message.RequiresColdChain,
                message.IsHazardous,
                message.IsBatchTracked,
                hasExpiryDate: false,
                message.OccurredAt);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
