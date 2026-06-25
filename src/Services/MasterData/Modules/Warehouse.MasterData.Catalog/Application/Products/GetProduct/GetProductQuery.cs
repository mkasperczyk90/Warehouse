using Microsoft.EntityFrameworkCore;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;

namespace Warehouse.MasterData.Catalog.Application.Products.GetProduct;

/// <summary>Read model for a single product card (full detail, including unit conversions).</summary>
public sealed record GetProductQuery(string Sku);

public sealed record ProductDto(
    string Sku,
    string Name,
    string? Ean,
    string Category,
    DimensionsDto Dimensions,
    decimal UnitWeightKg,
    string BaseUnit,
    StorageDto Storage,
    bool IsBatchTracked,
    bool HasExpiryDate,
    IReadOnlyList<UnitConversionDto> UnitConversions);

public sealed record DimensionsDto(decimal LengthCm, decimal WidthCm, decimal HeightCm);

public sealed record StorageDto(
    string Mode,
    decimal? MinCelsius,
    decimal? MaxCelsius,
    bool RequiresColdChain,
    bool IsHazardous);

public sealed record UnitConversionDto(string Unit, decimal FactorToBase);

public sealed class GetProductHandler(CatalogDbContext db)
{
    public async Task<ProductDto?> HandleAsync(GetProductQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var sku = Sku.Of(query.Sku);

        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == sku, cancellationToken);
        return product is null ? null : Map(product);
    }

    internal static ProductDto Map(ProductType p) => new(
        p.Sku.Value,
        p.Name,
        p.Ean?.Value,
        p.Category.ToString(),
        new DimensionsDto(p.Dimensions.LengthCm, p.Dimensions.WidthCm, p.Dimensions.HeightCm),
        p.UnitWeight.Kilograms,
        p.BaseUnit.Code,
        new StorageDto(
            ProductStorageMap.ToMode(p.Storage),
            p.Storage.Temperature?.MinCelsius,
            p.Storage.Temperature?.MaxCelsius,
            p.Storage.RequiresColdChain,
            p.Storage.IsHazardous),
        p.IsBatchTracked,
        p.HasExpiryDate,
        p.UnitConversions.Select(c => new UnitConversionDto(c.Unit.Code, c.FactorToBase)).ToList());
}
