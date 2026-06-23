using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Orders.CreateOutboundOrder;

/// <summary>
/// Validates every ordered SKU against the local Catalog replica (ADR-0003), creates the
/// <see cref="OutboundOrder"/>, and announces it through the transactional outbox so Inventory can
/// make a soft reservation against available-to-promise (UC-09).
/// </summary>
public sealed class CreateOutboundOrderHandler(
    IOutboundOrderRepository orders,
    ICatalogProductReplica catalog,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task<Guid> HandleAsync(CreateOutboundOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Lines is null || command.Lines.Count == 0)
        {
            throw new DomainException("order_lines_empty", "An outbound order needs at least one line.");
        }

        var codes = command.Lines.Select(l => ProductCode.Of(l.ProductCode)).ToList();
        var unknown = await catalog.FindUnknownAsync(codes, cancellationToken);
        if (unknown.Count > 0)
        {
            throw new DomainException(
                "order_unknown_sku",
                $"Unknown SKU(s) not in the catalog: {string.Join(", ", unknown)}.");
        }

        var lines = command.Lines
            .Select(l => (ProductCode.Of(l.ProductCode), Quantity.Of(l.Quantity, UnitOfMeasure.FromCode(l.Unit))))
            .ToList();

        var order = OutboundOrder.Create(
            new PartyRoleRef(command.CustomerRoleId),
            Address.Of(command.ShipTo.Street, command.ShipTo.City, command.ShipTo.PostalCode, command.ShipTo.CountryCode),
            WarehouseRef.Of(command.WarehouseCode),
            command.RequiredAt,
            lines);

        orders.Add(order);

        await outbox.PublishAsync(new OutboundOrderPlacedV1(
            order.Id.Value,
            command.CustomerRoleId,
            order.Warehouse.Code,
            order.Lines
                .Select(l => new OutboundOrderLineV1(l.LineNo, l.Product.Value, l.Ordered.Amount, l.Ordered.Unit.Code))
                .ToList(),
            DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return order.Id.Value;
    }
}
