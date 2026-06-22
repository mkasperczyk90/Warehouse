using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Application.Consumers;

/// <summary>
/// UC-10: builds the routed <see cref="PickList"/> from the picks Inventory planned for a released
/// order (<see cref="PicksPlannedV1"/>). Idempotent — a redelivery for an order that already has a
/// pick list is ignored.
/// </summary>
public sealed class PicksPlannedConsumer(IPickListRepository pickLists, IUnitOfWork unitOfWork)
{
    public async Task Handle(PicksPlannedV1 message, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(message.OrderId);
        if (message.Picks.Count == 0 || await pickLists.GetByOrderAsync(orderId, cancellationToken) is not null)
        {
            return;
        }

        var plannedPicks = message.Picks
            .Select(p => (
                LocationRef.Of(p.Location),
                ProductCode.Of(p.Sku),
                p.BatchNumber is { } b ? BatchInfo.Of(b) : null,
                Quantity.Of(p.Quantity, UnitOfMeasure.FromCode(p.Unit))))
            .ToList();

        var pickList = PickList.CreateFor(orderId, plannedPicks);
        pickLists.Add(pickList);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
