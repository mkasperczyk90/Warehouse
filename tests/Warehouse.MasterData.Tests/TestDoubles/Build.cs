using Warehouse.MasterData.Catalog.Application.Products.DefineProduct;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Tests.TestDoubles;

/// <summary>Builders for valid catalog fixtures, so each test only states what it cares about.</summary>
internal static class Build
{
    public static ProductType Product(
        string sku = "SKU-1",
        string name = "Widget",
        ProductCategory category = ProductCategory.DryGoods,
        StorageRequirement? storage = null,
        bool isBatchTracked = false,
        bool hasExpiryDate = false) =>
        ProductType.Define(
            Sku.Of(sku),
            name,
            ean: null,
            category,
            Dimensions.Of(10, 10, 10),
            Weight.FromKilograms(1),
            UnitOfMeasure.Piece,
            storage ?? StorageRequirement.Ambient,
            isBatchTracked,
            hasExpiryDate);

    public static DefineProductCommand DefineCommand(
        string sku = "SKU-1",
        string? ean = null,
        string category = "DryGoods",
        string storage = "Ambient",
        decimal? min = null,
        decimal? max = null,
        bool isBatchTracked = false,
        bool hasExpiryDate = false) =>
        new(sku, "Widget", ean, category, 10, 10, 10, 1, "pcs", storage, min, max, isBatchTracked, hasExpiryDate);
}
