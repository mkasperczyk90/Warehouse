using Warehouse.SharedKernel.Domain;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// Inventory's product identifier. Deliberately lighter than Catalog's strict <c>Sku</c>:
/// by the time goods are on stock the product is cataloged and the code is known-good, so
/// Inventory normalizes but does not re-run Catalog's syntax rules. Owning the type (instead
/// of sharing one) keeps Catalog's validation from leaking into a different service — the
/// same reasoning that duplicates <c>LocationCode</c> across contexts.
/// </summary>
public sealed record Sku
{
    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Sku Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new Sku(value.Trim().ToUpperInvariant());
    }

    public override string ToString() => Value;
}
