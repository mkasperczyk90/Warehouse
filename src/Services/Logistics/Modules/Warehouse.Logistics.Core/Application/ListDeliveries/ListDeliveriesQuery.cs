using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

namespace Warehouse.Logistics.Core.Application.ListDeliveries;

/// <summary>List inbound deliveries, optionally filtered to one lifecycle state (e.g. arriving today).</summary>
public sealed record ListDeliveriesQuery(DeliveryStatus? Status);

public sealed record DeliverySummaryDto(
    Guid Id,
    string WarehouseCode,
    DateTimeOffset PlannedAt,
    string Status,
    int LineCount);

public sealed class ListDeliveriesHandler(LogisticsDbContext db)
{
    public async Task<IReadOnlyList<DeliverySummaryDto>> HandleAsync(
        ListDeliveriesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var deliveries = db.Deliveries.AsNoTracking();
        if (query.Status is { } status)
        {
            deliveries = deliveries.Where(d => d.Status == status);
        }

        // Project to scalars on the server, format the enum on the client (it is stored as text but
        // ToString() over the enum does not translate cleanly).
        var rows = await deliveries
            .OrderBy(d => d.PlannedAt)
            .Select(d => new
            {
                d.Id,
                d.Warehouse,
                d.PlannedAt,
                d.Status,
                LineCount = d.Lines.Count,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new DeliverySummaryDto(
                r.Id.Value, r.Warehouse.Code, r.PlannedAt, r.Status.ToString(), r.LineCount))
            .ToList();
    }
}
