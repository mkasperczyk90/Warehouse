using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.SharedKernel.Tests.ValueObjects;

public sealed class TemperatureRangeTests
{
    [Fact]
    public void Of_rejects_an_inverted_range()
    {
        Expect.DomainError("temperature_range_invalid", () => TemperatureRange.Of(10, 5));
    }

    [Fact]
    public void Contains_is_true_only_when_the_other_range_fits_entirely_inside()
    {
        var coldRoom = TemperatureRange.Of(2, 8);

        Assert.True(coldRoom.Contains(TemperatureRange.Of(3, 6)));
        Assert.False(coldRoom.Contains(TemperatureRange.Of(1, 6)));  // below min
        Assert.False(coldRoom.Contains(TemperatureRange.Of(3, 9)));  // above max
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(8, true)]
    [InlineData(1, false)]
    [InlineData(9, false)]
    public void Contains_celsius_is_inclusive(decimal celsius, bool expected)
    {
        Assert.Equal(expected, TemperatureRange.Of(2, 8).Contains(celsius));
    }

    [Fact]
    public void Overlaps_is_true_when_the_ranges_share_any_temperature()
    {
        var a = TemperatureRange.Of(2, 8);

        Assert.True(a.Overlaps(TemperatureRange.Of(6, 12)));   // partial overlap
        Assert.True(a.Overlaps(TemperatureRange.Of(8, 10)));   // touch at the edge
        Assert.False(a.Overlaps(TemperatureRange.Of(9, 12)));  // disjoint
    }
}
