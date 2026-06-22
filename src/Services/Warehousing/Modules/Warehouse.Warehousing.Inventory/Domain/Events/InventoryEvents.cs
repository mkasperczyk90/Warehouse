using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain.Events;

public sealed record StockReceived(
    StockItemId StockItemId, Sku Sku, LocationCode Location, Quantity Quantity, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Soft reservation opened against available-to-promise (SKU + warehouse, no batch/location yet).</summary>
public sealed record StockReserved(
    StockReservationId ReservationId, Sku Sku, WarehouseCode Warehouse, OrderRef OrderRef, Quantity Quantity, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Soft reservation released before being fully allocated.</summary>
public sealed record ReservationReleased(
    StockReservationId ReservationId, Sku Sku, WarehouseCode Warehouse, OrderRef OrderRef, Quantity Outstanding, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Hard allocation pinned to a concrete StockItem (batch + location) at wave/pick time.</summary>
public sealed record StockAllocated(
    StockItemId StockItemId, AllocationId AllocationId, StockReservationId ReservationId, OrderRef OrderRef, Quantity Quantity, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record AllocationReleased(
    StockItemId StockItemId, AllocationId AllocationId, OrderRef OrderRef, Quantity Quantity, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record StockPicked(
    StockItemId StockItemId, AllocationId AllocationId, Sku Sku, LocationCode Location, Quantity Quantity, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record StockAdjusted(
    StockItemId StockItemId, Sku Sku, LocationCode Location, Quantity Before, Quantity After, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Handled by quarantining every StockItem of this batch (cross-aggregate convergence).</summary>
public sealed record BatchQuarantined(
    BatchId BatchId, Sku Sku, BatchNumber Number, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Handled by blocking every StockItem of this batch (cross-aggregate convergence).</summary>
public sealed record BatchBlocked(
    BatchId BatchId, Sku Sku, BatchNumber Number, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record BatchReleased(
    BatchId BatchId, Sku Sku, BatchNumber Number, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record StocktakeApproved(
    StocktakeId StocktakeId, int CountedLocations, int Differences, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Handled by transferring the unit's stock lines to the new location (StockTransferService).</summary>
public sealed record HandlingUnitMoved(
    HandlingUnitId HandlingUnitId, LpnCode Lpn, LocationCode From, LocationCode To, DateTimeOffset OccurredAt) : IDomainEvent;
