using Warehouse.SharedKernel.Domain;

namespace Warehouse.Logistics.Core.Domain;

/// <summary>Reference to a party role (supplier/customer/carrier) owned by the Partners context. An
/// opaque external identifier — a Party-role id in production, a free-text label in dev — so it is
/// carried as a string rather than a Guid (the desk announces orders before a Partner picker exists).</summary>
public readonly record struct PartyRoleRef(string Value)
{
    public override string ToString() => Value;
}

/// <summary>Reference to a warehouse owned by the Topology context.</summary>
public sealed record WarehouseRef
{
    private WarehouseRef(string code) => Code = code;

    public string Code { get; }

    public static WarehouseRef Of(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new WarehouseRef(code.Trim().ToUpperInvariant());
    }

    public override string ToString() => Code;
}

/// <summary>Reference to a storage location owned by the Topology/Inventory contexts.</summary>
public sealed record LocationRef
{
    private LocationRef(string code) => Code = code;

    public string Code { get; }

    public static LocationRef Of(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new LocationRef(code.Trim().ToUpperInvariant());
    }

    public override string ToString() => Code;
}

/// <summary>A reserved time window at a dock (ramp) for a truck.</summary>
public sealed record DockSlot
{
    private DockSlot(string dockCode, DateTimeOffset from, DateTimeOffset to)
    {
        DockCode = dockCode;
        From = from;
        To = to;
    }

    public string DockCode { get; }

    public DateTimeOffset From { get; }

    public DateTimeOffset To { get; }

    public static DockSlot Of(string dockCode, DateTimeOffset from, DateTimeOffset to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dockCode);
        return from >= to
            ? throw new DomainException("dock_slot_invalid", $"Dock slot window must be positive, got {from:u}..{to:u}.")
            : new DockSlot(dockCode.Trim().ToUpperInvariant(), from, to);
    }

    public override string ToString() => $"{DockCode} {From:u}..{To:u}";
}

/// <summary>Batch details captured at goods receipt for batch-tracked products.</summary>
public sealed record BatchInfo
{
    private BatchInfo(string number, DateOnly? expiryDate)
    {
        Number = number;
        ExpiryDate = expiryDate;
    }

    public string Number { get; }

    public DateOnly? ExpiryDate { get; }

    public static BatchInfo Of(string number, DateOnly? expiryDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        return new BatchInfo(number.Trim().ToUpperInvariant(), expiryDate);
    }
}

/// <summary>Carrier's tracking number for a dispatched shipment.</summary>
public sealed record TrackingNumber
{
    private TrackingNumber(string value) => Value = value;

    public string Value { get; }

    public static TrackingNumber Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new TrackingNumber(value.Trim());
    }

    public override string ToString() => Value;
}
