using Warehouse.Contracts.Catalog;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Application.Consumers;

/// <summary>
/// Projects the Catalog's <see cref="ProductDefinedV1"/> into Inventory's local
/// <see cref="ProductSnapshot"/> (ADR-0003). The V1 event is intentionally minimal, so the
/// dimensional fields the put-away policy would use (unit weight/volume, temperature range) are
/// seeded with neutral defaults until a richer product event ships; the flags it does carry
/// (cold-chain, hazardous, batch-tracked) are projected faithfully.
/// </summary>
public sealed class ProductDefinedConsumer(IProductSnapshotRepository products, IUnitOfWork unitOfWork)
{
    public async Task Handle(ProductDefinedV1 message, CancellationToken cancellationToken)
    {
        var sku = Sku.Of(message.Sku);
        var unit = UnitOfMeasure.FromCode(message.BaseUnit);
        var existing = await products.FindAsync(sku, cancellationToken);

        if (existing is null)
        {
            products.Add(new ProductSnapshot(
                sku,
                unit,
                Weight.FromKilograms(0),
                Volume.FromCubicMeters(0),
                requiredTemperature: null,
                message.RequiresColdChain,
                message.IsHazardous,
                message.IsBatchTracked,
                hasExpiryDate: false,
                message.OccurredAt));
        }
        else
        {
            existing.Apply(
                unit,
                Weight.FromKilograms(0),
                Volume.FromCubicMeters(0),
                requiredTemperature: null,
                message.RequiresColdChain,
                message.IsHazardous,
                message.IsBatchTracked,
                hasExpiryDate: false,
                message.OccurredAt);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
