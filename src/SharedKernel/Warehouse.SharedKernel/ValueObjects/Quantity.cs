using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.ValueObjects;

/// <summary>
/// Quantity archetype: an amount that always carries its unit. Warehouse quantities are
/// non-negative by definition — direction of a movement is modeled by the movement type,
/// never by a signed amount.
/// </summary>
public sealed record Quantity
{
    private Quantity(decimal amount, UnitOfMeasure unit)
    {
        Amount = amount;
        Unit = unit;
    }

    public decimal Amount { get; }

    public UnitOfMeasure Unit { get; }

    public static Quantity Of(decimal amount, UnitOfMeasure unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return amount < 0
            ? throw new DomainException("quantity_negative", $"Quantity cannot be negative, got {amount} {unit}.")
            : new Quantity(amount, unit);
    }

    public static Quantity Zero(UnitOfMeasure unit) => Of(0, unit);

    public bool IsZero => Amount == 0;

    public Quantity Add(Quantity other)
    {
        EnsureSameUnit(other);
        return new Quantity(Amount + other.Amount, Unit);
    }

    public Quantity Subtract(Quantity other)
    {
        EnsureSameUnit(other);
        return other.Amount > Amount
            ? throw new DomainException(
                "quantity_insufficient",
                $"Cannot subtract {other.Amount} {Unit} from {Amount} {Unit} — quantity would become negative.")
            : new Quantity(Amount - other.Amount, Unit);
    }

    public bool IsGreaterThanOrEqualTo(Quantity other)
    {
        EnsureSameUnit(other);
        return Amount >= other.Amount;
    }

    public static Quantity operator +(Quantity left, Quantity right) => Add(left, right);

    public static Quantity operator -(Quantity left, Quantity right) => Subtract(left, right);

    public override string ToString() => $"{Amount} {Unit}";

    private static Quantity Add(Quantity left, Quantity right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Add(right);
    }

    private static Quantity Subtract(Quantity left, Quantity right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Subtract(right);
    }

    private void EnsureSameUnit(Quantity other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Unit != other.Unit)
        {
            throw new DomainException(
                "quantity_unit_mismatch",
                $"Cannot combine quantities of different units: {Unit} and {other.Unit}.");
        }
    }
}
