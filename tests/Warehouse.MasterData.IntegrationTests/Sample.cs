using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.IntegrationTests;

/// <summary>Builders for valid catalog fixtures used by the database tests.</summary>
internal static class Sample
{
    public static ProductType Product(
        string sku = "MILK-1L",
        string name = "Whole milk 3.2% — 1 L carton",
        string? ean = "4006381333931",
        ProductCategory category = ProductCategory.Refrigerated,
        StorageRequirement? storage = null,
        bool isBatchTracked = true,
        bool hasExpiryDate = true,
        bool withConversion = true)
    {
        var product = ProductType.Define(
            Sku.Of(sku),
            name,
            ean is null ? null : Ean.Of(ean),
            category,
            Dimensions.Of(7, 7, 20),
            Weight.FromKilograms(1.03m),
            UnitOfMeasure.Piece,
            storage ?? StorageRequirement.ColdChain(TemperatureRange.Of(2, 6)),
            isBatchTracked,
            hasExpiryDate);

        if (withConversion)
        {
            product.AddUnitConversion(UnitOfMeasure.Carton, 24);
        }

        return product;
    }

    public static ProductType DryProduct(string sku = "BOX-L") =>
        Product(
            sku,
            name: "Cardboard box L",
            ean: null,
            category: ProductCategory.DryGoods,
            storage: StorageRequirement.Ambient,
            isBatchTracked: false,
            hasExpiryDate: false,
            withConversion: false);
}
