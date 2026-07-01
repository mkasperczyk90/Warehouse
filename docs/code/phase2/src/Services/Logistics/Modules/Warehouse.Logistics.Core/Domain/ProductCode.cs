using Warehouse.SharedKernel.Domain;

namespace Warehouse.Logistics.Core.Domain;

/// <summary>
/// A product reference in Logistics — "what the scanner read" or "what the supplier
/// announced". Deliberately loose: an inbound line may carry a code that is not yet a known
/// catalog SKU (UC-01 flags unknown codes for clarification), so this type must accept it.
/// Resolving a <c>ProductCode</c> to a real catalog SKU is an integration concern, not a
/// constructor rule — which is exactly why Logistics must not share Catalog's strict <c>Sku</c>.
/// </summary>
public sealed record ProductCode
{
    private ProductCode(string value) => Value = value;

    public string Value { get; }

    public static ProductCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ProductCode(value.Trim().ToUpperInvariant());
    }

    public override string ToString() => Value;
}
