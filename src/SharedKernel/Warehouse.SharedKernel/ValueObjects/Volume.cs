using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.ValueObjects;

/// <summary>Volume, normalized to cubic meters. Used for location capacity checks.</summary>
public readonly record struct Volume : IComparable<Volume>
{
    private Volume(decimal cubicMeters) => CubicMeters = cubicMeters;

    public decimal CubicMeters { get; }

    public static Volume Zero { get; } = new(0);

    public static Volume FromCubicMeters(decimal cubicMeters) =>
        cubicMeters < 0
            ? throw new DomainException("volume_negative", $"Volume cannot be negative, got {cubicMeters} m³.")
            : new Volume(cubicMeters);

    public Volume Add(Volume other) => new(CubicMeters + other.CubicMeters);

    public Volume Subtract(Volume other) =>
        other.CubicMeters > CubicMeters
            ? throw new DomainException("volume_negative", "Volume cannot become negative.")
            : new Volume(CubicMeters - other.CubicMeters);

    public int CompareTo(Volume other) => CubicMeters.CompareTo(other.CubicMeters);

    public static Volume operator +(Volume left, Volume right) => left.Add(right);

    public static Volume operator -(Volume left, Volume right) => left.Subtract(right);

    public static bool operator >(Volume left, Volume right) => left.CompareTo(right) > 0;

    public static bool operator <(Volume left, Volume right) => left.CompareTo(right) < 0;

    public static bool operator >=(Volume left, Volume right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Volume left, Volume right) => left.CompareTo(right) <= 0;

    public override string ToString() => $"{CubicMeters} m³";
}
