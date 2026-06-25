namespace Warehouse.MasterData.Catalog.Application.Products.DefineProduct;

/// <summary>
/// Define a new product card (UC: register a SKU). Primitives only — the handler builds the value
/// objects. <see cref="Storage"/> is one of <c>Ambient</c> / <c>ColdChain</c> / <c>Hazardous</c>;
/// <see cref="MinCelsius"/>/<see cref="MaxCelsius"/> carry the temperature range when it applies.
/// </summary>
public sealed record DefineProductCommand(
    string Sku,
    string Name,
    string? Ean,
    string Category,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    decimal UnitWeightKg,
    string BaseUnit,
    string Storage,
    decimal? MinCelsius,
    decimal? MaxCelsius,
    bool IsBatchTracked,
    bool HasExpiryDate);
