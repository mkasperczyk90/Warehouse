using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.SharedKernel.Tests.ValueObjects;

public sealed class AddressTests
{
    [Fact]
    public void Of_trims_fields_and_upper_cases_the_country()
    {
        var address = Address.Of(" Main 1 ", " Wrocław ", " 50-001 ", "pl");

        Assert.Equal("Main 1", address.Street);
        Assert.Equal("Wrocław", address.City);
        Assert.Equal("50-001", address.PostalCode);
        Assert.Equal("PL", address.CountryCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Of_rejects_a_blank_street(string street)
    {
        Assert.ThrowsAny<ArgumentException>(() => Address.Of(street, "Wrocław", "50-001", "PL"));
    }

    [Theory]
    [InlineData("P")]    // too short
    [InlineData("POL")]  // too long
    [InlineData("P1")]   // not letters
    public void Of_rejects_a_non_iso_country_code(string country)
    {
        Expect.DomainError("country_code_invalid", () => Address.Of("Main 1", "Wrocław", "50-001", country));
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(
            Address.Of("Main 1", "Wrocław", "50-001", "PL"),
            Address.Of("Main 1", "Wrocław", "50-001", "PL"));
    }
}
