using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.PickLists.ConfirmPick;

/// <summary>UC-10 — the operator picked a task (both scans landed): mark it picked, tell Inventory to
/// consume the allocation (<see cref="PickConfirmedV1"/>), and pack the order once every task is done.</summary>
public sealed record ConfirmPickCommand(Guid OrderId, int Sequence, string PickedBy);

public sealed class ConfirmPickHandler(
    IPickListRepository pickLists,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(ConfirmPickCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var orderId = new OrderId(command.OrderId);
        var pickList = await pickLists.GetByOrderAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"No pick list for order {command.OrderId}.");

        pickList.ConfirmPick(command.Sequence, command.PickedBy);
        pickLists.Update(pickList);

        var task = pickList.Tasks.Single(t => t.Sequence == command.Sequence);
        await outbox.PublishAsync(new PickConfirmedV1(
            command.OrderId,
            task.Sequence,
            task.Location.Code,
            task.Product.Value,
            task.Batch?.Number,
            task.Quantity.Amount,
            task.Quantity.Unit.Code,
            DateTimeOffset.UtcNow));

        // Picking complete does NOT pack the order — the operator packs it in the packing zone
        // (UC-11), which is where the order transitions Picking → Packed.
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
