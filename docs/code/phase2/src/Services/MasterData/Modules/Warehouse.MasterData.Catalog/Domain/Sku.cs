using System.Text.RegularExpressions;
using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Catalog.Domain;

/// <summary>
/// The canonical Stock Keeping Unit — Catalog owns it because Catalog is where a SKU
/// *means* something (a product card, linked to an EAN). Strict syntax: every SKU minted
/// here is a guaranteed-valid product identifier. Other contexts deliberately do NOT share
/// this type: Inventory keeps a lighter <c>Sku</c> (stock is always for cataloged products),
/// Logistics keeps a loose <c>ProductCode</c> ("what the scanner read"). The shared thing is
/// the language (the code convention), never the type.
/// </summary>
public sealed partial record Sku
{
    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Sku Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return SkuPattern().IsMatch(normalized)
            ? new Sku(normalized)
            : throw new DomainException(
                "sku_invalid",
                $"'{value}' is not a valid SKU (2-32 chars: A-Z, 0-9, '-', must start alphanumeric).");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{1,31}$")]
    private static partial Regex SkuPattern();
}
