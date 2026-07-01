using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Catalog.Domain;

/// <summary>EAN-8 / EAN-13 barcode with checksum validation.</summary>
public sealed record Ean
{
    private Ean(string value) => Value = value;

    public string Value { get; }

    public static Ean Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var digits = value.Trim();
        if ((digits.Length != 8 && digits.Length != 13) || !digits.All(char.IsAsciiDigit))
        {
            throw new DomainException("ean_invalid", $"'{value}' is not a valid EAN-8/EAN-13 (digits only).");
        }

        return HasValidChecksum(digits)
            ? new Ean(digits)
            : throw new DomainException("ean_checksum_invalid", $"'{value}' has an invalid EAN checksum.");
    }

    public override string ToString() => Value;

    private static bool HasValidChecksum(string digits)
    {
        var sum = 0;
        // Weights 1/3 alternate from the right, starting with 3 next to the check digit.
        for (var i = digits.Length - 2; i >= 0; i--)
        {
            var weight = (digits.Length - 2 - i) % 2 == 0 ? 3 : 1;
            sum += (digits[i] - '0') * weight;
        }

        var check = (10 - (sum % 10)) % 10;
        return check == digits[^1] - '0';
    }
}
