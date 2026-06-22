using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.ValueObjects;

/// <summary>Mass, normalized to kilograms. Used for load limits and product unit weight.</summary>
public readonly record struct Weight : IComparable<Weight>
{
    private Weight(decimal kilograms) => Kilograms = kilograms;

    public decimal Kilograms { get; }

    public static Weight Zero { get; } = new(0);

    public static Weight FromKilograms(decimal kilograms) =>
        kilograms < 0
            ? throw new DomainException("weight_negative", $"Weight cannot be negative, got {kilograms} kg.")
            : new Weight(kilograms);

    public static Weight FromGrams(decimal grams) => FromKilograms(grams / 1000m);

    public Weight Add(Weight other) => new(Kilograms + other.Kilograms);

    public Weight Subtract(Weight other) =>
        other.Kilograms > Kilograms
            ? throw new DomainException("weight_negative", "Weight cannot become negative.")
            : new Weight(Kilograms - other.Kilograms);

    public int CompareTo(Weight other) => Kilograms.CompareTo(other.Kilograms);

    public static Weight operator +(Weight left, Weight right) => left.Add(right);

    public static Weight operator -(Weight left, Weight right) => left.Subtract(right);

    public static bool operator >(Weight left, Weight right) => left.CompareTo(right) > 0;

    public static bool operator <(Weight left, Weight right) => left.CompareTo(right) < 0;

    public static bool operator >=(Weight left, Weight right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Weight left, Weight right) => left.CompareTo(right) <= 0;

    public override string ToString() => $"{Kilograms} kg";
}
