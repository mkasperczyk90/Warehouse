using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.PickLists.ReportShortPick;

/// <summary>UC-10 exception — a location was short. Marks the task <c>ShortPick</c>. Replanning onto
/// another location/batch is the deferred wave optimiser (docs/PLAN.md), so this records the fact.</summary>
public sealed record ReportShortPickCommand(Guid OrderId, int Sequence, string ReportedBy, string? Reason);

public sealed class ReportShortPickHandler(IPickListRepository pickLists, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(ReportShortPickCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var pickList = await pickLists.GetByOrderAsync(new OrderId(command.OrderId), cancellationToken)
            ?? throw new KeyNotFoundException($"No pick list for order {command.OrderId}.");

        pickList.ReportShort(command.Sequence, command.ReportedBy);
        pickLists.Update(pickList);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
