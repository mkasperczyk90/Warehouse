namespace Warehouse.Logistics.Core.Application.Orders.CreateOutboundOrder;

/// <summary>UC-09 — place an outbound order: consignee, ship-to address, warehouse, required date, lines.</summary>
public sealed record CreateOutboundOrderCommand(
    Guid CustomerRoleId,
    OutboundShipTo ShipTo,
    string WarehouseCode,
    DateTimeOffset RequiredAt,
    IReadOnlyList<OutboundOrderLineInput> Lines);

public sealed record OutboundShipTo(string Street, string City, string PostalCode, string CountryCode);

public sealed record OutboundOrderLineInput(string ProductCode, decimal Quantity, string Unit);
