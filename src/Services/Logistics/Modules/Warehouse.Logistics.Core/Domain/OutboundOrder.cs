using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): an outbound order from creation to dispatch (UC-09…UC-12).
/// Reservations live in the Inventory context; this aggregate tracks the process state
/// (see the OutboundOrder state diagram in docs/03-use-cases.md).
/// </summary>
public sealed class OutboundOrder : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    private OutboundOrder(
        OrderId id, PartyRoleRef customer, Address shipTo, WarehouseRef warehouse, DateTimeOffset requiredAt)
        : base(id)
    {
        Customer = customer;
        ShipTo = shipTo;
        Warehouse = warehouse;
        RequiredAt = requiredAt;
        Status = OrderStatus.Created;
    }

    private OutboundOrder()
    {
    }

    public PartyRoleRef Customer { get; private set; }

    public Address ShipTo { get; private set; } = null!;

    public WarehouseRef Warehouse { get; private set; } = null!;

    public DateTimeOffset RequiredAt { get; private set; }

    public OrderStatus Status { get; private set; }

    /// <summary>How the coordinator resolved a partial reservation (null until a decision is made).</summary>
    public PartialDecision? Resolution { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static OutboundOrder Create(
        PartyRoleRef customer,
        Address shipTo,
        WarehouseRef warehouse,
        DateTimeOffset requiredAt,
        IReadOnlyCollection<(ProductCode Product, Quantity Ordered)> lines)
    {
        ArgumentNullException.ThrowIfNull(shipTo);
        ArgumentNullException.ThrowIfNull(warehouse);
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException("order_lines_empty", "An outbound order needs at least one line.");
        }

        var order = new OutboundOrder(OrderId.New(), customer, shipTo, warehouse, requiredAt);
        var lineNo = 1;
        foreach (var (product, ordered) in lines)
        {
            order._lines.Add(new OrderLine(lineNo++, product, ordered));
        }

        order.Raise(new OutboundOrderCreated(order.Id, customer, requiredAt, DateTimeOffset.UtcNow));
        return order;
    }

    /// <summary>Called by the fulfillment saga after Inventory confirms reservations.</summary>
    public void MarkReserved(bool fully)
    {
        if (Status is not (OrderStatus.Created or OrderStatus.PartiallyReserved))
        {
            throw new DomainException("order_invalid_status", $"Cannot mark order {Id} reserved: it is {Status}.");
        }

        Status = fully ? OrderStatus.Reserved : OrderStatus.PartiallyReserved;
        Raise(new OrderReserved(Id, fully, DateTimeOffset.UtcNow));
    }

    /// <summary>UC-09 coordinator decision on a partial order: ship the reserved portion now and backorder
    /// the rest. The available portion proceeds as a normal reserved order.</summary>
    public void SplitForAvailable()
    {
        EnsureStatus(OrderStatus.PartiallyReserved, "split");
        Status = OrderStatus.Reserved;
        Resolution = PartialDecision.Split;
    }

    /// <summary>UC-09 coordinator decision on a partial order: hold the whole order until stock covers it.
    /// The order stays partially reserved (waiting); only the recorded decision changes.</summary>
    public void HoldForStock()
    {
        EnsureStatus(OrderStatus.PartiallyReserved, "hold");
        Resolution = PartialDecision.Hold;
    }

    public void StartPicking()
    {
        EnsureStatus(OrderStatus.Reserved, "start picking");
        Status = OrderStatus.Picking;
    }

    public void MarkPacked()
    {
        EnsureStatus(OrderStatus.Picking, "mark as packed");
        Status = OrderStatus.Packed;
    }

    public void MarkDispatched()
    {
        EnsureStatus(OrderStatus.Packed, "mark as dispatched");
        Status = OrderStatus.Dispatched;
    }

    /// <summary>Reservations are released by the saga reacting to this transition.</summary>
    public void Cancel()
    {
        if (Status is not (OrderStatus.Created or OrderStatus.PartiallyReserved or OrderStatus.Reserved))
        {
            throw new DomainException("order_not_cancellable", $"Order {Id} is {Status} and can no longer be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainException(
                "order_invalid_status",
                $"Cannot {action}: order {Id} is {Status}, expected {expected}.");
        }
    }
}

public sealed class OrderLine
{
    internal OrderLine(int lineNo, ProductCode product, Quantity ordered)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(ordered);
        if (ordered.IsZero)
        {
            throw new DomainException("order_line_zero", $"Order line {lineNo} ({product}) must order a positive quantity.");
        }

        LineNo = lineNo;
        Product = product;
        Ordered = ordered;
    }

    private OrderLine()
    {
    }

    public int LineNo { get; }

    public ProductCode Product { get; } = null!;

    public Quantity Ordered { get; } = null!;
}
