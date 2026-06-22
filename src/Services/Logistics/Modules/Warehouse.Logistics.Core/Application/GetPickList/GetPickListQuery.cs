using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

namespace Warehouse.Logistics.Core.Application.GetPickList;

/// <summary>Read model for an order's pick list (the terminal's picking worklist).</summary>
public sealed record GetPickListQuery(Guid OrderId);

public sealed record PickListDto(Guid OrderId, int Picked, int Total, IReadOnlyList<PickTaskDto> Tasks);

public sealed record PickTaskDto(
    int Sequence,
    string Location,
    string ProductCode,
    string? BatchNumber,
    decimal Quantity,
    string Unit,
    string Status);

public sealed class GetPickListHandler(LogisticsDbContext db)
{
    public async Task<PickListDto?> HandleAsync(GetPickListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var orderId = new OrderId(query.OrderId);

        var pickList = await db.PickLists.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        if (pickList is null)
        {
            return null;
        }

        var tasks = pickList.Tasks
            .OrderBy(t => t.Sequence)
            .Select(t => new PickTaskDto(
                t.Sequence, t.Location.Code, t.Product.Value, t.Batch?.Number,
                t.Quantity.Amount, t.Quantity.Unit.Code, t.Status.ToString()))
            .ToList();

        return new PickListDto(query.OrderId, tasks.Count(t => t.Status == nameof(PickTaskStatus.Picked)), tasks.Count, tasks);
    }
}
