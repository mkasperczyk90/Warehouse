using System.Text.RegularExpressions;
using Warehouse.SharedKernel.Domain;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>Supplier's or producer's batch/lot number.</summary>
public sealed partial record BatchNumber
{
    private BatchNumber(string value) => Value = value;

    public string Value { get; }

    public static BatchNumber Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return Pattern().IsMatch(normalized)
            ? new BatchNumber(normalized)
            : throw new DomainException("batch_number_invalid", $"'{value}' is not a valid batch number.");
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9\-/\.]{0,31}$")]
    private static partial Regex Pattern();
}

/// <summary>
/// Inventory's own copy of the location address (the Topology context owns the structure;
/// contexts never share domain types — only the code format, which is part of the
/// ubiquitous language).
/// </summary>
public sealed partial record LocationCode
{
    private LocationCode(string value) => Value = value;

    public string Value { get; }

    public static LocationCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return Pattern().IsMatch(normalized)
            ? new LocationCode(normalized)
            : throw new DomainException("location_code_invalid", $"'{value}' is not a valid location code.");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{1,10}(-[A-Z0-9]{1,10}){1,4}$")]
    private static partial Regex Pattern();
}

/// <summary>
/// License Plate Number — the scannable identity of a handling unit (pallet, carton).
/// Unique across all warehouses; printed as a barcode on the unit itself.
/// </summary>
public sealed partial record LpnCode
{
    private LpnCode(string value) => Value = value;

    public string Value { get; }

    public static LpnCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return Pattern().IsMatch(normalized)
            ? new LpnCode(normalized)
            : throw new DomainException("lpn_invalid", $"'{value}' is not a valid LPN (4-20 chars A-Z, 0-9).");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{4,20}$")]
    private static partial Regex Pattern();
}

/// <summary>
/// Inventory's copy of a warehouse code — the scope of a soft reservation
/// (available-to-promise is tracked per SKU per warehouse).
/// </summary>
public sealed partial record WarehouseCode
{
    private WarehouseCode(string value) => Value = value;

    public string Value { get; }

    public static WarehouseCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return Pattern().IsMatch(normalized)
            ? new WarehouseCode(normalized)
            : throw new DomainException("warehouse_code_invalid", $"'{value}' is not a valid warehouse code.");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex Pattern();
}

/// <summary>Reference to the outbound order (Logistics context) a reservation/allocation belongs to.</summary>
public sealed record OrderRef
{
    private OrderRef(string value) => Value = value;

    public string Value { get; }

    public static OrderRef Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new OrderRef(value.Trim());
    }

    public override string ToString() => Value;
}
