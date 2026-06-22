using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

namespace Warehouse.Logistics.Core.Application.ListOrders;

/// <summary>List outbound orders, optionally filtered to one lifecycle state.</summary>
public sealed record ListOrdersQuery(OrderStatus? Status);

public sealed record OrderSummaryDto(
    Guid Id,
    string WarehouseCode,
    DateTimeOffset RequiredAt,
    string Status,
    int LineCount);

public sealed class ListOrdersHandler(LogisticsDbContext db)
{
    public async Task<IReadOnlyList<OrderSummaryDto>> HandleAsync(
        ListOrdersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orders = db.Orders.AsNoTracking();
        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        var rows = await orders
            .OrderBy(o => o.RequiredAt)
            .Select(o => new { o.Id, o.Warehouse, o.RequiredAt, o.Status, LineCount = o.Lines.Count })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new OrderSummaryDto(
                r.Id.Value, r.Warehouse.Code, r.RequiredAt, r.Status.ToString(), r.LineCount))
            .ToList();
    }
}
