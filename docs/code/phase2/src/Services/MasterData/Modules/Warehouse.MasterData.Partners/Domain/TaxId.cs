using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Partners.Domain;

/// <summary>
/// Tax identification number (NIP/VAT id). Country-specific checksum validation is a
/// later concern; here we normalize and require 8-15 alphanumeric characters.
/// </summary>
public sealed record TaxId
{
    private TaxId(string value) => Value = value;

    public string Value { get; }

    public static TaxId Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = new string([.. value.Where(char.IsLetterOrDigit)]).ToUpperInvariant();
        return normalized.Length is < 8 or > 15
            ? throw new DomainException("tax_id_invalid", $"'{value}' is not a valid tax identification number.")
            : new TaxId(normalized);
    }

    public override string ToString() => Value;
}
