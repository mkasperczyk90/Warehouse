using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): one delivery from announcement (ASN) to completed
/// put-away (UC-01, UC-02, UC-04). Only announced deliveries can be received —
/// an unannounced truck gets an ad-hoc ASN first.
/// </summary>
public sealed class InboundDelivery : AggregateRoot<DeliveryId>
{
    private readonly List<DeliveryLine> _lines = [];

    private InboundDelivery(
        DeliveryId id, PartyRoleRef supplier, WarehouseRef warehouse, DateTimeOffset plannedAt)
        : base(id)
    {
        Supplier = supplier;
        Warehouse = warehouse;
        PlannedAt = plannedAt;
        Status = DeliveryStatus.Announced;
    }

    private InboundDelivery()
    {
    }

    public PartyRoleRef Supplier { get; private set; }

    public WarehouseRef Warehouse { get; private set; } = null!;

    public DateTimeOffset PlannedAt { get; private set; }

    public DockSlot? Slot { get; private set; }

    public DeliveryStatus Status { get; private set; }

    public IReadOnlyCollection<DeliveryLine> Lines => _lines.AsReadOnly();

    public static InboundDelivery Announce(
        PartyRoleRef supplier,
        WarehouseRef warehouse,
        DateTimeOffset plannedAt,
        IReadOnlyCollection<(ProductCode Product, Quantity Expected, DeliveryPack? Pack)> lines)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException("delivery_lines_empty", "An ASN needs at least one line.");
        }

        var delivery = new InboundDelivery(DeliveryId.New(), supplier, warehouse, plannedAt);
        var lineNo = 1;
        foreach (var (product, expected, pack) in lines)
        {
            delivery._lines.Add(new DeliveryLine(lineNo++, product, expected, pack));
        }

        delivery.Raise(new DeliveryAnnounced(delivery.Id, supplier, warehouse, plannedAt, DateTimeOffset.UtcNow));
        return delivery;
    }

    public void AssignDockSlot(DockSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        EnsureStatus(DeliveryStatus.Announced, "assign a dock slot");
        Slot = slot;
    }

    public void RegisterArrival()
    {
        EnsureStatus(DeliveryStatus.Announced, "register arrival");
        Status = DeliveryStatus.Arrived;
        Raise(new DeliveryArrived(Id, DateTimeOffset.UtcNow));
    }

    public void StartReceiving()
    {
        EnsureStatus(DeliveryStatus.Arrived, "start receiving");
        Status = DeliveryStatus.Receiving;
    }

    public void RecordReceipt(int lineNo, Quantity actual, BatchInfo? batch, DiscrepancyType discrepancy, string? note = null)
    {
        ArgumentNullException.ThrowIfNull(actual);
        EnsureStatus(DeliveryStatus.Receiving, "record a receipt");

        var line = _lines.SingleOrDefault(l => l.LineNo == lineNo)
            ?? throw new DomainException("delivery_line_not_found", $"Delivery {Id} has no line {lineNo}.");
        line.RecordReceipt(actual, batch, discrepancy, note);
    }

    public void ConfirmReceipt()
    {
        EnsureStatus(DeliveryStatus.Receiving, "confirm the receipt");
        if (_lines.All(l => !l.IsRecorded))
        {
            throw new DomainException("delivery_nothing_received", $"Delivery {Id} has no recorded lines to confirm.");
        }

        // Unrecorded lines are implicit full shortages.
        foreach (var line in _lines.Where(l => !l.IsRecorded))
        {
            line.RecordReceipt(Quantity.Zero(line.Expected.Unit), batch: null, DiscrepancyType.Shortage, "Not delivered");
        }

        Status = DeliveryStatus.Received;
        Raise(new GoodsReceiptConfirmed(
            Id, Warehouse, _lines.Count, _lines.Count(l => l.Discrepancy != DiscrepancyType.None), DateTimeOffset.UtcNow));
    }

    public void StartPutAway()
    {
        EnsureStatus(DeliveryStatus.Received, "start put-away");
        Status = DeliveryStatus.PutAwayInProgress;
    }

    public void CompletePutAway()
    {
        EnsureStatus(DeliveryStatus.PutAwayInProgress, "complete put-away");
        Status = DeliveryStatus.Completed;
    }

    public void Cancel()
    {
        EnsureStatus(DeliveryStatus.Announced, "cancel");
        Status = DeliveryStatus.Cancelled;
    }

    private void EnsureStatus(DeliveryStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainException(
                "delivery_invalid_status",
                $"Cannot {action}: delivery {Id} is {Status}, expected {expected}.");
        }
    }
}

/// <summary>One announced position of a delivery; receipt facts land on the same line.</summary>
public sealed class DeliveryLine
{
    internal DeliveryLine(int lineNo, ProductCode product, Quantity expected, DeliveryPack? pack = null)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(expected);
        LineNo = lineNo;
        Product = product;
        Expected = expected;
        Pack = pack;
    }

    private DeliveryLine()
    {
    }

    public int LineNo { get; }

    public ProductCode Product { get; } = null!;

    public Quantity Expected { get; } = null!;

    /// <summary>Delivery-specific pack for converting announced units to base units; null = use catalog default.</summary>
    public DeliveryPack? Pack { get; }

    public Quantity? Actual { get; private set; }

    public BatchInfo? Batch { get; private set; }

    public DiscrepancyType Discrepancy { get; private set; }

    public string? Note { get; private set; }

    public bool IsRecorded => Actual is not null;

    internal void RecordReceipt(Quantity actual, BatchInfo? batch, DiscrepancyType discrepancy, string? note)
    {
        Actual = actual;
        Batch = batch;
        Discrepancy = discrepancy;
        Note = note;
    }
}
