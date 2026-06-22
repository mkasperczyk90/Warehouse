using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Domain.Services;

/// <summary>
/// The outbound flow is a relay across three aggregates — <see cref="OutboundOrder"/>,
/// <see cref="PickList"/> and <see cref="Shipment"/> — and the handoff rules between them belong
/// to none of them alone. This service owns those cross-aggregate transitions: a pick list may be
/// raised only for a <c>Reserved</c> order, and a shipment may be created only once its pick list
/// is complete. Each aggregate still guards its own state; the service guards the seam.
/// </summary>
public static class OutboundFulfillmentService
{
    /// <summary>
    /// Releases a reserved order to the floor: transitions the order to <c>Picking</c> (which the
    /// order itself rejects unless it is <c>Reserved</c>) and builds the routed pick list for it.
    /// </summary>
    public static PickList ReleaseToPicking(
        OutboundOrder order,
        IReadOnlyCollection<(LocationRef Location, ProductCode Product, BatchInfo? Batch, Quantity Quantity)> plannedPicks)
    {
        ArgumentNullException.ThrowIfNull(order);

        order.StartPicking();
        return PickList.CreateFor(order.Id, plannedPicks);
    }

    /// <summary>
    /// Closes picking and opens a shipment: requires the pick list to belong to the order and to
    /// have no pending tasks, marks the order <c>Packed</c>, and creates the shipment for a carrier.
    /// </summary>
    public static Shipment CompletePacking(OutboundOrder order, PickList pickList, PartyRoleRef carrier)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(pickList);

        if (pickList.OrderId != order.Id)
        {
            throw new DomainException(
                "picklist_order_mismatch",
                $"Pick list {pickList.Id} belongs to order {pickList.OrderId}, not {order.Id}.");
        }

        if (!pickList.IsCompleted)
        {
            throw new DomainException(
                "picklist_incomplete",
                $"Pick list {pickList.Id} still has pending tasks; order {order.Id} cannot be packed yet.");
        }

        order.MarkPacked();
        return Shipment.CreateFor(order.Id, carrier);
    }
}
