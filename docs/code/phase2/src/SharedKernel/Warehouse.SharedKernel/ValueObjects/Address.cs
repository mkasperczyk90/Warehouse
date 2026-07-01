using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.ValueObjects;

/// <summary>Postal address — used for warehouse sites, consignees and party roles.</summary>
public sealed record Address
{
    private Address(string street, string city, string postalCode, string countryCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    public string Street { get; }

    public string City { get; }

    public string PostalCode { get; }

    /// <summary>ISO 3166-1 alpha-2, e.g. "PL".</summary>
    public string CountryCode { get; }

    public static Address Of(string street, string city, string postalCode, string countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        return normalizedCountry.Length != 2 || !normalizedCountry.All(char.IsAsciiLetterUpper)
            ? throw new DomainException("country_code_invalid", $"'{countryCode}' is not a valid ISO 3166-1 alpha-2 code.")
            : new Address(street.Trim(), city.Trim(), postalCode.Trim(), normalizedCountry);
    }

    public override string ToString() => $"{Street}, {PostalCode} {City}, {CountryCode}";
}
