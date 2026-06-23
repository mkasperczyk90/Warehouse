namespace Warehouse.Logistics.Core.Application.Deliveries.AssignDockSlot;

/// <summary>UC-01 — book a dock + time window for an announced delivery.</summary>
public sealed record AssignDockSlotCommand(Guid DeliveryId, string DockCode, DateTimeOffset From, DateTimeOffset To);
