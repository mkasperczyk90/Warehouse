using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

namespace Warehouse.Logistics.Core.Application.Orders.GetOrder;

/// <summary>Read model for a single outbound order (header + ship-to + lines).</summary>
public sealed record GetOrderQuery(Guid OrderId);

public sealed record OrderDto(
    Guid Id,
    string CustomerRoleId,
    string WarehouseCode,
    DateTimeOffset RequiredAt,
    string Status,
    AddressDto ShipTo,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record AddressDto(string Street, string City, string PostalCode, string CountryCode);

public sealed record OrderLineDto(int LineNo, string ProductCode, decimal Quantity, string Unit);

public sealed class GetOrderHandler(LogisticsDbContext db)
{
    public async Task<OrderDto?> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var id = new OrderId(query.OrderId);

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return order is null ? null : Map(order);
    }

    internal static OrderDto Map(OutboundOrder o) => new(
        o.Id.Value,
        o.Customer.Value,
        o.Warehouse.Code,
        o.RequiredAt,
        o.Status.ToString(),
        new AddressDto(o.ShipTo.Street, o.ShipTo.City, o.ShipTo.PostalCode, o.ShipTo.CountryCode),
        o.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new OrderLineDto(l.LineNo, l.Product.Value, l.Ordered.Amount, l.Ordered.Unit.Code))
            .ToList());
}
