using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Domain.Services;

/// <summary>
/// Rule archetype as a domain service: can this product be stored at this location?
/// Used on every put-away and move. Environment compatibility is a HARD invariant
/// (a violation is rejected, never overridden); capacity additionally has a database
/// constraint as the last line of defense against concurrent put-aways.
/// </summary>
public static class PutAwayPolicy
{
    public static PutAwayCheck CanStore(
        ProductSnapshot product,
        LocationSnapshot location,
        Quantity quantityToStore,
        Volume volumeAlreadyAtLocation,
        Weight weightAlreadyAtLocation)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(quantityToStore);

        // Hard invariant: the room must maintain the product's required temperature range.
        if (product.RequiredTemperature is { } required &&
            !required.Contains(location.EnvironmentTemperature))
        {
            return PutAwayCheck.Rejected(
                $"Location {location.Code} maintains {location.EnvironmentTemperature}, " +
                $"but {product.Sku} requires {required}.");
        }

        if (product.IsHazardous && !location.IsHazmatZone)
        {
            return PutAwayCheck.Rejected(
                $"{product.Sku} is hazardous and {location.Code} is not a hazmat zone.");
        }

        var addedVolume = Volume.FromCubicMeters(product.UnitVolume.CubicMeters * quantityToStore.Amount);
        if (volumeAlreadyAtLocation + addedVolume > location.Capacity)
        {
            return PutAwayCheck.Rejected(
                $"Location {location.Code} capacity {location.Capacity} would be exceeded " +
                $"({volumeAlreadyAtLocation} used + {addedVolume} new).");
        }

        var addedWeight = Weight.FromKilograms(product.UnitWeight.Kilograms * quantityToStore.Amount);
        if (weightAlreadyAtLocation + addedWeight > location.MaxLoad)
        {
            return PutAwayCheck.Rejected(
                $"Location {location.Code} load limit {location.MaxLoad} would be exceeded " +
                $"({weightAlreadyAtLocation} used + {addedWeight} new).");
        }

        return PutAwayCheck.Allowed();
    }
}

public sealed record PutAwayCheck
{
    private PutAwayCheck(bool isAllowed, string? rejectionReason)
    {
        IsAllowed = isAllowed;
        RejectionReason = rejectionReason;
    }

    public bool IsAllowed { get; }

    public string? RejectionReason { get; }

    public static PutAwayCheck Allowed() => new(true, null);

    public static PutAwayCheck Rejected(string reason) => new(false, reason);
}
