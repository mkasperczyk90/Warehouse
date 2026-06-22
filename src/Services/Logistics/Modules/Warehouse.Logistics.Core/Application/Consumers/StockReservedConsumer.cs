using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Consumers;

/// <summary>
/// Closes the UC-09 loop: Inventory's <see cref="StockReservedV1"/> moves the order to Reserved
/// (or PartiallyReserved). Idempotent — a redelivery for an order past reservation is ignored.
/// </summary>
public sealed class StockReservedConsumer(IOutboundOrderRepository orders, IUnitOfWork unitOfWork)
{
    public async Task Handle(StockReservedV1 message, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(new OrderId(message.OrderId), cancellationToken);
        if (order is null || order.Status is not (OrderStatus.Created or OrderStatus.PartiallyReserved))
        {
            return;
        }

        order.MarkReserved(message.Fully);
        orders.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
