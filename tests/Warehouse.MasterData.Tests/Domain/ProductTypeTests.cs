using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Catalog.Domain.Events;
using Warehouse.MasterData.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.MasterData.Tests.Domain;

public sealed class ProductTypeTests
{
    [Fact]
    public void Define_creates_a_product_and_raises_ProductDefined()
    {
        var product = Build.Product();

        Assert.Equal("SKU-1", product.Sku.Value);
        Assert.Equal("Widget", product.Name);
        Assert.Contains(product.DomainEvents, e => e is ProductDefined);
    }

    [Fact]
    public void Define_rejects_expiry_dates_without_batch_tracking() =>
        Expect.DomainError(
            "product_expiry_requires_batches",
            () => Build.Product(isBatchTracked: false, hasExpiryDate: true));

    [Fact]
    public void Define_rejects_a_refrigerated_product_without_a_cold_chain() =>
        Expect.DomainError(
            "storage_cold_chain_required",
            () => Build.Product(category: ProductCategory.Refrigerated, storage: StorageRequirement.Ambient));

    [Fact]
    public void Rename_trims_and_updates_the_label()
    {
        var product = Build.Product();

        product.Rename("  Renamed  ");

        Assert.Equal("Renamed", product.Name);
    }

    [Fact]
    public void ChangeStorageRequirement_rejects_dropping_the_cold_chain_on_a_refrigerated_product()
    {
        var product = Build.Product(
            category: ProductCategory.Refrigerated,
            storage: StorageRequirement.ColdChain(TemperatureRange.Of(0, 5)));

        Expect.DomainError(
            "storage_cold_chain_required",
            () => product.ChangeStorageRequirement(StorageRequirement.Ambient));
    }
}
