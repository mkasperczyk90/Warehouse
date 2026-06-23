using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Domain;

public sealed class InboundDeliveryTests
{
    private static DockSlot Slot()
    {
        var from = DateTimeOffset.UtcNow.AddHours(1);
        return DockSlot.Of("D-1", from, from.AddHours(1));
    }

    [Fact]
    public void Announce_starts_in_Announced_with_lines_and_raises_event()
    {
        var delivery = Build.Delivery();

        Assert.Equal(DeliveryStatus.Announced, delivery.Status);
        Assert.Single(delivery.Lines);
        Assert.Contains(delivery.DomainEvents, e => e is DeliveryAnnounced);
    }

    [Fact]
    public void Announce_without_lines_is_rejected()
    {
        Expect.DomainError("delivery_lines_empty", () => InboundDelivery.Announce(
            new PartyRoleRef(Guid.NewGuid()), WarehouseRef.Of("WH01"), DateTimeOffset.UtcNow, []));
    }

    [Fact]
    public void AssignDockSlot_only_before_arrival()
    {
        var delivery = Build.Delivery();
        delivery.AssignDockSlot(Slot());
        Assert.NotNull(delivery.Slot);

        delivery.RegisterArrival();
        Expect.DomainError("delivery_invalid_status", () => delivery.AssignDockSlot(Slot()));
    }

    [Fact]
    public void RegisterArrival_moves_Announced_to_Arrived_and_raises_event()
    {
        var delivery = Build.Delivery();

        delivery.RegisterArrival();

        Assert.Equal(DeliveryStatus.Arrived, delivery.Status);
        Assert.Contains(delivery.DomainEvents, e => e is DeliveryArrived);
    }

    [Fact]
    public void StartReceiving_requires_arrival_first()
    {
        var delivery = Build.Delivery(); // Announced

        Expect.DomainError("delivery_invalid_status", delivery.StartReceiving);
    }

    [Fact]
    public void RecordReceipt_requires_the_receiving_state()
    {
        var delivery = Build.Delivery(); // Announced

        Expect.DomainError("delivery_invalid_status",
            () => delivery.RecordReceipt(1, Build.Qty(100), null, DiscrepancyType.None));
    }

    [Fact]
    public void RecordReceipt_on_an_unknown_line_is_rejected()
    {
        var delivery = Build.ReceivingDelivery();

        Expect.DomainError("delivery_line_not_found",
            () => delivery.RecordReceipt(99, Build.Qty(100), null, DiscrepancyType.None));
    }

    [Fact]
    public void ConfirmReceipt_with_nothing_recorded_is_rejected()
    {
        var delivery = Build.ReceivingDelivery();

        Expect.DomainError("delivery_nothing_received", delivery.ConfirmReceipt);
    }

    [Fact]
    public void ConfirmReceipt_treats_unrecorded_lines_as_shortages_and_reports_them()
    {
        // Two lines; only the first is counted, so the second must be confirmed as a full shortage.
        var delivery = InboundDelivery.Announce(
            new PartyRoleRef(Guid.NewGuid()), WarehouseRef.Of("WH01"), DateTimeOffset.UtcNow.AddDays(1),
            [(Build.Code("A"), Build.Qty(100), null), (Build.Code("B"), Build.Qty(50), null)]);
        delivery.RegisterArrival();
        delivery.StartReceiving();
        delivery.RecordReceipt(1, Build.Qty(100), BatchInfo.Of("LOT-1"), DiscrepancyType.None);

        delivery.ConfirmReceipt();

        Assert.Equal(DeliveryStatus.Received, delivery.Status);
        var line2 = delivery.Lines.Single(l => l.LineNo == 2);
        Assert.True(line2.IsRecorded);
        Assert.True(line2.Actual!.IsZero);
        Assert.Equal(DiscrepancyType.Shortage, line2.Discrepancy);

        var confirmed = Assert.Single(delivery.DomainEvents.OfType<GoodsReceiptConfirmed>());
        Assert.Equal(2, confirmed.ReceivedLines);
        Assert.Equal(1, confirmed.LinesWithDiscrepancies);
    }

    [Fact]
    public void Cancel_is_allowed_only_before_arrival()
    {
        var delivery = Build.Delivery();
        delivery.RegisterArrival();

        Expect.DomainError("delivery_invalid_status", delivery.Cancel);
    }
}
