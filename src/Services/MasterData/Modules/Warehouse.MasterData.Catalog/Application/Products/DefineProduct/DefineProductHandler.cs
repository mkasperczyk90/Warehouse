using Warehouse.Contracts.Catalog;
using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Catalog.Domain.Services;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.MasterData.Catalog.Application.Products.DefineProduct;

/// <summary>
/// Registers a product after the set-level uniqueness rules pass (<see cref="CatalogRegistrationPolicy"/>),
/// then announces it to the rest of the system through the transactional outbox: the product row and the
/// <see cref="ProductDefinedV1"/> event commit as one transaction.
/// </summary>
public sealed class DefineProductHandler(
    IProductTypeRepository products,
    IDbContextOutbox<CatalogDbContext> outbox)
{
    public async Task<string> HandleAsync(DefineProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sku = Sku.Of(command.Sku);
        var ean = string.IsNullOrWhiteSpace(command.Ean) ? null : Ean.Of(command.Ean);

        CatalogRegistrationPolicy.EnsureCanRegister(
            sku,
            ean,
            skuAlreadyExists: await products.ExistsAsync(sku, cancellationToken),
            eanAlreadyExists: ean is not null && await products.ExistsByEanAsync(ean, cancellationToken));

        if (!Enum.TryParse<ProductCategory>(command.Category, ignoreCase: true, out var category))
        {
            throw new DomainException("category_unknown", $"Unknown product category '{command.Category}'.");
        }

        var product = ProductType.Define(
            sku,
            command.Name,
            ean,
            category,
            Dimensions.Of(command.LengthCm, command.WidthCm, command.HeightCm),
            Weight.FromKilograms(command.UnitWeightKg),
            UnitOfMeasure.FromCode(command.BaseUnit),
            ProductStorageMap.ToRequirement(command.Storage, command.MinCelsius, command.MaxCelsius),
            command.IsBatchTracked,
            command.HasExpiryDate);

        products.Add(product);

        // Enqueue the integration event onto the outbox, then commit product + outbox row atomically.
        // V2 carries the unit footprint + temperature range so Inventory can enforce cold-chain/capacity
        // at put-away from its replica (ADR-0003); Logistics reads the subset it needs from the same V2.
        // V1 is retained as the prior wire shape (additive-only) for any external binder.
        await outbox.PublishAsync(new ProductDefinedV2(
            product.Sku.Value,
            product.Name,
            product.BaseUnit.Code,
            product.Storage.RequiresColdChain,
            product.Storage.IsHazardous,
            product.IsBatchTracked,
            product.UnitWeight.Kilograms,
            product.Dimensions.UnitVolume().CubicMeters,
            product.Storage.Temperature?.MinCelsius,
            product.Storage.Temperature?.MaxCelsius,
            DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return product.Sku.Value;
    }
}
