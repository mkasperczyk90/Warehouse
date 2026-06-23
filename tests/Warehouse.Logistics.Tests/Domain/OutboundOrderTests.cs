using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Domain;

public sealed class OutboundOrderTests
{
    [Fact]
    public void Create_starts_in_Created_numbers_lines_and_raises_event()
    {
        var order = OutboundOrder.Create(
            new PartyRoleRef(Guid.NewGuid()),
            Build.ShipTo(),
            WarehouseRef.Of("WH01"),
            DateTimeOffset.UtcNow.AddDays(2),
            [(Build.Code("A"), Build.Qty(3)), (Build.Code("B"), Build.Qty(7))]);

        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Equal([1, 2], order.Lines.Select(l => l.LineNo));
        Assert.Contains(order.DomainEvents, e => e is OutboundOrderCreated);
    }

    [Fact]
    public void Create_without_lines_is_rejected()
    {
        Expect.DomainError("order_lines_empty", () => OutboundOrder.Create(
            new PartyRoleRef(Guid.NewGuid()), Build.ShipTo(), WarehouseRef.Of("WH01"),
            DateTimeOffset.UtcNow, []));
    }

    [Fact]
    public void Create_line_with_zero_quantity_is_rejected()
    {
        Expect.DomainError("order_line_zero", () => OutboundOrder.Create(
            new PartyRoleRef(Guid.NewGuid()), Build.ShipTo(), WarehouseRef.Of("WH01"),
            DateTimeOffset.UtcNow, [(Build.Code(), Quantity.Zero(UnitOfMeasure.Piece))]));
    }

    [Fact]
    public void MarkReserved_fully_moves_to_Reserved_and_reports_fully()
    {
        var order = Build.Order();

        order.MarkReserved(fully: true);

        Assert.Equal(OrderStatus.Reserved, order.Status);
        var reserved = Assert.Single(order.DomainEvents.OfType<OrderReserved>());
        Assert.True(reserved.Fully);
    }

    [Fact]
    public void MarkReserved_partially_moves_to_PartiallyReserved()
    {
        var order = Build.Order();

        order.MarkReserved(fully: false);

        Assert.Equal(OrderStatus.PartiallyReserved, order.Status);
    }

    [Fact]
    public void MarkReserved_is_rejected_once_already_picking()
    {
        var order = Build.PickingOrder();

        Expect.DomainError("order_invalid_status", () => order.MarkReserved(fully: true));
    }

    [Fact]
    public void StartPicking_requires_a_reserved_order()
    {
        var order = Build.Order(); // Created

        Expect.DomainError("order_invalid_status", order.StartPicking);
    }

    [Fact]
    public void MarkPacked_requires_picking()
    {
        var order = Build.ReservedOrder();

        Expect.DomainError("order_invalid_status", order.MarkPacked);
    }

    [Fact]
    public void MarkDispatched_requires_packed()
    {
        var order = Build.PickingOrder();

        Expect.DomainError("order_invalid_status", order.MarkDispatched);
    }

    [Fact]
    public void Happy_path_runs_Created_to_Dispatched()
    {
        var order = Build.Order();

        order.MarkReserved(fully: true);
        order.StartPicking();
        order.MarkPacked();
        order.MarkDispatched();

        Assert.Equal(OrderStatus.Dispatched, order.Status);
    }

    [Theory]
    [InlineData(false)] // Created
    [InlineData(true)]  // Reserved
    public void Cancel_is_allowed_before_dispatch(bool reserve)
    {
        var order = Build.Order();
        if (reserve)
        {
            order.MarkReserved(fully: true);
        }

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_is_rejected_once_picking_has_started()
    {
        var order = Build.PickingOrder();

        Expect.DomainError("order_not_cancellable", order.Cancel);
    }
}
