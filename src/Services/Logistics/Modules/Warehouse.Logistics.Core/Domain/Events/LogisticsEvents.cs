using Warehouse.SharedKernel.Domain;

namespace Warehouse.Logistics.Core.Domain.Events;

public sealed record DeliveryAnnounced(
    DeliveryId DeliveryId, PartyRoleRef Supplier, WarehouseRef Warehouse, DateTimeOffset PlannedAt, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record DeliveryArrived(
    DeliveryId DeliveryId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Triggers stock receipt into the dock buffer on the Inventory side.</summary>
public sealed record GoodsReceiptConfirmed(
    DeliveryId DeliveryId, WarehouseRef Warehouse, int ReceivedLines, int LinesWithDiscrepancies, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record OutboundOrderCreated(
    OrderId OrderId, PartyRoleRef Customer, DateTimeOffset RequiredAt, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when a routed pick list is generated for an order (UC-10).</summary>
public sealed record PickListCreated(
    PickListId PickListId, OrderId OrderId, int TaskCount, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record OrderReserved(
    OrderId OrderId, bool Fully, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ShipmentDispatched(
    ShipmentId ShipmentId, OrderId OrderId, PartyRoleRef Carrier, TrackingNumber? Tracking, DateTimeOffset OccurredAt) : IDomainEvent;
