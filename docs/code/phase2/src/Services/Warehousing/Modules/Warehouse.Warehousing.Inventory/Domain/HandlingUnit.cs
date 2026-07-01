using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain.Events;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// Thing archetype (🟩): a logistic unit (pallet/carton/tote) identified by a scannable
/// LPN. Moving the unit means one scan instead of one per SKU.
///
/// Stock truth stays in <see cref="StockItem"/> — a handling unit describes HOW goods are
/// physically grouped, not how much exists. On relocation the application layer pairs the
/// <see cref="HandlingUnitMoved"/> event with StockTransferService calls per content line,
/// in the same transaction.
/// </summary>
public sealed class HandlingUnit : AggregateRoot<HandlingUnitId>
{
    private readonly List<HandlingUnitLine> _lines = [];

    private HandlingUnit(HandlingUnitId id, LpnCode lpn, HandlingUnitKind kind, LocationCode location)
        : base(id)
    {
        Lpn = lpn;
        Kind = kind;
        Location = location;
        Status = HandlingUnitStatus.Open;
    }

    private HandlingUnit()
    {
    }

    public LpnCode Lpn { get; private set; } = null!;

    public HandlingUnitKind Kind { get; private set; }

    public LocationCode Location { get; private set; } = null!;

    public HandlingUnitStatus Status { get; private set; }

    public IReadOnlyCollection<HandlingUnitLine> Lines => _lines.AsReadOnly();

    public bool IsEmpty => _lines.Count == 0;

    public static HandlingUnit CreateAt(LpnCode lpn, HandlingUnitKind kind, LocationCode location)
    {
        ArgumentNullException.ThrowIfNull(lpn);
        ArgumentNullException.ThrowIfNull(location);
        return new HandlingUnit(HandlingUnitId.New(), lpn, kind, location);
    }

    /// <summary>Adds goods to the unit; same SKU+batch lines are merged.</summary>
    public void Pack(Sku sku, BatchNumber? batch, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(quantity);
        EnsureOpen("pack");
        if (quantity.IsZero)
        {
            throw new DomainException("handling_unit_pack_zero", "Cannot pack a zero quantity.");
        }

        var line = _lines.SingleOrDefault(l => l.Sku == sku && l.Batch == batch);
        if (line is null)
        {
            _lines.Add(new HandlingUnitLine(sku, batch, quantity));
        }
        else
        {
            line.Increase(quantity);
        }
    }

    public void Unpack(Sku sku, BatchNumber? batch, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(quantity);
        EnsureOpen("unpack");

        var line = _lines.SingleOrDefault(l => l.Sku == sku && l.Batch == batch)
            ?? throw new DomainException(
                "handling_unit_line_not_found",
                $"Handling unit {Lpn} does not contain {sku} (batch {batch?.Value ?? "-"}).");

        line.Decrease(quantity);
        if (line.Quantity.IsZero)
        {
            _lines.Remove(line);
        }
    }

    public void Close()
    {
        EnsureOpen("close");
        if (IsEmpty)
        {
            throw new DomainException("handling_unit_empty", $"Handling unit {Lpn} has no contents to close.");
        }

        Status = HandlingUnitStatus.Closed;
    }

    public void Reopen()
    {
        if (Status != HandlingUnitStatus.Closed)
        {
            throw new DomainException("handling_unit_not_closed", $"Handling unit {Lpn} is not closed.");
        }

        Status = HandlingUnitStatus.Open;
    }

    /// <summary>
    /// One scan moves the whole unit. The application layer reacts to the event by
    /// transferring each content line via StockTransferService in the same transaction.
    /// </summary>
    public void RelocateTo(LocationCode location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (location == Location)
        {
            throw new DomainException("handling_unit_same_location", $"Handling unit {Lpn} is already at {location}.");
        }

        if (IsEmpty)
        {
            throw new DomainException("handling_unit_empty", $"Handling unit {Lpn} is empty — nothing to relocate.");
        }

        var from = Location;
        Location = location;
        Raise(new HandlingUnitMoved(Id, Lpn, from, location, DateTimeOffset.UtcNow));
    }

    private void EnsureOpen(string action)
    {
        if (Status != HandlingUnitStatus.Open)
        {
            throw new DomainException(
                "handling_unit_not_open",
                $"Cannot {action}: handling unit {Lpn} is {Status}.");
        }
    }
}

/// <summary>Contents of a handling unit: one SKU + batch with its quantity.</summary>
public sealed class HandlingUnitLine
{
    internal HandlingUnitLine(Sku sku, BatchNumber? batch, Quantity quantity)
    {
        Sku = sku;
        Batch = batch;
        Quantity = quantity;
    }

    private HandlingUnitLine()
    {
    }

    public Sku Sku { get; } = null!;

    public BatchNumber? Batch { get; }

    public Quantity Quantity { get; private set; } = null!;

    internal void Increase(Quantity by) => Quantity = Quantity.Add(by);

    internal void Decrease(Quantity by) => Quantity = Quantity.Subtract(by);
}
