using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.SharedKernel.Tests.ValueObjects;

public sealed class WeightTests
{
    [Fact]
    public void FromKilograms_rejects_a_negative_value()
    {
        Expect.DomainError("weight_negative", () => Weight.FromKilograms(-0.1m));
    }

    [Fact]
    public void FromGrams_normalizes_to_kilograms()
    {
        Assert.Equal(1.5m, Weight.FromGrams(1500).Kilograms);
    }

    [Fact]
    public void Subtract_below_zero_is_rejected()
    {
        Expect.DomainError("weight_negative", () => Weight.FromKilograms(1).Subtract(Weight.FromKilograms(2)));
    }

    [Fact]
    public void Comparison_operators_order_by_mass()
    {
        Assert.True(Weight.FromKilograms(2) > Weight.FromKilograms(1));
        Assert.True(Weight.FromKilograms(1) <= Weight.FromKilograms(1));
    }
}

public sealed class VolumeTests
{
    [Fact]
    public void FromCubicMeters_rejects_a_negative_value()
    {
        Expect.DomainError("volume_negative", () => Volume.FromCubicMeters(-1));
    }

    [Fact]
    public void Add_and_subtract_combine_cubic_meters()
    {
        Assert.Equal(3, (Volume.FromCubicMeters(5) - Volume.FromCubicMeters(2)).CubicMeters);
    }

    [Fact]
    public void Subtract_below_zero_is_rejected()
    {
        Expect.DomainError("volume_negative", () => Volume.FromCubicMeters(1).Subtract(Volume.FromCubicMeters(2)));
    }
}

public sealed class UnitOfMeasureTests
{
    [Fact]
    public void FromCode_is_case_insensitive_and_returns_the_singleton()
    {
        Assert.Same(UnitOfMeasure.Piece, UnitOfMeasure.FromCode("PCS"));
        Assert.Same(UnitOfMeasure.Kilogram, UnitOfMeasure.FromCode("kg"));
    }

    [Fact]
    public void FromCode_rejects_an_unknown_unit()
    {
        Expect.DomainError("unit_unknown", () => UnitOfMeasure.FromCode("furlong"));
    }
}
