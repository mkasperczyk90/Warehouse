using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

namespace Warehouse.Logistics.Core.Application.Dispatch.GetDispatchBoard;

/// <summary>UC-12 — the dispatch board (admin-6): packed shipments grouped into the lifecycle columns the
/// coordinator works (awaiting carrier → carrier assigned → pickup notice sent → dispatched).</summary>
public sealed record GetDispatchBoardQuery;

public sealed record DispatchColumnDto(string Key, IReadOnlyList<DispatchShipmentDto> Shipments);

public sealed record DispatchShipmentDto(
    string Id,
    string Customer,
    string Summary,
    DispatchCarrierDto? Carrier,
    string? Pickup,
    DispatchBadgeDto? Badge,
    string? Tracking,
    bool? CanAssign);

public sealed record DispatchCarrierDto(string Code, string Name);

public sealed record DispatchBadgeDto(string Variant, string Label);

public sealed class GetDispatchBoardHandler(LogisticsDbContext db)
{
    public async Task<IReadOnlyList<DispatchColumnDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var shipments = await db.Shipments.AsNoTracking().ToListAsync(cancellationToken);
        var orderIds = shipments.Select(s => s.OrderId).Distinct().ToList();
        var orders = await db.Orders.AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        return Map(shipments, orders);
    }

    public static IReadOnlyList<DispatchColumnDto> Map(
        IReadOnlyCollection<Shipment> shipments, IReadOnlyDictionary<OrderId, OutboundOrder> orders)
    {
        DispatchColumnDto Column(string key, ShipmentStatus status) => new(
            key,
            shipments.Where(s => s.Status == status).Select(s => ToCard(s, orders)).ToList());

        return
        [
            Column("awaitingCarrier", ShipmentStatus.AwaitingCarrier),
            Column("assigned", ShipmentStatus.CarrierAssigned),
            Column("noticeSent", ShipmentStatus.ReadyForPickup),
            Column("dispatched", ShipmentStatus.Dispatched),
        ];
    }

    private static DispatchShipmentDto ToCard(Shipment s, IReadOnlyDictionary<OrderId, OutboundOrder> orders)
    {
        var customer = orders.TryGetValue(s.OrderId, out var order) ? order.Customer.Value : s.OrderId.Value.ToString();
        var totalWeight = s.Packages.Sum(p => p.Weight.Kilograms);
        var summary = $"{s.Packages.Count} pkg · {totalWeight.ToString("0.##", CultureInfo.InvariantCulture)} kg";

        var carrier = s.Carrier is { } c ? new DispatchCarrierDto(c.Value, CarrierName(c.Value)) : null;
        var badge = s.Status switch
        {
            ShipmentStatus.ReadyForPickup => new DispatchBadgeDto("transit", "Awaiting collection"),
            ShipmentStatus.Dispatched => new DispatchBadgeDto("available", "Collected ✓"),
            _ => null,
        };

        return new DispatchShipmentDto(
            s.Id.Value.ToString(),
            customer,
            summary,
            carrier,
            s.Pickup,
            badge,
            s.Status == ShipmentStatus.Dispatched ? s.Tracking?.Value : null,
            s.Status == ShipmentStatus.AwaitingCarrier ? true : null);
    }

    private static string CarrierName(string code) => code switch
    {
        "DH" => "DHL",
        "GL" => "GLS",
        "DP" => "DPD",
        _ => code,
    };
}
