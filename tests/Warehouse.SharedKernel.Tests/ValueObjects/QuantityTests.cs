using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.SharedKernel.Tests.ValueObjects;

public sealed class QuantityTests
{
    private static readonly UnitOfMeasure Pcs = UnitOfMeasure.Piece;
    private static readonly UnitOfMeasure Kg = UnitOfMeasure.Kilogram;

    [Fact]
    public void Of_keeps_amount_and_unit()
    {
        var qty = Quantity.Of(5, Pcs);

        Assert.Equal(5, qty.Amount);
        Assert.Equal(Pcs, qty.Unit);
    }

    [Fact]
    public void Of_rejects_a_negative_amount()
    {
        Expect.DomainError("quantity_negative", () => Quantity.Of(-1, Pcs));
    }

    [Fact]
    public void Zero_is_zero_and_reports_it()
    {
        var zero = Quantity.Zero(Pcs);

        Assert.True(zero.IsZero);
        Assert.Equal(0, zero.Amount);
    }

    [Fact]
    public void Add_sums_amounts_of_the_same_unit()
    {
        var sum = Quantity.Of(2, Pcs) + Quantity.Of(3, Pcs);

        Assert.Equal(5, sum.Amount);
    }

    [Fact]
    public void Subtract_below_zero_is_rejected()
    {
        Expect.DomainError("quantity_insufficient", () => Quantity.Of(2, Pcs).Subtract(Quantity.Of(3, Pcs)));
    }

    [Fact]
    public void Combining_different_units_is_rejected()
    {
        Expect.DomainError("quantity_unit_mismatch", () => Quantity.Of(2, Pcs).Add(Quantity.Of(1, Kg)));
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, true)]
    [InlineData(4, 5, false)]
    public void IsGreaterThanOrEqualTo_compares_same_unit(decimal left, decimal right, bool expected)
    {
        Assert.Equal(expected, Quantity.Of(left, Pcs).IsGreaterThanOrEqualTo(Quantity.Of(right, Pcs)));
    }

    [Fact]
    public void Equality_is_by_amount_and_unit()
    {
        Assert.Equal(Quantity.Of(5, Pcs), Quantity.Of(5, Pcs));
        Assert.NotEqual(Quantity.Of(5, Pcs), Quantity.Of(5, Kg));
    }
}
