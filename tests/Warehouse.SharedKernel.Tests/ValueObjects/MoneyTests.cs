using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.SharedKernel.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Of_normalizes_the_currency_to_upper_case()
    {
        var money = Money.Of(10, "pln");

        Assert.Equal("PLN", money.Currency);
        Assert.Equal(10, money.Amount);
    }

    [Theory]
    [InlineData("PL")]    // too short
    [InlineData("EURO")]  // too long
    [InlineData("E1R")]   // not all letters
    public void Of_rejects_a_non_iso4217_currency(string currency)
    {
        Expect.DomainError("currency_invalid", () => Money.Of(10, currency));
    }

    [Fact]
    public void Add_and_subtract_keep_the_currency()
    {
        var result = Money.Of(10, "EUR") + Money.Of(5, "EUR") - Money.Of(2, "EUR");

        Assert.Equal(13, result.Amount);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public void Multiply_scales_the_amount()
    {
        Assert.Equal(30, Money.Of(10, "EUR").Multiply(3).Amount);
    }

    [Fact]
    public void Combining_different_currencies_is_rejected()
    {
        Expect.DomainError("currency_mismatch", () => Money.Of(10, "EUR").Add(Money.Of(10, "PLN")));
    }
}
