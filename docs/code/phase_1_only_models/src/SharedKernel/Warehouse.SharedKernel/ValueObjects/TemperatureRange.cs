using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.ValueObjects;

/// <summary>
/// An inclusive temperature range in °C. The Rule archetype building block used on both
/// sides of the storage-compatibility check (product requirement vs room environment).
/// </summary>
public sealed record TemperatureRange
{
    private TemperatureRange(decimal minCelsius, decimal maxCelsius)
    {
        MinCelsius = minCelsius;
        MaxCelsius = maxCelsius;
    }

    public decimal MinCelsius { get; }

    public decimal MaxCelsius { get; }

    /// <summary>Typical ambient storage conditions.</summary>
    public static TemperatureRange Ambient { get; } = new(5, 30);

    public static TemperatureRange Of(decimal minCelsius, decimal maxCelsius) =>
        minCelsius > maxCelsius
            ? throw new DomainException(
                "temperature_range_invalid",
                $"Min temperature {minCelsius}°C cannot be greater than max {maxCelsius}°C.")
            : new TemperatureRange(minCelsius, maxCelsius);

    /// <summary>True when <paramref name="other"/> lies entirely within this range.</summary>
    public bool Contains(TemperatureRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return MinCelsius <= other.MinCelsius && other.MaxCelsius <= MaxCelsius;
    }

    public bool Contains(decimal celsius) => MinCelsius <= celsius && celsius <= MaxCelsius;

    /// <summary>True when the ranges share at least one temperature.</summary>
    public bool Overlaps(TemperatureRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return MinCelsius <= other.MaxCelsius && other.MinCelsius <= MaxCelsius;
    }

    public override string ToString() => $"{MinCelsius}°C..{MaxCelsius}°C";
}
