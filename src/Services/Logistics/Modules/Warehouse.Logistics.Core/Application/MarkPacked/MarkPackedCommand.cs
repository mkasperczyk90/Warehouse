using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.MarkPacked;

/// <summary>UC-11 — picking finished and goods are packed (Picking → Packed).</summary>
public sealed record MarkPackedCommand(Guid OrderId);

public sealed class MarkPackedHandler(IOutboundOrderRepository orders, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(MarkPackedCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var order = await orders.GetByIdAsync(new OrderId(command.OrderId), cancellationToken)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");

        order.MarkPacked();
        orders.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
