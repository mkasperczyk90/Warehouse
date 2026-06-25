using Warehouse.Contracts.Catalog;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Replicas;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Consumers;

/// <summary>
/// Projects the Catalog's <see cref="ProductDefinedV2"/> integration event into the local product
/// replica so the inbound use cases can validate SKUs without calling MasterData (ADR-0003). Logistics
/// reads only the subset it needs (code, base unit, batch-tracked); the footprint/temperature fields V2
/// adds for Inventory are ignored here. Wolverine discovers this handler by the <c>Handle</c> convention.
/// </summary>
public sealed class ProductDefinedConsumer(ICatalogProductReplica catalog, IUnitOfWork unitOfWork)
{
    public async Task Handle(ProductDefinedV2 message, CancellationToken cancellationToken)
    {
        var code = ProductCode.Of(message.Sku);
        var existing = await catalog.FindAsync(code, cancellationToken);
        if (existing is null)
        {
            catalog.Add(new CatalogProductSnapshot(code, message.BaseUnit, message.IsBatchTracked, message.OccurredAt));
        }
        else
        {
            existing.Apply(message.BaseUnit, message.IsBatchTracked, message.OccurredAt);
            catalog.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
