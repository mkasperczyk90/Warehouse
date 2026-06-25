using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Application.Products;

/// <summary>
/// Translates the primitive storage shape the API speaks (a mode name + optional temperature bounds)
/// to and from the <see cref="StorageRequirement"/> value object. Shared by the define and
/// change-storage slices so the wire contract stays in one place.
/// </summary>
internal static class ProductStorageMap
{
    public const string Ambient = "Ambient";
    public const string ColdChain = "ColdChain";
    public const string Hazardous = "Hazardous";

    public static StorageRequirement ToRequirement(string mode, decimal? minCelsius, decimal? maxCelsius)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);

        TemperatureRange? range = minCelsius is { } min && maxCelsius is { } max
            ? TemperatureRange.Of(min, max)
            : null;

        return mode switch
        {
            Ambient => StorageRequirement.Ambient,
            ColdChain => range is null
                ? throw new DomainException(
                    "storage_temperature_required",
                    "A cold-chain product must declare its temperature range (min and max °C).")
                : StorageRequirement.ColdChain(range),
            Hazardous => StorageRequirement.Hazardous(range),
            _ => throw new DomainException(
                "storage_mode_unknown",
                $"Unknown storage mode '{mode}'. Use Ambient, ColdChain or Hazardous."),
        };
    }

    public static string ToMode(StorageRequirement storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        return storage switch
        {
            { RequiresColdChain: true } => ColdChain,
            { IsHazardous: true } => Hazardous,
            _ => Ambient,
        };
    }
}
